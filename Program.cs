using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Newtonsoft.Json;
using System.Collections.Generic;
using Websocket;
namespace WebSocketYatzy
{   
    public class handleClient
    {
        TcpClient clientSocket;
        String userName = "";
        Guid clientId;
        bool doQuit = false;
        string command = "-";
        NetworkStream networkStream;
        Websocket.Server server;
        string clNo;
        public void startClient(TcpClient inClientSocket, string clineNo)
        {
            this.clientSocket = inClientSocket;
            this.clNo = clineNo;
            Thread ctThread = new Thread(handler);
            ctThread.Start();
        }
        
        public void AnnounceEvent(YatzyGameEvent e)
        {
            networkStream = clientSocket.GetStream();
            byte[] sendBytes = Websocket.Util.GetFrameFromString(e.ToString());
            networkStream.Write(sendBytes, 0, sendBytes.Length);
        }
        public void AnnonceEventAllUsers(YatzyGameEvent e)
        {
            foreach (var client in Program.clients)
            {
                client.AnnounceEvent(e);
            }    
        }

        private void handler()
        {
                    
            byte[] b={12};
            server = new Websocket.Server();
            networkStream = clientSocket.GetStream();
            bool isReconnect = false;
            
            if (!server.Negotiate(networkStream))
                doQuit = true;

            Console.WriteLine("Sucessfully negotiated websock connection with client");
            
            if (Program.game == null)
                Program.game = new YatzyGame();

            doQuit = false;
            while (!doQuit)
            {
                Console.WriteLine("blocking waiting for data");
                command = server.GetMessage(networkStream);
                if (networkStream == null || command == "timeout" || !networkStream.CanWrite)
                {
                    doQuit = true;
                    isReconnect = true;
                    continue;
                }
                Console.WriteLine("Command: " + command);
                
                if (command == "zero" || command == "")
                {
                    Console.WriteLine("socket state: " + clientSocket.Connected.ToString());
                    isReconnect = true;
                    doQuit = true;
                    continue;
                }
                try
                {
                    Command cmd = JsonConvert.DeserializeObject<Command>(command);
                    YatzyPlayer player;
                    YatzyGameEvent ev;                    

                   
                    switch (cmd.command)
                    {
                        case "hello":
                            var arr = cmd.args.Split(';');
                            if(arr.Length != 2) {
                                doQuit = true;
                                continue;
                            }
                            userName = Program.GetUserName(arr[0]);                            
                            player = Program.game.Players.Find(p => p.UserGuid.ToString() == arr[1]);
                            if(player == null)
                               player = new YatzyPlayer(userName, (clientSocket.Client.RemoteEndPoint as IPEndPoint).Address.ToString());
                            else
                            {
                                Console.WriteLine("client reconnected");

                                // should send complete game info here incl. all players and their score.
                            }
                            this.clientId = player.UserGuid;
                            Program.game.Players.Add(player);
                            this.AnnonceEventAllUsers(new YatzyGameEvent(YatzyGameEventType.UserJoined, player));
                            this.AnnounceEvent(new YatzyGameEvent(YatzyGameEventType.UserNameChanged, player));
                            if (isReconnect)
                            {
                                this.AnnounceEvent(new YatzyGameEvent(YatzyGameEventType.GameInfo, player, Program.game.ToString()));
                                isReconnect = true;

                            }
                            break;

                        case "goodbye":
                            player = Program.game.Players.Find(p => p.UserName == cmd.args);
                            if (player != null)
                                Program.game.Players.Remove(player);
                            
                            this.AnnonceEventAllUsers(new YatzyGameEvent(YatzyGameEventType.UserLeft, player));
                            doQuit = true;
                            break;

                        case "resetgame":

                            foreach (var p in Program.game.Players)                            
                                this.AnnonceEventAllUsers(new YatzyGameEvent(YatzyGameEventType.UserLeft, p));                            
                            Program.game.reset();                            
                            doQuit = true;
                            break;

                        case "roll":
                            var rollArgs = cmd.args.Split(';');
                            player = Program.game.Players.Find(p => p.UserName == rollArgs[0]);
                            var doRollDice = rollArgs[1].Split(',');
                            if (player != null && player.ItsMyTurn && player.RollState.RollsLeft > 0)
                            {
                                player.RollState.Roll(
                                    doRollDice[0] == "1" ? false : true,
                                    doRollDice[1] == "1" ? false : true,
                                    doRollDice[2] == "1" ? false : true,
                                    doRollDice[3] == "1" ? false : true,                                    
                                    doRollDice[4] == "1" ? false : true);
                               this.AnnonceEventAllUsers(new YatzyGameEvent(YatzyGameEventType.UserRolled, player,
                                   "{\"rollState\":" + player.RollState.ToString() + ", \"bestBets\":" + player.ScoreCard.GetBestBets(player.RollState) + "}"));                                                              
                            }
                            break;

                        case "score":
                            var scoreArgs = cmd.args.Split(';');
                            player = Program.game.Players.Find(p => p.UserName == scoreArgs[0]);
                            string markAs = scoreArgs[1];
                            
                            if (player != null && player.ItsMyTurn && player.ScoreCard.MarkScore(markAs, player.RollState))
                            {                                
                               ev = new YatzyGameEvent(YatzyGameEventType.UserScored, player);
                               ev.EventArg = player.ScoreCard.ToString();
                               player.ItsMyTurn = false;
                               
                               var index = Program.game.Players.FindIndex(a => a.UserName == player.UserName) + 1;
                               if(index >= Program.game.Players.Count)
                                    index=0;
                               this.AnnonceEventAllUsers(ev);
                               player = Program.game.Players[index];
                               player.RollState.Reset();
                               player.ItsMyTurn = true;                               
                               this.AnnonceEventAllUsers(new YatzyGameEvent(YatzyGameEventType.TurnChanged, player));
                               
                               
                            }
                            break;
                        case "changeDiceHold":
                            var diceArgs = cmd.args.Split(';');
                            player = Program.game.Players.Find(p => p.UserName == diceArgs[0]);                            
                            if (player != null)                            
                                this.AnnonceEventAllUsers(new YatzyGameEvent(YatzyGameEventType.UserChangedDiceHold, player, diceArgs[1]));
                            break;

                        case "startgame":
                            if (Program.game.Players != null && Program.game.Players.Count > 0)
                            {
                                player = Program.game.Players.Find(p => p.UserName == cmd.args);
                                if (player != null)
                                {
                                    player.ItsMyTurn = true;
                                    this.AnnonceEventAllUsers(new YatzyGameEvent(YatzyGameEventType.TurnChanged, player));                                    
                                }
                            }                           
                            break;

                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine("Command Parse Error. Exception: " + e.Message);
                    doQuit = true;
                }                
            }
            Console.WriteLine("Client " + clNo + " exited");
            clientSocket.Close();
            Program.clients.Remove(this);                        
        }

    }
    class Program
    {
        
