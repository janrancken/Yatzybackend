using System;
using System.Collections.Generic;
namespace WebSocketYatzy
{

    public class YatzyGame
    {
        public List<YatzyPlayer> Players { get; set; }
        public YatzyGame()
        {
            this.Players = new List<YatzyPlayer>();
        }

        public void reset()
        {
            this.Players = new List<YatzyPlayer>();
        }


        public override string ToString()
        {
            var ret = "{\"players\": [";
            foreach(var player in this.Players)
            {
                ret += player.ToString() + ",";
            }
            if (ret.LastIndexOf(',') == ret.Length - 1)
                ret = ret.Substring(0, ret.Length - 1);
            ret += "]}";
            return ret;

        }



        
    }

    
    public class YatzyScoreCard
{
    public YatzyScoreCard()
    {
        this.Score = EmptyScore(); 
    }

    public Dictionary<string,int> EmptyScore()
    {
        var r = new Dictionary<string, int>();
        r.Add("ones", -1);
        r.Add("twos", -1);
        r.Add("threes", -1);
        r.Add("fours", -1);
        r.Add("fives", -1);
        r.Add("sixes", -1);
        r.Add("onepair", -1);
        r.Add("twopair", -1);
        r.Add("tripple", -1);
        r.Add("quadruple", -1);
        r.Add("fullhouse", -1);
        r.Add("smallstrait", -1);
        r.Add("largestrait", -1);
        r.Add("chance", -1);
        r.Add("yatzy", -1);
        return r;
    }

    public Dictionary<String, int> Score { get; set; }

    
    public int CurrentScore
    {
        get
        {
            return (this.Score["ones"] == -1 ? 0 : this.Score["ones"]) +
                (this.Score["twos"] == -1 ? 0 : this.Score["twos"]) +
                (this.Score["threes"] == -1 ? 0 : this.Score["threes"]) +
                (this.Score["fours"] == -1 ? 0 : this.Score["fours"]) +
                (this.Score["fives"] == -1 ? 0 : this.Score["fives"]) +
                (this.Score["sixes"] == -1 ? 0 : this.Score["sixes"]) +
                (this.Score["onepair"] == -1 ? 0 : this.Score["onepair"]) +
                (this.Score["twopair"] == -1 ? 0 : this.Score["twopair"]) +
                (this.Score["tripple"]== -1 ? 0 : this.Score["tripple"]) +
                (this.Score["quadruple"] == -1 ? 0 : this.Score["quadruple"]) +
                (this.Score["fullhouse"] == -1 ? 0 : this.Score["fullhouse"]) +
                (this.Score["smallstrait"] == -1 ? 0 : this.Score["smallstrait"]) +
                (this.Score["largestrait"] == -1 ? 0 : this.Score["largestrait"]) +
                (this.Score["yatzy"] == -1 ? 0 : this.Score["yatzy"]) +
                (this.Score["chance"] == -1 ? 0 : this.Score["chance"]);
        }
    }
    public override string ToString()
    {
        return "{" +
            "\"ones\":" + this.Score["ones"].ToString() + "," +
            "\"twos\":" + this.Score["twos"].ToString() + "," +
            "\"threes\":" + this.Score["threes"].ToString() + "," +
            "\"fours\":" + this.Score["fours"].ToString() + "," +
            "\"fives\":" + this.Score["fives"].ToString() + "," +
            "\"sixes\":" + this.Score["sixes"].ToString() + "," +
            "\"onepair\":" + this.Score["onepair"].ToString() + "," +
            "\"twopair\":" + this.Score["twopair"].ToString() + "," +
            "\"tripple\":" + this.Score["tripple"].ToString() + "," +
            "\"quadruple\":" + this.Score["quadruple"].ToString() + "," +
            "\"fullhouse\":" + this.Score["fullhouse"].ToString() + "," +
            "\"smallstrait\":" + this.Score["smallstrait"].ToString() + "," +
            "\"largestrait\":" + this.Score["largestrait"].ToString() + "," +
            "\"yatzy\":" + this.Score["yatzy"].ToString() + "," +
            "\"chance\":" + this.Score["chance"].ToString() + "}";            
    }



    public bool MarkScore(string markAs, YatzyRollState rollState)
    {
        int v = this.BestScore(markAs, rollState);
        if (v >= 0)
        {
            this.Score[markAs] = v;
            return true;
        }
        return false;
    }

