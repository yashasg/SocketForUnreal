// Based on tester for PS9 written by Dave Heyborne (U0459350) and Hoon Ik Cho (U0713654).
// Extended by Joe Zachary

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using BoggleService;
using Newtonsoft.Json;
using System.Threading;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BoggleServiceTest
{
    [TestClass]
    public partial class BoggleServiceGrader
    {
        // To run the ConsoleTester, the declaration below must be uncommented.
        // To run unit tests, the declaration below must be commented out.
        // This will cause errors in Console Tester, so you should Unload that project
        // while using unit tests.
        public AssertionCounter Assert;

        private string AssertMakeUser(String nickname)
        {
            // Create one player
            MakeUserPOST userPost = new MakeUserPOST
            {
                nickname = nickname,
            };
            MakeUserTestResponse postResponse = POST_MakeUser(userPost).Result;
            return postResponse.Response.userToken;
        }

        public string AssertJoinGame(String userToken, int code = 200)
        {
            JoinGamePOST userJoinPost = new JoinGamePOST
            {
                userToken = userToken
            };
            JoinGamePOSTTestResponse joinResponse = POST_JoinGame(userJoinPost).Result;
            HttpStatusCode statusCode = joinResponse.ResponseCode;
            Assert.AreEqual(code, (int)statusCode);
            if (IsOK(joinResponse.ResponseCode))
            {
                return joinResponse.Response.gameToken;
            }
            else
            {
                Assert.AreEqual(code, (int)joinResponse.ResponseCode);
                return null;
            }
        }
        public void AssertCancelGame(String userToken, String gameToken, int code = 204)
        {
            JoinGameDELETETestResponse joinResponse = DELETE_JoinGame(userToken, gameToken).Result;
            HttpStatusCode resultCode = joinResponse.ResponseCode;
            Assert.AreEqual(code, (int)joinResponse.ResponseCode);
        }

        public int AssertBriefStatus(String gameToken, String status, int score1, int score2, int timeleft = -1, int code = 200)
        {
            BriefStatusTestResponse briefResponse = GET_BriefStatus(gameToken).Result;
            HttpStatusCode resultCode = briefResponse.ResponseCode;
            Assert.AreEqual(code, (int)resultCode);
            if (IsOK(resultCode))
            {
                Assert.AreEqual(HttpStatusCode.OK, briefResponse.ResponseCode);
                Assert.AreEqual(status, briefResponse.Response.gameStatus);
                Assert.AreEqual(0, briefResponse.Response.score1);
                Assert.AreEqual(0, briefResponse.Response.score2);
                int reportedTimeleft = briefResponse.Response.timeleft;
                if (timeleft >= 0) Assert.IsTrue(Math.Abs(timeleft - reportedTimeleft) <= 2);
                return reportedTimeleft;
            }
            else
            {
                return 0;
            }
        }

        public class Status
        {
            public string Board { get; set; }
            public int TimeLimit { get; set; }
        }

        public Status AssertFullStatus(String gameToken, String nickname1, String nickname2, String status, int score1, int score2,
                                        List<WordScorePair> words1, List<WordScorePair> words2, int timelimit = -1, int timeleft = -1, int code = 200)
        {
            FullStatusTestResponse fullResponse = GET_FullStatus(gameToken).Result;
            HttpStatusCode resultCode = fullResponse.ResponseCode;
            Assert.AreEqual(code, (int)resultCode);
            if (IsOK(resultCode))
            {
                Assert.AreEqual(HttpStatusCode.OK, fullResponse.ResponseCode);
                Assert.AreEqual(status, fullResponse.Response.gameStatus);
                Assert.AreEqual(nickname1, fullResponse.Response.player1.nickname);
                Assert.AreEqual(nickname2, fullResponse.Response.player2.nickname);
                Assert.AreEqual(score1, fullResponse.Response.player1.score);
                Assert.AreEqual(score2, fullResponse.Response.player2.score);
                Assert.IsTrue(new HashSet<WordScorePair>(words1).SetEquals(fullResponse.Response.player1.wordsPlayed));
                Assert.IsTrue(new HashSet<WordScorePair>(words2).SetEquals(fullResponse.Response.player2.wordsPlayed));
                string board = fullResponse.Response.board;
                if (status == "playing" || status == "finished")
                {
                    Assert.AreEqual(16, board.Length);
                }
                else
                {
                    Assert.AreEqual(0, board.Length);
                }
                int reportedTimeleft = fullResponse.Response.timeleft;
                if (timeleft >= 0) Assert.IsTrue(Math.Abs(timeleft - reportedTimeleft) <= 2);
                int reportedTimelimit = fullResponse.Response.timelimit;
                if (timelimit >= 0) Assert.AreEqual(timelimit, reportedTimelimit);
                return new Status() { Board = board, TimeLimit = reportedTimelimit };
            }
            else
            {
                return null;
            }
        }

        public int AssertPlayWord(string gameToken, string playerToken, string word, int score, int code = 200)
        {
            PlayWordPOST data = new PlayWordPOST()
            {
                gameToken = gameToken,
                playerToken = playerToken,
                word = word
            };
            PlayWordTestResponse response = POST_PlayWord(data).Result;
            HttpStatusCode resultCode = response.ResponseCode;
            Assert.AreEqual(code, (int)resultCode);
            if (IsOK(resultCode))
            {
                int theScore = response.Response.wordScore;
                Assert.AreEqual(score, theScore);
                return theScore;
            }
            else
            {
                return -2;
            }
        }

        private bool IsOK(HttpStatusCode code)
        {
            int c = (int)code;
            return c >= 200 && c < 300;
        }

        public void AssertGameOver(String gameToken)
        {
            BriefStatusTestResponse briefResponse = GET_BriefStatus(gameToken).Result;
            Assert.AreEqual(HttpStatusCode.OK, briefResponse.ResponseCode);
            int reportedTimeleft = briefResponse.Response.timeleft;
            Assert.IsTrue(reportedTimeleft > 0);
            if (briefResponse.Response.gameStatus != "playing")
            {
                Console.WriteLine("Game is unexpectedly not playing.  Consider making games longer.");
            }
            Assert.AreEqual("playing", briefResponse.Response.gameStatus);

            int timeleft = reportedTimeleft;
            while (true)
            {
                Thread.Sleep(1000);
                briefResponse = GET_BriefStatus(gameToken).Result;
                //Assert.AreEqual(HttpStatusCode.OK, briefResponse.ResponseCode);
                reportedTimeleft = briefResponse.Response.timeleft;
                //Assert.IsTrue(timeleft < 0 || Math.Abs(timeleft - reportedTimeleft) <= 2);
                if (reportedTimeleft <= 0 || briefResponse.Response.gameStatus == "finished")
                {
                    Assert.AreEqual(HttpStatusCode.OK, briefResponse.ResponseCode);
                    Assert.AreEqual("finished", briefResponse.Response.gameStatus);
                    Assert.AreEqual(0, reportedTimeleft);
                    Console.Write("\r                                                          \r");
                    return;
                }
                else
                {
                    //Assert.AreEqual("playing", briefResponse.Response.gameStatus);
                    Console.Write("\r" + reportedTimeleft + " seconds left                      ");
                    timeleft = reportedTimeleft - 1;
                }
            }
        }

        /// <summary>
        /// Make players, start game, let game end with no words, using brief status.
        /// </summary>
        [TestMethod]
        public void UseCase1()
        {
            Console.WriteLine("UseCase1");
            String user1Token = AssertMakeUser("Player 1");
            String user2Token = AssertMakeUser("Player 2");
            String gameToken = AssertJoinGame(user1Token);
            AssertBriefStatus(gameToken, "waiting", 0, 0, 0);
            String gameToken2 = AssertJoinGame(user2Token);
            Assert.AreEqual(gameToken, gameToken2);
            AssertBriefStatus(gameToken, "playing", 0, 0);
            AssertGameOver(gameToken);
            AssertBriefStatus(gameToken, "finished", 0, 0, 0);
        }

        /// <summary>
        /// Make players, start game, let game end with no words, using full status.
        /// </summary>
        [TestMethod]
        public void UseCase2()
        {
            Console.WriteLine("UseCase2");
            String user1Token = AssertMakeUser("Player 1");
            String user2Token = AssertMakeUser("Player 2");
            String gameToken = AssertJoinGame(user1Token);
            AssertFullStatus(gameToken, "Player 1", "", "waiting", 0, 0, new List<WordScorePair>(),
                new List<WordScorePair>(), 0, 0);
            String gameToken2 = AssertJoinGame(user2Token);
            Assert.AreEqual(gameToken, gameToken2);
            Status status = AssertFullStatus(gameToken, "Player 1", "Player 2", "playing", 0, 0, new List<WordScorePair>(),
               new List<WordScorePair>());
            AssertGameOver(gameToken);
            AssertFullStatus(gameToken, "Player 1", "Player 2", "finished", 0, 0, new List<WordScorePair>(),
                new List<WordScorePair>(), status.TimeLimit, 0);
        }

        /// <summary>
        /// Make player, join game, cancel, using brief status.
        /// </summary>
        [TestMethod]
        public void UseCase3()
        {
            Console.WriteLine("UseCase3");
            String userToken = AssertMakeUser("Player 1");
            String gameToken = AssertJoinGame(userToken);
            AssertBriefStatus(gameToken, "waiting", 0, 0, 0);
            AssertCancelGame(userToken, gameToken);
            AssertBriefStatus(gameToken, "canceled", 0, 0, 0);
        }

        /// <summary>
        /// Make player, join game, cancel, using full status.
        /// </summary>
        [TestMethod]
        public void UseCase4()
        {
            Console.WriteLine("UseCase4");
            String userToken = AssertMakeUser("Player 1");
            String gameToken = AssertJoinGame(userToken);
            AssertFullStatus(gameToken, "Player 1", "", "waiting", 0, 0, new List<WordScorePair>(),
                new List<WordScorePair>());
            AssertCancelGame(userToken, gameToken);
            AssertFullStatus(gameToken, "Player 1", "", "canceled", 0, 0, new List<WordScorePair>(),
                new List<WordScorePair>(), 0, 0);
        }

        /// <summary>
        /// Plays incorrect words of various types
        /// </summary>
        [TestMethod]
        public void UseCase5()
        {
            Console.WriteLine("UseCase5");
            String user1Token = AssertMakeUser("Player 1");
            String user2Token = AssertMakeUser("Player 2");
            String gameToken = AssertJoinGame(user1Token);
            AssertJoinGame(user2Token);
            AssertPlayWord(gameToken, user1Token, "XYZ", -1);
            AssertPlayWord(gameToken, user2Token, "XYZ", -1);
            AssertPlayWord(gameToken, user1Token, "AN", 0);
            AssertPlayWord(gameToken, user1Token, "XYZ", 0);
            AssertPlayWord(gameToken, user2Token, "AN", 0);
            AssertPlayWord(gameToken, user2Token, "XYZ", 0);
            AssertPlayWord(gameToken, user2Token, "QABALA", -1);
            AssertPlayWord(gameToken, user1Token, "QABALA", -1);
            AssertPlayWord(gameToken, user1Token, "XYZZY", -1);
            AssertPlayWord(gameToken, user1Token, "QQ", 0);
            AssertPlayWord(gameToken, user2Token, "QQ", 0);

            List<WordScorePair> list1 = new List<WordScorePair>();
            list1.Add(new WordScorePair() { word = "XYZ", score = -1 });
            list1.Add(new WordScorePair() { word = "AN", score = 0 });
            list1.Add(new WordScorePair() { word = "XYZ", score = 0 });
            list1.Add(new WordScorePair() { word = "QABALA", score = -1 });
            list1.Add(new WordScorePair() { word = "XYZZY", score = -1 });
            list1.Add(new WordScorePair() { word = "QQ", score = 0 });

            List<WordScorePair> list2 = new List<WordScorePair>();
            list2.Add(new WordScorePair() { word = "XYZ", score = -1 });
            list2.Add(new WordScorePair() { word = "AN", score = 0 });
            list2.Add(new WordScorePair() { word = "XYZ", score = 0 });
            list2.Add(new WordScorePair() { word = "QABALA", score = -1 });
            list2.Add(new WordScorePair() { word = "QQ", score = 0 });

            AssertFullStatus(gameToken, "Player 1", "Player 2", "playing", -3, -2,
                new List<WordScorePair>(), new List<WordScorePair>());

            AssertGameOver(gameToken);

            AssertFullStatus(gameToken, "Player 1", "Player 2", "finished", -3, -2,
                list1, list2, -1, 0);
        }

        /// <summary>
        /// Plays correct words of various types
        /// </summary>
        [TestMethod]
        public void UseCase6()
        {
            Console.WriteLine("UseCase6");
            String user1Token = AssertMakeUser("Player 1");
            String user2Token = AssertMakeUser("Player 2");
            String gameToken = AssertJoinGame(user1Token);
            AssertJoinGame(user2Token);

            List<WordScorePair> list1 = new List<WordScorePair>();
            List<WordScorePair> list2 = new List<WordScorePair>();
            Status status = AssertFullStatus(gameToken, "Player 1", "Player 2", "playing", 0, 0,
                list1, list2);
            List<string> words = GetSomeValidWords(status.Board);
            int score1 = 0;
            int score2 = 0;

            int count = 0;
            int limit = 26;
            foreach (string word in words)
            {
                int s = Score(word);
                AssertPlayWord(gameToken, user1Token, word, s);
                list1.Add(new WordScorePair() { word = word, score = s });
                score1 += s;
                if (count % 2 == 0)
                {
                    AssertPlayWord(gameToken, user2Token, word, s);
                    list2.Add(new WordScorePair() { word = word, score = s });
                    score2 += s;
                }
                count++;
                limit--;
                if (limit <= 0) break;
            }
            if (limit > 0)
            {
                Console.WriteLine("Boggle board did not contain at least 26 valid words.  Use another board.");
            }
            AssertFullStatus(gameToken, "Player 1", "Player 2", "playing", score1, score2,
                new List<WordScorePair>(), new List<WordScorePair>());

            AssertGameOver(gameToken);

            AssertFullStatus(gameToken, "Player 1", "Player 2", "finished", score1, score2,
                list1, list2, -1, 0);
        }


        private int Score(String word)
        {
            if (word.Length < 3)
            {
                return 0;
            }
            else if (word.Length <= 4)
            {
                return 1;
            }
            else if (word.Length <= 5)
            {
                return 2;
            }
            else if (word.Length <= 6)
            {
                return 3;
            }
            else if (word.Length <= 7)
            {
                return 5;
            }
            else
            {
                return 11;
            }
        }

        private List<string> GetSomeValidWords(string board)
        {
            List<string> result = new List<string>();
            Boggle.BoggleBoard bb = new Boggle.BoggleBoard(board);
            using (StreamReader dict = new StreamReader("../../../dictionary.txt"))
            {
                string word;
                while ((word = dict.ReadLine()) != null)
                {
                    if (bb.CanBeFormed(word))
                    {
                        result.Add(word);
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Error cases for /joingame (POST)
        /// </summary>
        [TestMethod]
        public void JoinGameErrorTests()
        {
            Console.WriteLine("JoinGameErrorTests");
            AssertJoinGame("garbage", 403);
            String userToken = AssertMakeUser("Player 1");
            String gameToken = AssertJoinGame(userToken);
            AssertJoinGame(userToken, 409);
            AssertCancelGame(userToken, gameToken);
        }

        /// <summary>
        /// Error cases for /joingame (DELETE)
        /// </summary>
        [TestMethod]
        public void DeleteGameErrorTests()
        {
            Console.WriteLine("DeleteGameErrorTests");
            String userToken1 = AssertMakeUser("Player 1");
            String userToken2 = AssertMakeUser("Player 2");
            String gameToken = AssertJoinGame(userToken1);
            AssertCancelGame("garbage", gameToken, 403);
            AssertCancelGame(userToken1, "garbage", 403);
            AssertCancelGame(userToken2, gameToken, 403);
            AssertJoinGame(userToken2);
            AssertCancelGame(userToken2, gameToken, 409);
        }

        /// <summary>
        /// Error cases for /status
        /// </summary>
        [TestMethod]
        public void StatusErrorTests()
        {
            Console.WriteLine("StatusErrorTests");
            AssertFullStatus("garbage", "", "", "", 0, 0, null, null, 0, 0, 403);
        }

        /// <summary>
        /// Error cases for /fullstatus
        /// </summary>
        [TestMethod]
        public void BriefStatusErrorTests()
        {
            Console.WriteLine("BriefStatusErrorTest");
            AssertBriefStatus("garbage", "", 0, 0, 0, 403);
        }

        /// <summary>
        /// Error cases for /playword
        /// </summary>
        [TestMethod]
        public void PlayWordErrorTests()
        {
            Console.WriteLine("PlayWordErrorTests");
            String userToken1 = AssertMakeUser("Player 1");
            AssertPlayWord("garbage", userToken1, "", 0, 403);
            String userToken2 = AssertMakeUser("Player 2");
            String gameToken = AssertJoinGame(userToken1);
            AssertPlayWord(gameToken, "garbage", "the", 0, 403);
            AssertPlayWord(gameToken, userToken2, "the", 0, 403);
            AssertPlayWord(gameToken, userToken1, "the", 0, 409);
            AssertJoinGame(userToken2);
            String userToken3 = AssertMakeUser("Player 3");
            AssertPlayWord(gameToken, userToken3, "the", 0, 403);
            gameToken = AssertJoinGame(userToken3);
            AssertCancelGame(userToken3, gameToken);
            AssertPlayWord(gameToken, userToken3, "the", 0, 409);
        }
    }
}
