using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.ServiceModel.Web;
using static System.Net.HttpStatusCode;

namespace Boggle
{
    public class BoggleService : IBoggleService
    {
        private readonly object sync = new object();
        private static Dictionary<string, string> users = new Dictionary<string, string>();
        private static Dictionary<string, BoggleGame> games = new Dictionary<string, BoggleGame>();
        private static ISet<string> dictionary = new HashSet<string>();
        private static string pendingGame = "G0";
        private static string pendingPlayer = null;
        private static int pendingTimeLimit = 0;

        static BoggleService()
        {
            using (StreamReader words = new StreamReader(AppDomain.CurrentDomain.BaseDirectory + @"dictionary.txt"))
            {
                string word;
                while ((word = words.ReadLine()) != null)
                {
                    dictionary.Add(word.ToUpper());
                }
            }

        }

        private static void SetStatus(HttpStatusCode status)
        {
            WebOperationContext.Current.OutgoingResponse.StatusCode = status;
        }

        public Stream API()
        {
            WebOperationContext.Current.OutgoingResponse.ContentType = "text/html";
            return File.OpenRead(AppDomain.CurrentDomain.BaseDirectory + "index.html");
        }

        public User MakeUser(Name data)
        {
            lock (sync)
            {
                if (data.Nickname == null || data.Nickname.Trim() == "")
                {
                    SetStatus(Forbidden);
                    return null;
                }
                else
                {
                    string guid = Guid.NewGuid().ToString();
                    users[guid] = data.Nickname.Trim();
                    SetStatus(Created);
                    return new User() { UserToken = guid };
                }
            }
        }

        public Game JoinGame(GameRequest data)
        {
            lock (sync)
            {
                if (data.UserToken == null || !users.ContainsKey(data.UserToken) || data.TimeLimit < 5 || data.TimeLimit > 120)
                {
                    SetStatus(Forbidden);
                    return null;
                }
                else if (data.UserToken == pendingPlayer)
                {
                    SetStatus(Conflict);
                    return null;
                }
                else if (pendingPlayer == null)
                {
                    pendingPlayer = data.UserToken;
                    pendingTimeLimit = data.TimeLimit;
                    SetStatus(Accepted);
                    return new Game() { GameID = pendingGame };
                }
                else
                {
                    games[pendingGame] = new BoggleGame(pendingPlayer, data.UserToken, dictionary, (pendingTimeLimit + data.TimeLimit) / 2);
                    pendingPlayer = null;
                    Game result = new Game() { GameID = pendingGame };
                    pendingGame = "G" + (Int32.Parse(pendingGame.Substring(1)) + 1);
                    SetStatus(Created);
                    return result;
                }
            }
        }

        public void CancelGame(User data)
        {
            lock (sync)
            {
                if (data.UserToken == null || data.UserToken != pendingPlayer)
                {
                    SetStatus(Forbidden);
                }
                else
                {
                    pendingPlayer = null;
                    SetStatus(OK);
                }
            }
        }

        public WordScore PlayWord(PlayedWord data, string gameID)
        {
            lock (sync)
            {
                if (gameID == null || data.UserToken == null || !users.ContainsKey(data.UserToken) || !games.ContainsKey(gameID)
                    || data.Word == null || data.Word.Trim().Length == 0)
                {
                    SetStatus(Forbidden);
                    return null;
                }

                BoggleGame game = games[gameID];
                if (game.GameState != "active")
                {
                    SetStatus(Conflict);
                    return null;
                }

                try
                {
                    int score = game.PlayWord(data.UserToken, data.Word.Trim());
                    SetStatus(OK);
                    return new WordScore() { Score = score };
                }
                catch (Exception)
                {
                    SetStatus(Forbidden);
                    return null;
                }
            }
        }

        public Status Status(string gameID, string brief)
        {
            lock (sync)
            {
                BoggleGame game;
                if (gameID == pendingGame)
                {
                    Status status = new Status();
                    status.GameState = "pending";
                    SetStatus(OK);
                    return status;
                }

                else if (!games.TryGetValue(gameID, out game))
                {
                    SetStatus(Forbidden);
                    return null;
                }
                else
                {
                    Status status = new Status();
                    status.GameState = game.GameState;

                    status.TimeLeft = game.TimeLeft;
                    status.Player1 = new Player();
                    status.Player1.Score = game.Score1;
                    status.Player2 = new Player();
                    status.Player2.Score = game.Score2;

                    if (brief != "yes")
                    {
                        status.Board = game.Board;
                        status.TimeLimit = game.TimeLimit;
                        status.Player1.Nickname = users[game.Player1];
                        status.Player2.Nickname = users[game.Player2];

                        if (game.GameState != "active")
                        {
                            status.Player1.WordsPlayed = game.Words1;
                            status.Player2.WordsPlayed = game.Words2;
                        }
                    }
                    return status;
                }
            }
        }
    }
}