    public int BestScore(string markAs, YatzyRollState rollState)
    {
        bool largestOk = false;
        switch (markAs)
        {
            case "ones":
                if (this.Score["ones"] >= 0)
                    return -1;
                return rollState.DiceStates.FindAll(ds => ds.Value == 1).Count;

            case "twos":
                if (this.Score["twos"] >= 0)
                    return -1;
                return rollState.DiceStates.FindAll(ds => ds.Value == 2).Count * 2;

            case "threes":
                if (this.Score["threes"] >= 0)
                    return -1;
                return rollState.DiceStates.FindAll(ds => ds.Value == 3).Count * 3;
            case "fours":
                if (this.Score["fours"] >= 0)
                    return -1;
                return rollState.DiceStates.FindAll(ds => ds.Value == 4).Count * 4;

            case "fives":
                if (this.Score["fives"]>= 0)
                    return -2;
                return rollState.DiceStates.FindAll(ds => ds.Value == 5).Count * 5;
            case "sixes":
                if (this.Score["sixes"]>= 0)
                    return -1;
                return rollState.DiceStates.FindAll(ds => ds.Value == 6).Count * 6;
            case "onepair":
                if (this.Score["onepair"]>= 0)
                    return -1;
                for (var largest = 6; largest > 0; largest--)
                {
                    largestOk = rollState.DiceStates.FindAll(ds => ds.Value == largest).Count >= 2;
                    if (largestOk)
                    {
                        return largest * 2;
                    }
                }
                return 0;
            case "tripple":
                if (this.Score["tripple"]>= 0)
                    return -1;
                for (var largest = 6; largest > 0; largest--)
                {
                    largestOk = rollState.DiceStates.FindAll(ds => ds.Value == largest).Count >= 3;
                    if (largestOk)
                    {
                        return largest * 3;
                    }
                }
                return 0;
            case "quadruple":
                if (this.Score["quadruple"]>= 0)
                    return -2;
                for (var largest = 6; largest > 0; largest--)
                {
                    largestOk = rollState.DiceStates.FindAll(ds => ds.Value == largest).Count >= 4;
                    if (largestOk)
                    {
                        return largest * 4;
                    }
                }
                return 0;
            case "yatzy":
                if (this.Score["yatzy"]>= 0)
                    return -1;
                for (var largest = 6; largest > 0; largest--)
                {
                    largestOk = rollState.DiceStates.FindAll(ds => ds.Value == largest).Count >= 5;
                    if (largestOk)
                    {
                        return 50;
                    }
                }
                return 0;
            case "smallstrait":
                if (this.Score["smallstrait"]>= 0)
                    return -1;
                if (
                    rollState.DiceStates.FindAll(ds => ds.Value == 1).Count == 1 &&
                    rollState.DiceStates.FindAll(ds => ds.Value == 2).Count == 1 &&
                    rollState.DiceStates.FindAll(ds => ds.Value == 3).Count == 1 &&
                    rollState.DiceStates.FindAll(ds => ds.Value == 4).Count == 1 &&
                    rollState.DiceStates.FindAll(ds => ds.Value == 5).Count == 1)
                {
                    return 15;

                }
                return 0;

            case "largestrait":
                if (this.Score["largestrait"]>= 0)
                    return -1;
                if (
                    rollState.DiceStates.FindAll(ds => ds.Value == 2).Count == 1 &&
                    rollState.DiceStates.FindAll(ds => ds.Value == 3).Count == 1 &&
                    rollState.DiceStates.FindAll(ds => ds.Value == 4).Count == 1 &&
                    rollState.DiceStates.FindAll(ds => ds.Value == 5).Count == 1 &&
                    rollState.DiceStates.FindAll(ds => ds.Value == 6).Count == 1)
                {
                    return 20;
                }
                return 0;
            case "fullhouse":
                if (this.Score["fullhouse"]>= 0)
                    return -1;
                for (var largest = 6; largest > 0; largest--)
                {
                    largestOk = rollState.DiceStates.FindAll(ds => ds.Value == largest).Count >= 3;
                    if (largestOk)
                    {
                        var largest1 = largest;
                        largestOk = false;
                        for (var largest2 = 6; largest2 > 0; largest2--)
                        {
                            largestOk = rollState.DiceStates.FindAll(ds => ds.Value == largest2 && ds.Value != largest1).Count >= 2;
                            if (largestOk)
                            {
                                return largest1 * 3 + largest2 * 2;
                            }
                        }
                    }
                }
                return 0;

            case "twopair":
                if (this.Score["twopair"]>= 0)
                    return -1;
                for (var largest = 6; largest > 0; largest--)
                {
                    largestOk = rollState.DiceStates.FindAll(ds => ds.Value == largest).Count >= 2;
                    if (largestOk)
                    {
                        var largest1 = largest;
                        largestOk = false;
                        for (var largest2 = 6; largest2 > 0; largest2--)
                        {
                            largestOk = rollState.DiceStates.FindAll(ds => ds.Value == largest2 && ds.Value != largest1).Count >= 2;
                            if (largestOk)
                            {
                                return largest1 * 2 + largest2 * 2;
                            }
                        }
                    }
                }
                return 0;
            case "chance":
                if (this.Score["chance"]>= 0)
                    return -1;
                return rollState.DiceStates[0].Value +
                    rollState.DiceStates[1].Value +
                    rollState.DiceStates[2].Value +
                    rollState.DiceStates[3].Value +
                    rollState.DiceStates[4].Value;


        }

        return 0;


    }

