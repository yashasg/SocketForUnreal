using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Boggle
{
    public class Name
    {
        public string Nickname { get; set; }
    }

    public class User
    {
        public string UserToken { get; set; }
    }

    public class GameRequest
    {
        public string UserToken { get; set; }
        public int TimeLimit { get; set; }
    }

    public class Game
    {
        public string GameID { get; set; }
    }

    public class PlayedWord
    {
        public string UserToken { get; set; }

        public string Word { get; set; }
    }

    public class WordScore
    {
        public int Score { get; set; }
    }

    [DataContract]
    public class Status
    {
        [DataMember]
        public string GameState { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string Board { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int? TimeLimit { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int? TimeLeft { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public Player Player1 { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public Player Player2 { get; set; }
    }

    [DataContract]
    public class Player
    {
        [DataMember(EmitDefaultValue = false)]
        public string Nickname { get; set; }

        [DataMember]
        public int Score { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public List<WordAndScore> WordsPlayed { get; set; }
    }

    public class WordAndScore
    {
        public string Word { get; set; }

        public int Score { get; set; }
    }
}