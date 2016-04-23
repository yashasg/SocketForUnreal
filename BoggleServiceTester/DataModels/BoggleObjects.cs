// Boggle Objects for PS9 written by Dave Heyborne (U0459350) and Hoon Ik Cho (U0713654).

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace BoggleService
{
    /// <summary>
    ///     Represents a player in a game.
    /// </summary>
    [DataContract]
    public sealed class Player
    {
        /// <summary>
        ///     The player's nickname.
        /// </summary>
        [DataMember]
        public string nickname { get; set; }

        /// <summary>
        ///     The player's score.
        /// </summary>
        [DataMember]
        public int score { get; set; }

        /// <summary>
        ///     A list of the words the player has played, as well as the score for each word.
        /// </summary>
        [DataMember]
        public List<WordScorePair> wordsPlayed { get; set; }
    }

    /// <summary>
    ///     Represents a word paired with the score for playing that word.
    /// </summary>
    [DataContract]
    public sealed class WordScorePair
    {
        /// <summary>
        ///     A word that was played in a game.
        /// </summary>
        [DataMember]
        public string word { get; set; }

        /// <summary>
        ///     The score awarded for playing the word in the game.
        /// </summary>
        [DataMember]
        public int score { get; set; }

        /// <summary>
        /// Custom equality that ignores case
        /// </summary>
        public override bool Equals(object o)
        {
            if (o is WordScorePair)
            {
                WordScorePair w = o as WordScorePair;
                return w.word.ToUpper() == this.word.ToUpper() &&
                    w.score == this.score;
            }
            return false;
        }

        /// <summary>
        /// Custom == operator
        /// </summary>
        public static bool operator ==(WordScorePair w1, WordScorePair w2)
        {
            if (ReferenceEquals(w1, null))
            {
                return ReferenceEquals(w2, null);
            }
            else if (ReferenceEquals(w2, null))
            {
                return ReferenceEquals(w1, null);
            }
            else
            {
                return w1.Equals(w2);
            }
        }

        /// <summary>
        /// Custom != operator
        /// </summary>
        public static bool operator !=(WordScorePair w1, WordScorePair w2)
        {
            return !(w1 == w2);
        }

        /// <summary>
        /// Custom hash code
        /// </summary>
        public override int GetHashCode()
        {
            return word.GetHashCode() ^ score.GetHashCode();
        }

    }

    /// <summary>
    ///     An object which is passed as part of a POST request when instructing the <see cref = "BoggleService" /> to create a new user.
    /// </summary>
    [DataContract]
    public sealed class MakeUserPOST
    {
        /// <summary>
        ///     The nickname to be associated with the newly-created user.
        /// </summary>
        [DataMember]
        public string nickname { get; set; }
    }

    /// <summary>
    ///     An object which is returned from a POST request instructing the <see cref = "BoggleService" /> to create a new user.
    /// </summary>
    [DataContract]
    public sealed class MakeUserResponse
    {
        /// <summary>
        ///     A unique token corresponding to the new user that was created.
        /// </summary>
        [DataMember]
        public string userToken { get; set; }
    }

    /// <summary>
    ///     An object which is passed as part of a POST request when instructing the <see cref = "BoggleService" /> to let a user join a game.
    /// </summary>
    [DataContract]
    public sealed class JoinGamePOST
    {
        /// <summary>
        ///     A unique token identifying a registered user that wishes to join a game.
        /// </summary>
        [DataMember]
        public string userToken { get; set; }
    }

    /// <summary>
    ///     An object which is returned from a POST request instructing the <see cref = "BoggleService" /> to let a user join a game.
    /// </summary>
    [DataContract]
    public sealed class JoinGameResponse
    {
        /// <summary>
        ///     A unique token corresponding to the game that the user joined.
        /// </summary>
        [DataMember]
        public string gameToken { get; set; }
    }

    /// <summary>
    ///     An object which is returned from a GET request instructing the <see cref = "BoggleService" /> to provide detailed information about a game.
    /// </summary>
    [DataContract]
    public sealed class FullStatus
    {
        /// <summary>
        ///     The status of the game the service is providing information about.
        /// </summary>
        [DataMember]
        public string gameStatus { get; set; }

        /// <summary>
        ///     The string that represents the board in the game the service is providing information about.
        /// </summary>
        [DataMember]
        public string board { get; set; }

        /// <summary>
        ///     The amount of time originally available in the game the service is providing information about.
        /// </summary>
        [DataMember]
        public int timelimit { get; set; }

        /// <summary>
        ///     The time left in the game the service is providing information about.
        /// </summary>
        [DataMember]
        public int timeleft { get; set; }

        /// <summary>
        ///     The first player in the game the service is providing information about.
        /// </summary>
        [DataMember]
        public Player player1 { get; set; }

        /// <summary>
        ///     The second player in the game the service is providing information about.
        /// </summary>
        [DataMember]
        public Player player2 { get; set; }
    }

    /// <summary>
    ///     An object which is returned from a GET request instructing the <see cref = "BoggleService" /> to provide brief information about a game.
    /// </summary>
    [DataContract]
    public sealed class BriefStatus
    {
        /// <summary>
        ///     The status of the game the service is providing information about.
        /// </summary>
        [DataMember]
        public string gameStatus { get; set; }

        /// <summary>
        ///     The time left in the game the service is providing information about.
        /// </summary>
        [DataMember]
        public int timeleft { get; set; }

        /// <summary>
        ///     The score of the first player in the game the service is providing information about.
        /// </summary>
        [DataMember]
        public int score1 { get; set; }

        /// <summary>
        ///     The score of the second player in the game the service is providing information about.
        /// </summary>
        [DataMember]
        public int score2 { get; set; }
    }

    /// <summary>
    ///     An object which is passed as part of a POST request when instructing the <see cref = "BoggleService" /> to let a user play a word in a game.
    /// </summary>
    [DataContract]
    public sealed class PlayWordPOST
    {
        /// <summary>
        ///     A unique token corresponding to the user that wishes to play a word.
        /// </summary>
        [DataMember]
        public string playerToken { get; set; }

        /// <summary>
        ///     A unique token corresponding to the game the word should be played in.
        /// </summary>
        [DataMember]
        public string gameToken { get; set; }

        /// <summary>
        ///     The word the player wants to play.
        /// </summary>
        [DataMember]
        public string word { get; set; }
    }

    /// <summary>
    ///     An object which is returned from a POST request instructing the <see cref = "BoggleService" /> to let a user play a word in a game.
    /// </summary>
    [DataContract]
    public sealed class PlayWordResponse
    {
        /// <summary>
        ///     The score of the word that was played.
        /// </summary>
        [DataMember]
        public int wordScore { get; set; }
    }
}