    public string GetBestBets(YatzyRollState rollState)
    {
        return "{" +
            "\"ones\":" + this.BestScore("ones",rollState) + "," +
            "\"twos\":" + this.BestScore("twos",rollState) + "," +
            "\"threes\":" + this.BestScore("threes",rollState) + "," +
            "\"fours\":" + this.BestScore("fours",rollState) + "," +
            "\"fives\":" + this.BestScore("fives",rollState) + "," +
            "\"sixes\":" + this.BestScore("sixes",rollState) + "," +
            "\"onepair\":" + this.BestScore("onepair",rollState) + "," +
            "\"twopair\":" + this.BestScore("twopair",rollState) + "," +
            "\"tripple\":" + this.BestScore("tripple",rollState) + "," +
            "\"quadruple\":" + this.BestScore("quadruple",rollState) + "," +
            "\"fullhouse\":" + this.BestScore("fullhouse",rollState) + "," +
            "\"smallstrait\":" + this.BestScore("smallstrait",rollState) + "," +
            "\"largestrait\":" + this.BestScore("largestrait",rollState) + "," +
            "\"yatzy\":" + this.BestScore("yatzy",rollState) + "," +
            "\"chance\":" + this.BestScore("chance",rollState) + "}";
    }

   }
    public enum YatzyGameEventType
    {
        UserJoined,
        UserLeft,
        UserNameChanged,
        UserRolled,
        UserScored,
        UserChangedDiceHold,
        TurnChanged,
        GameInfo
    }

    public class YatzyGameEvent
    {
        public YatzyGameEventType Event { get; set; }
        public YatzyPlayer Player { get; set; }
        public string EventArg {get;set;}
        public override string ToString()
        {
            if(this.EventArg != null && this.EventArg.Length > 0)
              return "{\"event\":\"" + this.Event.ToString() + "\", \"player\":\"" + this.Player.UserName + "\",\"clientId\":\"" + this.Player.UserGuid.ToString() + "\",\"eventArg\":" + this.EventArg + "}";
            else
                return "{\"event\":\"" + this.Event.ToString() + "\", \"player\":\"" + this.Player.UserName + "\",\"clientId\":\"" + this.Player.UserGuid.ToString() + "\"}";
        }
        public YatzyGameEvent(YatzyGameEventType t, YatzyPlayer p)
        {
            this.Event = t;
            this.Player = p;
        }
        public YatzyGameEvent(YatzyGameEventType t, YatzyPlayer p, string arg)
        {
            this.Event = t;
            this.Player = p;
            this.EventArg = arg;
        }
    }
    public class YatzyDiceState
    {
        public int Value { get; set; }
        public bool IsHeld { get; set; }
        public YatzyDiceState()
        {
            this.Value = 0;
            this.IsHeld = false;
        }
    }
    public class YatzyRollState
    {
        public List<YatzyDiceState> DiceStates {get;set;}
        
        public int RollsLeft {get;set;}
        private Random r = new Random();

