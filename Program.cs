using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Security.Cryptography;
using Newtonsoft.Json;
using System.Collections.Generic;
namespace WebSocketYatzy
{
    public class Command
    {        
        public string command { get; set; }
        public string args { get; set; }
    }
    
    

    public class handleClient
    {
        TcpClient clientSocket;
        string clNo;
        public void startClient(TcpClient inClientSocket, string clineNo)
        {
            this.clientSocket = inClientSocket;
            this.clNo = clineNo;
            Thread ctThread = new Thread(handleCommand);
            ctThread.Start();
        }
        
        public void AnnounceEvent(string eventJSON)
        {
            NetworkStream networkStream = clientSocket.GetStream();
            byte[] sendBytes = Program.GetFrameFromString(eventJSON);
            networkStream.Write(sendBytes, 0, sendBytes.Length);
        }

        private void handleCommand()
        {
            byte[] buffer = new byte[4096];
            Random rnd = new Random();
            string browserSent = "";
            String userName = "";
            string headerResponse = "";
            NetworkStream networkStream = clientSocket.GetStream();
            var i = networkStream.Read(buffer, 0, buffer.Length);

            //Console.WriteLine("client connected");
            if (Program.game == null)
                Program.game = new YatzyGame();
            
            headerResponse = (System.Text.Encoding.UTF8.GetString(buffer)).Substring(0, i);
            // write received data to the console
            //Console.WriteLine(headerResponse);
            //Console.WriteLine("=====================");


            /* Handshaking and managing ClientSocket */
            var key = headerResponse.Replace("ey:", "`")
                      .Split('`')[1]                     // dGhlIHNhbXBsZSBub25jZQ== \r\n .......
                      .Replace("\r", "").Split('\n')[0]  // dGhlIHNhbXBsZSBub25jZQ==
                      .Trim();

            // key should now equal dGhlIHNhbXBsZSBub25jZQ==
            var test1 = Program.AcceptKey(ref key);

            var newLine = "\r\n";

            var response = "HTTP/1.1 101 Switching Protocols" + newLine
                 + "Upgrade: websocket" + newLine
                 + "Connection: Upgrade" + newLine
                 + "Sec-WebSocket-Accept: " + test1 + newLine + newLine
                //+ "Sec-WebSocket-Protocol: chat, superchat" + newLine
                //+ "Sec-WebSocket-Version: 13" + newLine
                 ;
            var sendBytes = System.Text.Encoding.UTF8.GetBytes(response);
            networkStream.Write(sendBytes, 0, sendBytes.Length);
            var doQuit = false;
            while (!doQuit)
            {
                i = networkStream.Read(buffer, 0, buffer.Length);
                if (i < 4)
                {
                    Console.WriteLine("read timed out");
                    continue;
                }
                
                try
                {
                    browserSent = Program.GetDecodedData(buffer, i);
                }
                catch
                {
                    clientSocket.Close();
                    Program.clients.Remove(this);
                    doQuit = true;
                    continue;
                }
                if (userName == "")
                    userName = browserSent;
                //Console.WriteLine("BrowserSent: " + browserSent);
                try
                {
                    Command cmd = JsonConvert.DeserializeObject<Command>(browserSent);
                    YatzyPlayer player;
                    YatzyGameEvent ev;
                    switch (cmd.command)
                    {
                        case "hello":
                            player = new YatzyPlayer(Program.GetUserName(cmd.args), (clientSocket.Client.RemoteEndPoint as IPEndPoint).Address.ToString(), Program.game.Players.Count);
                            Program.game.Players.Add(player);

                            //Console.WriteLine("player=" + player.ToString());
                            
                            //Console.WriteLine("User " + player.UserName + " connected to game");
                            ev = new YatzyGameEvent(YatzyGameEventType.UserJoined, player);
                            foreach (var client in Program.clients)
                            {
                                //Console.WriteLine("Sending event to " + (client.clientSocket.Client.RemoteEndPoint as IPEndPoint).Address.ToString());

                                client.AnnounceEvent(ev.ToString());
                            }
                            ev = new YatzyGameEvent(YatzyGameEventType.UserNameChanged, player);
                            this.AnnounceEvent(ev.ToString());

                            /* inform about other users already connected */
                            foreach (var otherPlayer in Program.game.Players)
                            {                                
                                if (player.UserName != otherPlayer.UserName)
                                {
                                    ev = new YatzyGameEvent(YatzyGameEventType.UserJoined, otherPlayer);
                                    this.AnnounceEvent(ev.ToString());
                                }
                            }
                            
                            break;

                        case "goodbye":
                            player = Program.game.Players.Find(p => p.UserName == cmd.args);
                            if (player != null)
                            {
                                Program.game.Players.Remove(player);
                                //Console.Write("Player Removed from game");
                            }
                            ev = new YatzyGameEvent(YatzyGameEventType.UserLeft, player);                            
                            foreach (var client in Program.clients)
                            {                                                                
                                //Console.WriteLine("Sending event to " + (client.clientSocket.Client.RemoteEndPoint as IPEndPoint).Address.ToString());
                                client.AnnounceEvent(ev.ToString());                                
                            }
                            clientSocket.Close();
                            Program.clients.Remove(this);
                            doQuit = true;
                            break;

                        case "resetgame":

                            foreach (var p in Program.game.Players)
                            {
                                ev = new YatzyGameEvent(YatzyGameEventType.UserLeft, p);
                                foreach (var client in Program.clients)
                                {
                                    //Console.WriteLine("Sending event to " + (client.clientSocket.Client.RemoteEndPoint as IPEndPoint).Address.ToString());
                                    client.AnnounceEvent(ev.ToString());
                                }                                
                            }
                            Program.game.reset();
                            clientSocket.Close();
                            Program.clients.Remove(this);
                            doQuit = true;
                            break;

                        case "roll":
                            var rollArgs = cmd.args.Split(';');
                            player = Program.game.Players.Find(p => p.UserName == rollArgs[0]);
                            //Console.WriteLine("player " + player.UserName + " is attempting to roll");
                            var doRollDice = rollArgs[1].Split(',');
                            if (player != null && player.ItsMyTurn && player.RollState.RollsLeft > 0)
                            {
                                player.RollState.Roll(
                                    doRollDice[0] == "1" ? false : true,
                                    doRollDice[1] == "1" ? false : true,
                                    doRollDice[2] == "1" ? false : true,
                                    doRollDice[3] == "1" ? false : true,                                    
                                    doRollDice[4] == "1" ? false : true);
                               ev = new YatzyGameEvent(YatzyGameEventType.UserRolled, player);
                               ev.EventArg = "{\"rollState\":" + player.RollState.ToString() + ", \"bestBets\":" + player.ScoreCard.GetBestBets(player.RollState) + "}";
                               foreach (var client in Program.clients)
                               {                                
                                       client.AnnounceEvent(ev.ToString());                                
                               }                            
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
                               
                               foreach (var client in Program.clients)
                               {                                
                                       client.AnnounceEvent(ev.ToString());                                
                               }
                               player = Program.game.Players[index];
                               player.RollState.Reset();
                               player.ItsMyTurn = true;
                               ev = new YatzyGameEvent(YatzyGameEventType.TurnChanged, player);
                               foreach (var client in Program.clients)
                               {
                                   //Console.WriteLine("Sending event to " + (client.clientSocket.Client.RemoteEndPoint as IPEndPoint).Address.ToString());
                                   client.AnnounceEvent(ev.ToString());
                               }
                               
                               
                            }
                            break;
                        case "changeDiceHold":
                            var diceArgs = cmd.args.Split(';');
                            player = Program.game.Players.Find(p => p.UserName == diceArgs[0]);
                            string diceHoldStates = diceArgs[1];

                            if (player != null)
                            {
                                ev = new YatzyGameEvent(YatzyGameEventType.UserChangedDiceHold, player);
                                ev.EventArg = diceHoldStates;
                                                                
                                foreach (var client in Program.clients)
                                {
                                    client.AnnounceEvent(ev.ToString());
                                }                                
                            }
                            break;

                        case "startgame":
                            if (Program.game.Players != null && Program.game.Players.Count > 0)
                            {
                                player = Program.game.Players.Find(p => p.UserName == cmd.args);
                                if (player != null)
                                {
                                    player.ItsMyTurn = true;
                                    ev = new YatzyGameEvent(YatzyGameEventType.TurnChanged, player);
                                    foreach (var client in Program.clients)
                                    {
                                        //Console.WriteLine("Sending event to " + (client.clientSocket.Client.RemoteEndPoint as IPEndPoint).Address.ToString());
                                        client.AnnounceEvent(ev.ToString());
                                    }
                                }
                            }                           
                            break;

                    }
                }
                catch(Exception e)
                {
                    //Console.WriteLine("Command not understood " + browserSent + " Exception: " + e.Message);
                    clientSocket.Close();
                    Program.clients.Remove(this);
                    doQuit = true;
                    Program.game.reset();
                }
                //System.Threading.Thread.Sleep(1000);//wait for message to be sent       
            }
        }

    }
    class Program
    {
        static private string guid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
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
            TcpListener serverSocket = new TcpListener(localAddr, 8087);
            TcpClient clientSocket = default(TcpClient);
            
