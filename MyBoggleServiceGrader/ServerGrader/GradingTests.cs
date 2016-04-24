using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.Http;
using Boggle;
using System.Dynamic;
using Newtonsoft.Json;
using static System.Net.HttpStatusCode;
using System.Threading.Tasks;
using System.Text;
using System.Net;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace ServerGrader
{
    /// <summary>
    /// NOTE:  The service must already be running elsewhere, such as in a separate Visual Studio
    /// or on a remote server, before these tests are run.  When the tests are started, the pending
    /// game should contain NO players.
    /// 
    /// For best results, run these tests against a server to which you have exlusive access.
    /// Othewise, competing users may interfere with the tests.
    /// </summary>
    [TestClass]
    public class GradingTests
    {
        /// <summary>
        /// Creates an HttpClient for communicating with the boggle server.
        /// </summary>
        private static HttpClient CreateClient()
        {
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri("http://localhost:60000");
            //client.BaseAddress = new Uri("http://bogglecs3500s16db.azurewebsites.net");
            return client;
        }

        /// <summary>
        /// Helper for serializaing JSON.
        /// </summary>
        private static StringContent Serialize(dynamic json)
        {
            return new StringContent(JsonConvert.SerializeObject(json), Encoding.UTF8, "application/json");
        }

        /// <summary>
        /// All legal words
        /// </summary>
        private static readonly ISet<string> dictionary;

        static GradingTests()
        {
            dictionary = new HashSet<string>();
            using (StreamReader words = new StreamReader(AppDomain.CurrentDomain.BaseDirectory + @"/dictionary.txt"))
            {
                string word;
                while ((word = words.ReadLine()) != null)
                {
                    dictionary.Add(word.ToUpper());
                }
            }
        }


        /// <summary>
        /// Given a board configuration, returns all the valid words.
        /// </summary>
        private static List<string> AllValidWords(string board)
        {
            BoggleBoard bb = new BoggleBoard(board);
            List<string> validWords = new List<string>();
            foreach (string word in dictionary)
            {
                if (word.Length > 2 && bb.CanBeFormed(word))
                {
                    validWords.Add(word);
                }
            }
            return validWords;
        }

        /// <summary>
        /// Given a board configuration, returns as many words of different lengths as possible.
        /// </summary>
        private static List<string> DifferentLengthWords(string board)
        {
            List<string> variety = new List<string>();
            List<string> allWords = AllValidWords(board);
            for (int i = 3; i <= 10; i++)
            {
                int length = i;
                string word = allWords.Find(w => w.Length == length);
                if (word != null)
                {
                    variety.Add(word);
                }
            }
            return variety;
        }

        /// <summary>
        /// Returns the score for a word.
        /// </summary>
        private static int GetScore(string word)
        {
            if (!dictionary.Contains(word) && word.Length >= 3)
            {
                return -1;
            }
            switch (word.Length)
            {
                case 1:
                case 2:
                    return 0;
                case 3:
                case 4:
                    return 1;
                case 5:
                    return 2;
                case 6:
                    return 3;
                case 7:
                    return 5;
                default:
                    return 11;
            }
        }

        /// <summary>
        /// Makes a user and asserts that the resulting status code is equal to the
        /// status parameter.  Returns a Task that will produce the new userID.
        /// </summary>
        private async Task<string> MakeUser(String nickname, HttpStatusCode status = 0)
        {
            dynamic name = new ExpandoObject();
            name.Nickname = nickname;

            using (HttpClient client = CreateClient())
            {
                HttpResponseMessage response = await client.PostAsync("/BoggleService.svc/users", Serialize(name));
                if (status != 0) Assert.AreEqual(status, response.StatusCode);
                if (response.IsSuccessStatusCode)
                {
                    String result = await response.Content.ReadAsStringAsync();
                    dynamic user = JsonConvert.DeserializeObject(result);
                    return user.UserToken;
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Joins the game and asserts that the resulting status code is equal to the parameter status.
        /// Returns a Task that will produce the new GameID.
        /// </summary>
        private async Task<string> JoinGame(String player, int timeLimit, HttpStatusCode status = 0)
        {
            dynamic user = new ExpandoObject();
            user.UserToken = player;
            user.TimeLimit = timeLimit;

            using (HttpClient client = CreateClient())
            {
                HttpResponseMessage response = await client.PostAsync("/BoggleService.svc/games", Serialize(user));
                if (status != 0) Assert.AreEqual(status, response.StatusCode);
                if (response.IsSuccessStatusCode)
                {
                    String result = await response.Content.ReadAsStringAsync();
                    dynamic game = JsonConvert.DeserializeObject(result);
                    return game.GameID;
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Joins the game returns a Task that will produce the resulting status.
        /// </summary>
        private async Task<HttpStatusCode> JoinGameStatus(String player)
        {
            dynamic user = new ExpandoObject();
            user.UserToken = player;
            user.TimeLimit = 10;

            using (HttpClient client = CreateClient())
            {
                HttpResponseMessage response = await client.PostAsync("/BoggleService.svc/games", Serialize(user));
                return response.StatusCode;
            }
        }

        /// <summary>
        /// Cancels the pending game and asserts that the resulting status code is
        /// equal to the parameter status.
        /// </summary>
        private async Task CancelGame(String player, HttpStatusCode status = 0)
        {
            dynamic user = new ExpandoObject();
            user.UserToken = player;

            using (HttpClient client = CreateClient())
            {
                HttpResponseMessage response = await client.PutAsync("/BoggleService.svc/games", Serialize(user));
                if (status != 0) Assert.AreEqual(status, response.StatusCode);
            }
        }

        /// <summary>
        /// Gets the status for the specified game and value of brief.  Asserts that the resulting
        /// status code is equal to the parameter status.  Returns a task that produces the object
        /// returned by the service.
        /// </summary>
        private async Task<dynamic> GetStatus(String game, string brief, HttpStatusCode status = 0)
        {
            using (HttpClient client = CreateClient())
            {
                HttpResponseMessage response = await client.GetAsync("/BoggleService.svc/games/" + game + "?brief=" + brief);
                if (status != 0) Assert.AreEqual(status, response.StatusCode);
                if (response.IsSuccessStatusCode)
                {
                    String result = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject(result);
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Plays a word and asserts that the resulting status code is equal to the parameter
        /// status.  Returns a task that will produce the score of the word.
        /// </summary>
        private async Task<int> PlayWord(String player, String game, String word, HttpStatusCode status = 0)
        {
            dynamic play = new ExpandoObject();
            play.UserToken = player;
            play.Word = word;

            using (HttpClient client = CreateClient())
            {
                HttpResponseMessage response = await client.PutAsync("/BoggleService.svc/games/" + game, Serialize(play));
                if (status != 0) Assert.AreEqual(status, response.StatusCode);
                if (response.IsSuccessStatusCode)
                {
                    String result = await response.Content.ReadAsStringAsync();
                    dynamic score = JsonConvert.DeserializeObject(result);
                    return score.Score;
                }
                else
                {
                    return -2;
                }
            }
        }

        // Null player name status
        [TestMethod]
        public void MakeUser1()
        {
            MakeUser(null, Forbidden).Wait();
        }

        // Successful creation
        [TestMethod]
        public void MakeUser3()
        {
            String userID = MakeUser("Player").Result;
            Assert.IsTrue(userID.Length > 0);
        }

        // Bad UserID and bad time limit
        [TestMethod]
        public void JoinGame1()
        {
            JoinGame("xyzzy123", 200, Forbidden).Wait();
        }

        // Start a game
        [TestMethod]
        public void JoinGame4()
        {
            String player1 = MakeUser("Player 1").Result;
            String game1 = JoinGame(player1, 10).Result;
            String player2 = MakeUser("Player 1").Result;
            String game2 = JoinGame(player2, 10).Result;
            if (game1 != game2)
            {
                String player3 = MakeUser("Player3").Result;
                Assert.AreEqual(game2, JoinGame(player3, 10).Result);
            }
        }

        // Bad game ID cancel status correct
        [TestMethod]
        public void TestCancelGame1()
        {
            CancelGame("xyzzy123", Forbidden).Wait(); ;
        }

        // Successful cancellation
        [TestMethod]
        public void TestCancelGame5()
        {
            String player1 = MakeUser("Player 1").Result;
            String game1 = JoinGame(player1, 10).Result;
            String player2 = MakeUser("Player 1").Result;
            String game2 = JoinGame(player2, 10).Result;
            if (game1 != game2)
            {
                CancelGame(player2, OK).Wait();
            }
            else
            {
                String player3 = MakeUser("Player 3").Result;
                String game3 = JoinGame(player3, 10).Result;
                CancelGame(player3, OK).Wait();
            }
        }

        // Can't play a word with non-existent player/game
        [TestMethod]
        public void TestPlayWord5()
        {
            PlayWord("xxx", "yyy", "a", Forbidden).Wait();
        }

        // Bad word correct score for player 1
        [TestMethod]
        public void TestPlayWord7()
        {
            String player1 = MakeUser("Player 1").Result;
            String game1 = JoinGame(player1, 10).Result;
            String player2 = MakeUser("Player 1").Result;
            String game = JoinGame(player2, 10).Result;
            if (game1 != game)
            {
                player1 = MakeUser("Player1").Result;
                game = JoinGame(player1, 10).Result;
            }
            int score = PlayWord(player1, game, "xyzzy123").Result;
            Assert.AreEqual(-1, score);
        }

        // Status of bad game
        [TestMethod]
        public void TestStatus1()
        {
            GetStatus("xyzzy123", "no", Forbidden).Wait();
        }

        // GameState of active game
        [TestMethod]
        public void TestStatus3()
        {
            String player1 = MakeUser("Player 1").Result;
            String game1 = JoinGame(player1, 10).Result;
            String player2 = MakeUser("Player 1").Result;
            String game = JoinGame(player2, 10).Result;
            if (game1 != game)
            {
                player1 = MakeUser("Player1").Result;
                game = JoinGame(player1, 10).Result;
            }
            string state = GetStatus(game, "no").Result.GameState;
            Assert.AreEqual("active", state);
        }

        // Simulates a full game
        public string SimulateGame(out List<string> p1Words, out List<string> p2Words, AutoResetEvent resetEvent)
        {
            p1Words = new List<string>();
            p2Words = new List<string>();

            String player1 = MakeUser("Player 1").Result;
            String game1 = JoinGame(player1, 30).Result;
            String player2 = MakeUser("Player 1").Result;
            String game2 = JoinGame(player2, 30).Result;
            if (game1 != game2)
            {
                player1 = MakeUser("Player1").Result;
                game2 = JoinGame(player1, 30).Result;
            }

            resetEvent.Set();

            string board = GetStatus(game2, "no").Result.Board;
            List<string> allWords = AllValidWords(board);

            DateTime start = DateTime.Now;

            int count = 0;
            foreach (string w in allWords)
            {
                if (count > 30)
                {
                    break;
                }
                else if (count % 2 == 0)
                {
                    p1Words.Add(w);
                    PlayWord(player1, game2, w).Wait();
                }
                else
                {
                    p2Words.Add(w);
                    PlayWord(player2, game2, w).Wait();
                }
                count++;
            }

            
            do
            {
                Thread.Sleep(1000);
            }
            while (DateTime.Now.Subtract(start).TotalSeconds < 32);

            return game2;
        }


        // Play one game with correct scores
        public void Play(AutoResetEvent resetEvent)
        {
            List<string> p1Words, p2Words;
            string game = SimulateGame(out p1Words, out p2Words, resetEvent);
            string board = GetStatus(game, "no").Result.Board;
            List<dynamic> wordscores1 = new List<dynamic>(GetStatus(game, "no").Result.Player1.WordsPlayed);
            List<dynamic> wordscores2 = new List<dynamic>(GetStatus(game, "no").Result.Player2.WordsPlayed);
            wordscores1.Sort((x, y) => x.Word.CompareTo(y.Word));
            wordscores1.Sort((x, y) => x.Word.CompareTo(y.Word));
            p1Words.Sort();
            p2Words.Sort();
            Assert.AreEqual(p1Words.Count, wordscores1.Count);
            Assert.AreEqual(p2Words.Count, wordscores2.Count);

            int total1 = 0;
            for (int i = 0; i < p1Words.Count; i++)
            {
                Assert.AreEqual(p1Words[i].ToUpper(), wordscores1[i].Word.ToString().ToUpper());
                Assert.AreEqual(GetScore(p1Words[i]), (int)wordscores1[i].Score);
                total1 += GetScore(p1Words[i]);
            }

            int total2 = 0;
            for (int i = 0; i < p2Words.Count; i++)
            {
                Assert.AreEqual(p2Words[i].ToUpper(), wordscores2[i].Word.ToString().ToUpper());
                Assert.AreEqual(GetScore(p2Words[i]), (int)wordscores2[i].Score);
                total2 += GetScore(p2Words[i]);
            }

            int score1 = GetStatus(game, "no").Result.Player1.Score;
            int score2 = GetStatus(game, "no").Result.Player2.Score;
            Assert.AreEqual(total1, score1);
            Assert.AreEqual(total2, score2);
        }


        // Play one game with correct scores
        [TestMethod]
        public void PlayOne ()
        {
            AutoResetEvent resetEvent = new AutoResetEvent(false);
            Play(resetEvent);
        }
    }
}
