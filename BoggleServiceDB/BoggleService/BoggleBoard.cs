// Written by Joe Zachary for CS 3500, November 2012.

using System;

namespace Boggle
{
    /// <summary>
    /// Represents a Boggle board.
    /// </summary>
    public static class BoggleBoard
    {
        // The 16 cubes that make up a standard Boggle Board
        private static string[] cubes =
            {
                "LRYTTE",
                "ANAEEG",
                "AFPKFS",
                "YLDEVR",
                "VTHRWE",
                "IDSYTT",
                "XLDERI",
                "ZNRNHL",
                "EGHWNE",
                "OATTOW",
                "HCPOAS",
                "OBBAOJ",
                "SEOTIS",
                "MTOICU",
                "ENSIEU",
                "NMIQHU"
            };

        /// <summary>
        /// Creates a randomly-generated BoggleBoard 
        /// </summary>
        public static string GenerateBoard()
        {
            // Shuffle the cubes
            Random r = new Random();
            for (int i = cubes.Length - 1; i >= 0; i--)
            {
                int j = r.Next(i + 1);
                string temp = cubes[i];
                cubes[i] = cubes[j];
                cubes[j] = temp;
            }

            // Make a string by choosing one character at random
            // frome each cube.
            string letters = "";
            for (int i = 0; i < cubes.Length; i++)
            {
                letters += cubes[i][r.Next(6)];
            }

            // Make the board
            return letters.ToUpper();
        }

        /// <summary>
        /// Makes a board from the 16-letter string
        /// </summary>
        private static char[,] MakeBoard(string letters)
        {
            letters = letters.ToUpper();
            char[,] board = new char[4, 4];
            int index = 0;
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    board[i, j] = letters[index++];
                }
            }
            return board;
        }


        /// <summary>
        /// Reports whether the provided word can be formed by tracking through
        /// this Boggle board as described in the rules of Boggle.  The method
        /// is case-insensitive.
        /// </summary>
        public static bool CanBeFormed(string word, string letters)
        {
            char[,] board = MakeBoard(letters);

            // Work in upper case
            word = word.ToUpper();

            // Mark every square on the board as unvisited.
            bool[,] visited = new bool[4, 4];

            // See if there is any starting point on the board from which
            // the word can be formed.
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    if (CanBeFormed(word, i, j, board, visited))
                    {
                        return true;
                    }
                }
            }

            // If no starting point worked, return false.
            return false;
        }


        /// <summary>
        /// Reports whether the provided word can be formed by tracking through
        /// this Boggle board by beginning at location [i,j] and avoiding any
        /// squares marked as visited.
        /// </summary>
        private static bool CanBeFormed(string word, int i, int j, char[,] board, bool[,] visited)
        {
            // If the word is empty, report success.
            if (word.Length == 0)
            {
                return true;
            }

            // If an index is out of bounds, report failure.
            if (i < 0 || i >= 4 || j < 0 || j >= 4)
            {
                return false;
            }

            // If this square has already been visited, report failure.
            if (visited[i, j])
            {
                return false;
            }

            // If the first letter of the word doesn't match the letter on
            // this square, report failure.  Otherwise, obtain the remainder
            // of the word that we should match next.
            // (Note that Q gets special treatment.)

            char firstChar = word[0];
            string rest = word.Substring(1);

            if (firstChar != board[i, j])
            {
                return false;
            }

            if (firstChar == 'Q')
            {
                if (rest.Length == 0)
                {
                    return false;
                }
                if (rest[0] != 'U')
                {
                    return false;
                }
                rest = rest.Substring(1);
            }

            // Mark this square as visited.
            visited[i, j] = true;

            // Try to match the remainder of the word, beginning at a neighboring square.
            if (CanBeFormed(rest, i - 1, j - 1, board, visited)) return true;
            if (CanBeFormed(rest, i - 1, j, board, visited)) return true;
            if (CanBeFormed(rest, i - 1, j + 1, board, visited)) return true;
            if (CanBeFormed(rest, i, j - 1, board, visited)) return true;
            if (CanBeFormed(rest, i, j + 1, board, visited)) return true;
            if (CanBeFormed(rest, i + 1, j - 1, board, visited)) return true;
            if (CanBeFormed(rest, i + 1, j, board, visited)) return true;
            if (CanBeFormed(rest, i + 1, j + 1, board, visited)) return true;

            // We failed.  Unmark this square and return false.
            visited[i, j] = false;
            return false;
        }
    }
}