            int counter = 0;

            serverSocket.Start();
            //Console.WriteLine(" >> " + "Server Started");

            counter = 0;
            while (true)
            {
                counter += 1;
                clientSocket = serverSocket.AcceptTcpClient();
                //Console.WriteLine(" >> " + "Client No:" + Convert.ToString(counter) + " started!");
                handleClient client = new handleClient();
                clients.Add(client);
                client.startClient(clientSocket, Convert.ToString(counter));
            }

            clientSocket.Close();
            serverSocket.Stop();
            //Console.WriteLine(" >> " + "exit");
            
        }

      
        




        public static T[] SubArray<T>(T[] data, int index, int length)
        {
            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }

        public static string AcceptKey(ref string key)
        {
            string longKey = key + guid;
            byte[] hashBytes = ComputeHash(longKey);
            return Convert.ToBase64String(hashBytes);
        }

        static SHA1 sha1 = SHA1CryptoServiceProvider.Create();
        private static byte[] ComputeHash(string str)
        {
            return sha1.ComputeHash(System.Text.Encoding.ASCII.GetBytes(str));
        }

        //Needed to decode frame
        public static string GetDecodedData(byte[] buffer, int length)
        {
            byte b = buffer[1];
            int dataLength = 0;
            int totalLength = 0;
            int keyIndex = 0;

            if (b - 128 <= 125)
            {
                dataLength = b - 128;
                keyIndex = 2;
                totalLength = dataLength + 6;
            }

            if (b - 128 == 126)
            {
                dataLength = BitConverter.ToInt16(new byte[] { buffer[3], buffer[2] }, 0);
                keyIndex = 4;
                totalLength = dataLength + 8;
            }

            if (b - 128 == 127)
            {
                dataLength = (int)BitConverter.ToInt64(new byte[] { buffer[9], buffer[8], buffer[7], buffer[6], buffer[5], buffer[4], buffer[3], buffer[2] }, 0);
                keyIndex = 10;
                totalLength = dataLength + 14;
            }

            if (totalLength > length)
                throw new Exception("The buffer length is smaller than the data length");

            byte[] key = new byte[] { buffer[keyIndex], buffer[keyIndex + 1], buffer[keyIndex + 2], buffer[keyIndex + 3] };

            int dataIndex = keyIndex + 4;
            int count = 0;
            for (int i = dataIndex; i < totalLength; i++)
            {
                buffer[i] = (byte)(buffer[i] ^ key[count % 4]);
                count++;
            }

            return Encoding.ASCII.GetString(buffer, dataIndex, dataLength);
        }

