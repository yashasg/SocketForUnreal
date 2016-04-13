using System;
using System.Collections.Generic;
using System.Linq;

namespace Boggle
{
    public static class BoggleGame
    {
        //private ISet<string> dictionary;
        //private BoggleBoard board;
        //private DateTime startTime;
        
        public static int GetTimeLeft (DateTime startTime, int timeLimit)
        {
            int remainder = (int)Math.Floor((DateTime.Now - startTime).TotalSeconds);
            return (remainder >= timeLimit) ? 0 : timeLimit - remainder;
        }

        public static int WordValue(string word, string board, ISet<string> dictionary)
        {
            if (word == null)
            {
                return -2;
            }

            else if (word.Length < 3)
            {
                return 0;
            }

            else if (!dictionary.Contains(word.ToUpper()))
            {
                return -1;
            }

            else if (BoggleBoard.CanBeFormed(word, board))
            {
                if (word.Length <= 4) return 1;
                else if (word.Length == 5) return 2;
                else if (word.Length == 6) return 3;
                else if (word.Length == 7) return 5;
                else return 11;
            }

            else
            {
                return -1;
            }
        }
    }
}