        public YatzyRollState()
        {
            this.RollsLeft = 3;
            this.DiceStates = new List<YatzyDiceState>();
            this.DiceStates.Add(new YatzyDiceState());
            this.DiceStates.Add(new YatzyDiceState());
            this.DiceStates.Add(new YatzyDiceState());
            this.DiceStates.Add(new YatzyDiceState());
            this.DiceStates.Add(new YatzyDiceState());            
        }

        public void Reset()
        {
            foreach(var d in this.DiceStates)
            {
                d.Value = 0;
                d.IsHeld = false;
                
            }
            this.RollsLeft = 3;
        }
        public override string ToString()
        {
            return "{\"rollsLeft\":" + this.RollsLeft + "," +
                "\"d1\": {\"value\":" + this.DiceStates[0].Value.ToString() + ",\"isHeld\":" + this.DiceStates[0].IsHeld.ToString().ToLower() + "}," +
                "\"d2\": {\"value\":" + this.DiceStates[1].Value.ToString() + ",\"isHeld\":" + this.DiceStates[1].IsHeld.ToString().ToLower() + "}," +
                "\"d3\": {\"value\":" + this.DiceStates[2].Value.ToString() + ",\"isHeld\":" + this.DiceStates[2].IsHeld.ToString().ToLower() + "}," +
                "\"d4\": {\"value\":" + this.DiceStates[3].Value.ToString() + ",\"isHeld\":" + this.DiceStates[3].IsHeld.ToString().ToLower() + "}," +
                "\"d5\": {\"value\":" + this.DiceStates[4].Value.ToString() + ",\"isHeld\":" + this.DiceStates[4].IsHeld.ToString().ToLower() + "}}";       
        }

        public void Roll(bool die1, bool die2, bool die3, bool die4, bool die5)
        {
            if (this.RollsLeft == 0)
                return;
            if (die1 || this.RollsLeft == 3)
            {
                this.DiceStates[0].Value = r.Next(1, 7);
                this.DiceStates[0].IsHeld = false;
            }
            else
            {
                this.DiceStates[0].IsHeld = true;
            }
            if (die2 || this.RollsLeft == 3)
            {
                this.DiceStates[1].Value = r.Next(1, 7);
                this.DiceStates[1].IsHeld = false;
            }
            else
            {
                this.DiceStates[1].IsHeld = true;
            }
            if (die3 || this.RollsLeft == 3)
            {
                this.DiceStates[2].Value = r.Next(1, 7);
                this.DiceStates[2].IsHeld = false;
            }
            else
            {
                this.DiceStates[2].IsHeld = true;
            }
            if (die4 || this.RollsLeft == 3)
            {
                this.DiceStates[3].Value = r.Next(1, 7);
                this.DiceStates[3].IsHeld = false;
            }
            else
            {
                this.DiceStates[3].IsHeld = true;
            }
            if (die5 || this.RollsLeft == 3)
            {
                this.DiceStates[4].Value = r.Next(1, 7);
                this.DiceStates[4].IsHeld = false;
            }
            else
            {
                this.DiceStates[4].IsHeld = true;
            }            
            this.RollsLeft--;
        }
    }
    public class YatzyPlayer
    {
        public string UserName{get;set;}
        public string IPAddress { get; set; }
        public YatzyScoreCard ScoreCard { get; set; }
        public YatzyRollState RollState { get; set; }
        public bool ItsMyTurn { get; set; }
        public Guid UserGuid { get; set; }
        public YatzyPlayer()
        {
           
        }
        public YatzyPlayer(string username, string ip)
        {
            this.UserName = username;
            this.IPAddress = ip;
            this.ScoreCard = new YatzyScoreCard();
            this.RollState = new YatzyRollState();
            this.ItsMyTurn = false;
            this.UserGuid = Guid.NewGuid();
            Console.WriteLine("client id: " + this.UserGuid.ToString());
        }

        public override string ToString()
        {
            return "{\"userName\":\"" + this.UserName + "\",\"clientId\":\"" + this.UserGuid + "\",\"score\":" + this.ScoreCard.ToString() + ",\"itsHisTurn\":" + this.ItsMyTurn.ToString().ToLower() + ",\"rollsLeft\":" + this.RollState.RollsLeft + "}";
        }
    }

}