        //function to create  frames to send to client 
        /// <summary>
        /// Enum for opcode types
        /// </summary>
        public enum EOpcodeType
        {
            /* Denotes a continuation code */
            Fragment = 0,

            /* Denotes a text code */
            Text = 1,

            /* Denotes a binary code */
            Binary = 2,

            /* Denotes a closed connection */
            ClosedConnection = 8,

            /* Denotes a ping*/
            Ping = 9,

            /* Denotes a pong */
            Pong = 10
        }

        /// <summary>Gets an encoded websocket frame to send to a client from a string</summary>
        /// <param name="Message">The message to encode into the frame</param>
        /// <param name="Opcode">The opcode of the frame</param>
        /// <returns>Byte array in form of a websocket frame</returns>
        public static byte[] GetFrameFromString(string Message, EOpcodeType Opcode = EOpcodeType.Text)
        {
            byte[] response;
            byte[] bytesRaw = Encoding.Default.GetBytes(Message);
            byte[] frame = new byte[10];

            int indexStartRawData = -1;
            int length = bytesRaw.Length;

            frame[0] = (byte)(128 + (int)Opcode);
            if (length <= 125)
            {
                frame[1] = (byte)length;
                indexStartRawData = 2;
            }
            else if (length >= 126 && length <= 65535)
            {
                frame[1] = (byte)126;
                frame[2] = (byte)((length >> 8) & 255);
                frame[3] = (byte)(length & 255);
                indexStartRawData = 4;
            }
            else
            {
                frame[1] = (byte)127;
                frame[2] = (byte)((length >> 56) & 255);
                frame[3] = (byte)((length >> 48) & 255);
                frame[4] = (byte)((length >> 40) & 255);
                frame[5] = (byte)((length >> 32) & 255);
                frame[6] = (byte)((length >> 24) & 255);
                frame[7] = (byte)((length >> 16) & 255);
                frame[8] = (byte)((length >> 8) & 255);
                frame[9] = (byte)(length & 255);

                indexStartRawData = 10;
            }

            response = new byte[indexStartRawData + length];

            int i, reponseIdx = 0;

            //Add the frame bytes to the reponse
            for (i = 0; i < indexStartRawData; i++)
            {
                response[reponseIdx] = frame[i];
                reponseIdx++;
            }

            //Add the data bytes to the response
            for (i = 0; i < length; i++)
            {
                response[reponseIdx] = bytesRaw[i];
                reponseIdx++;
            }

            return response;
        }
    }
}