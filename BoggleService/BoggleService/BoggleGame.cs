using System;
using System.Collections.Generic;
using System.Linq;

namespace Boggle
{
    public class BoggleGame
    {
        private ISet<string> dictionary;
        private BoggleBoard board;
        private DateTime startTime;
        
        public string GameState
        {
            get
            {
                return (TimeLeft > 0) ? "active" : "completed";
            }
        }

        public string Board
        {
            get
            {
                return board.ToString();
            }
        }

        public int TimeLimit { get; private set; }

        public int TimeLeft
        {
            get
            {
                int remainder = (int)Math.Floor((DateTime.Now - startTime).TotalSeconds);
                return (remainder >= TimeLimit) ? 0 : TimeLimit - remainder;
            }
        }

        public string Player1
        {
            get; private set;
        }

        public string Player2
        {
            get; private set;
        }

        public List<WordAndScore> Words1
        {
            get; private set;
        }

        public List<WordAndScore> Words2
        {
            get; private set;
        }

        public int Score1
        {
            get
            {
                return Words1.Sum(p => p.Score);
            }
        }

        public int Score2
        {
            get
            {
                return Words2.Sum(p => p.Score);
            }
        }

        public BoggleGame(string player1, string player2, ISet<string> dictionary, int timeLimit)
        {
            this.dictionary = dictionary;
            this.Player1 = player1;
            this.Player2 = player2;
            Words1 = new List<WordAndScore>();
            Words2 = new List<WordAndScore>();
            TimeLimit = timeLimit;
            startTime = DateTime.Now;
            board = new BoggleBoard();
        }

        public int PlayWord(string userToken, string word)
        {
            if (userToken == null || word == null)
            {
                throw new ArgumentNullException();
            }
            else if (userToken != Player1 && userToken != Player2)
            {
                throw new ArgumentOutOfRangeException();
            }

            List<WordAndScore> words;
            if (userToken == Player1)
            {
                words = Words1;
            }
            else
            {
                words = Words2;
            }

            int score;
            if (word.Length < 3)
            {
                score = 0; 
            }

            else if (words.Find(p => p.Word == word) != null)
            {
                score = 0;
            }

            else if (!dictionary.Contains(word.ToUpper()))
            {
                score = -1;
            }

            else if (board.CanBeFormed(word))
            {
                if (word.Length <= 4) score = 1;
                else if (word.Length == 5) score = 2;
                else if (word.Length == 6) score = 3;
                else if (word.Length == 7) score = 5;
                else score = 11;
            }

            else
            {
                score = -1;
            }

            words.Add(new WordAndScore() { Word = word, Score = score });
            return score;
        }
    }
}