        public static YatzyGame game=null;
        public static List<handleClient> clients = new List<handleClient>();

        public static string GetUserName(string reqUserName)
        {
            if(game==null || game.Players == null || game.Players.Count == 0)
                return reqUserName;                
            var i=0;
            var resUserName = reqUserName;
            while(game.Players.Find(u => u.UserName == resUserName) != null) {
                resUserName = reqUserName + i.ToString();
            }
            return resUserName;
        }
        static void Main(string[] args)
        {
            IPAddress localAddr = IPAddress.Parse("192.168.1.201");
            Console.WriteLine("Server listening on port 8087");
            TcpListener serverSocket = new TcpListener(localAddr, 8087);
            TcpClient clientSocket = default(TcpClient);
            bool doQuit = false;
            int counter = 0;
            serverSocket.Start();            
            counter = 0;
            while (!doQuit)
            {                
                counter += 1;
                clientSocket = serverSocket.AcceptTcpClient();
                Console.WriteLine(" >> " + "Client No:" + Convert.ToString(counter) + " started!");
                handleClient client = new handleClient();
                clients.Add(client);
                client.startClient(clientSocket, Convert.ToString(counter));
            }
            
            serverSocket.Stop();
            clientSocket.Close();
            //Console.WriteLine(" >> " + "exit");            
        }


    }
}