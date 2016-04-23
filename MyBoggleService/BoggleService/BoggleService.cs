using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.ServiceModel.Web;
using static System.Net.HttpStatusCode;

namespace Boggle
{
    public class BoggleService : IBoggleService
    {
        // The connection string to the DB
        private static string BoggleDB;

        // The dictionary of legal words
        private static ISet<string> dictionary;

        // The log file
        private readonly static string logFile;

        /// <summary>
        /// Initialize the BoggleDB and dictionary
        /// </summary>
        static BoggleService()
        {
            BoggleDB = ConfigurationManager.ConnectionStrings["BoggleDB"].ConnectionString;
            logFile = AppDomain.CurrentDomain.BaseDirectory + "LOG.txt";
            dictionary = new HashSet<string>();
            using (StreamReader words = new StreamReader(AppDomain.CurrentDomain.BaseDirectory + @"..\dictionary.txt"))
            {
                string word;
                while ((word = words.ReadLine()) != null)
                {
                    dictionary.Add(word.ToUpper());
                }
            }
        }

        // Writes to the log file
        private static void WriteLog(Exception e)
        {
            try
            {
                lock (logFile)
                {
                    using (StreamWriter log = File.AppendText(logFile))
                    {
                        log.Write("Log Entry ");
                        log.WriteLine("{0} {1}", DateTime.Now.ToLongTimeString(), DateTime.Now.ToLongDateString());
                        log.WriteLine();
                        log.WriteLine(e);
                        log.WriteLine(e.StackTrace);
                        log.WriteLine("--------------------------------------------------");
                        log.WriteLine();
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Set the HTTP response status.
        /// </summary>
        /// <param name="status"></param>
        private static void SetStatus(HttpStatusCode status)
        {
            WebOperationContext.Current.OutgoingResponse.StatusCode = status;
        }

        /// <summary>
        /// Returns a stream to the API documentation.
        /// </summary>
        public Stream API()
        {
            WebOperationContext.Current.OutgoingResponse.ContentType = "text/html";
            return File.OpenRead(AppDomain.CurrentDomain.BaseDirectory + "index.html");
        }

        /// <summary>
        /// Create a user with the provided data.Nickname.
        /// </summary>
        public User MakeUser(Name data)
        {
            // Validate the nickname
            if (data.Nickname == null || data.Nickname.Trim() == "")
            {
                SetStatus(Forbidden);
                return null;
            }

            using (SqlConnection conn = new SqlConnection(BoggleDB))
            {
                conn.Open();
                SqlTransaction trans = conn.BeginTransaction();

                try
                {
                    // Insert a new user into the Users table
                    SqlCommand command = new SqlCommand("insert into Users (UserToken, Nickname) values(@UserToken, @Nickname)", conn, trans);
                    string guid = Guid.NewGuid().ToString();
                    command.Parameters.AddWithValue("@UserToken", guid);
                    command.Parameters.AddWithValue("@Nickname", data.Nickname.Trim());
                    command.ExecuteNonQuery();

                    // Send back the new UserToken
                    SetStatus(Created);
                    return new User() { UserToken = guid };
                }
                catch (Exception e)
                {
                    WriteLog(e);
                    trans.Rollback();
                    SetStatus(InternalServerError);
                    return null;
                }
                finally
                {
                    if (trans.Connection != null)
                    {
                        trans.Commit();
                    }
                }
            }
        }

        /// <summary>
        /// Join a game as either the first or second player.
        /// </summary>
        public Game JoinGame(GameRequest data)
        {
            // Validate incoming data
            if (data.UserToken == null || data.TimeLimit < 5 || data.TimeLimit > 120)
            {
                SetStatus(Forbidden);
                return null;
            }

            using (SqlConnection conn = new SqlConnection(BoggleDB))
            {
                conn.Open();
                SqlTransaction trans = conn.BeginTransaction();

                try
                {
                    // Validate the incoming UserToken
                    SqlCommand command = new SqlCommand("select UserToken from Users where UserToken=@UserToken", conn, trans);
                    command.Parameters.AddWithValue("@UserToken", data.UserToken);
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            SetStatus(Forbidden);
                            return null;
                        }
                    }

                    // See if there's a pending game, and get its gameID, player token, and timeLimit if so.
                    long gameID = 0;
                    string player1Token = null;
                    int timeLimit = 0;

                    command = new SqlCommand("select GameID, Player1, TimeLimit from Games where Player2 is null", conn, trans);
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            gameID = (long)reader["GameID"];
                            player1Token = (string)reader["Player1"];
                            timeLimit = (int)reader["TimeLimit"];
                        }
                    }

                    // There is a pending game
                    if (player1Token != null)
                    {
                        // Validate the incoming UserToken by making sure it is different from player1Token
                        if (player1Token == data.UserToken)
                        {
                            SetStatus(Conflict);
                            return null;
                        }

                        // Convert the pending game into an active game
                        else
                        {
                            command = new SqlCommand("update Games set Player2=@Player2, Board=@Board, StartTime=@StartTime, TimeLimit=@TimeLimit where GameID=@GameID", conn, trans);
                            command.Parameters.AddWithValue("@Player2", data.UserToken);
                            command.Parameters.AddWithValue("@Board", BoggleBoard.GenerateBoard());
                            command.Parameters.AddWithValue("@TimeLimit", (timeLimit + data.TimeLimit) / 2);
                            command.Parameters.Add("@StartTime", SqlDbType.DateTime).Value = DateTime.Now;
                            command.Parameters.AddWithValue("@GameID", gameID);
                            command.ExecuteNonQuery();

                            // Report the GameID of the new active game
                            SetStatus(Created);
                            return new Game() { GameID = gameID.ToString() };
                        }
                    }

                    // There is no pending game
                    else
                    {
                        // Create a new pending game 
                        command = new SqlCommand("insert into Games (Player1, TimeLimit) output inserted.GameID values(@Player1, @TimeLimit)", conn, trans);
                        command.Parameters.AddWithValue("@Player1", data.UserToken);
                        command.Parameters.AddWithValue("@TimeLimit", data.TimeLimit);
                        SetStatus(Accepted);
                        return new Game() { GameID = command.ExecuteScalar().ToString() };
                    }
                }
                catch (Exception)
                {
                    trans.Rollback();
                    SetStatus(InternalServerError);
                    return null;
                }
                finally
                {
                    if (trans.Connection != null)
                    {
                        trans.Commit();
                    }
                }
            }
        }

        /// <summary>
        /// Cancel participation in the pending game.
        /// </summary>
        public void CancelGame(User data)
        {
            if (data.UserToken == null)
            {
                SetStatus(Forbidden);
                return;
            }

            using (SqlConnection conn = new SqlConnection(BoggleDB))
            {
                conn.Open();
                SqlTransaction trans = conn.BeginTransaction();

                try
                {
                    // If there's a pending game involving data.UserToken, delete it.
                    SqlCommand command = new SqlCommand("delete from Games where Player1=@Player1 and Player2 is null", conn, trans);
                    command.Parameters.AddWithValue("@Player1", data.UserToken);
                    if (command.ExecuteNonQuery() == 0)
                    {
                        SetStatus(Forbidden);
                    }
                    else
                    {
                        SetStatus(OK);
                    }
                }
                catch (Exception)
                {
                    trans.Rollback();
                    SetStatus(InternalServerError);
                    return;
                }
                finally
                {
                    if (trans.Connection != null)
                    {
                        trans.Commit();
                    }
                }
            }
        }

        public WordScore PlayWord(PlayedWord data, string game)
        {
            // Validate incoming parameters
            if (game == null || data.UserToken == null || data.Word == null || data.Word.Trim().Length == 0)
            {
                SetStatus(Forbidden);
                return null;
            }

            // The GameID must be an integer
            long gameID;
            if (!Int64.TryParse(game, out gameID))
            {
                SetStatus(Forbidden);
                return null;
            }

            using (SqlConnection conn = new SqlConnection(BoggleDB))
            {
                conn.Open();
                SqlTransaction trans = conn.BeginTransaction();

                try
                {
                    // Get information about the game in which the word is to be played
                    SqlCommand command = new SqlCommand("select GameID, Player1, Player2, Board, TimeLimit, StartTime from Games where GameID=@GameID and (Player1 = @Player or Player2 = @Player)", conn, trans);
                    command.Parameters.AddWithValue("@GameID", gameID);
                    command.Parameters.AddWithValue("@Player", data.UserToken);

                    //string player;
                    string board;
                    int timeLimit;
                    DateTime startTime;

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            //player = (string)(((string)reader["Player1"] == data.UserToken) ? reader["Player1"] : reader["Player2"]);
                            if (reader["Player2"] is DBNull)
                            {
                                SetStatus(Conflict);
                                return null;
                            }
                            board = (string)reader["Board"];
                            timeLimit = (int)reader["TimeLimit"];
                            startTime = (DateTime)reader["StartTime"];
                            if (reader["Player2"] is DBNull)
                            {
                                SetStatus(Conflict);
                                return null;
                            }
                        }
                        else
                        {
                            SetStatus(Forbidden);
                            return null;
                        }
                    }

                    if (BoggleGame.GetTimeLeft(startTime, timeLimit) <= 0)
                    {
                        SetStatus(Conflict);
                        return null;
                    }

                    int score = BoggleGame.WordValue(data.Word.Trim(), board, dictionary);
                    if (score == -2)
                    {
                        SetStatus(Forbidden);
                        return null;
                    }

                    command = new SqlCommand("select Word from Words where GameID=@GameID and Player=@Player and Word=@Word", conn, trans);
                    command.Parameters.AddWithValue("@GameID", gameID);
                    command.Parameters.AddWithValue("@Player", data.UserToken);
                    command.Parameters.AddWithValue("@Word", data.Word.Trim());

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            score = 0;
                        }
                    }

                    command = new SqlCommand("insert into Words (Word, GameID, Player, Score) values(@Word, @GameID, @Player, @Score)", conn, trans);
                    command.Parameters.AddWithValue("@Word", data.Word);
                    command.Parameters.AddWithValue("@GameID", gameID);
                    command.Parameters.AddWithValue("@Player", data.UserToken);
                    command.Parameters.AddWithValue("@Score", score);
                    command.ExecuteNonQuery();

                    SetStatus(OK);
                    return new WordScore() { Score = score };
                }
                catch (Exception)
                {
                    trans.Rollback();
                    SetStatus(InternalServerError);
                    return null;
                }
                finally
                {
                    if (trans.Connection != null)
                    {
                        trans.Commit();
                    }
                }
            }
        }

        public Status Status(string game, string brief)
        {
            // Validate incoming parameters
            if (game == null)
            {
                SetStatus(Forbidden);
                return null;
            }
            long gameID;
            if (!Int64.TryParse(game, out gameID))
            {
                SetStatus(Forbidden);
                return null;
            }

            using (SqlConnection conn = new SqlConnection(BoggleDB))
            {
                conn.Open();
                SqlTransaction trans = conn.BeginTransaction();

                try
                {
                    string player1, player2;
                    int timeLimit;
                    DateTime startTime;
                    string board;

                    // Get information about the game in which the word is to be played
                    SqlCommand command = new SqlCommand("select GameID, Player1, Player2, TimeLimit, StartTime, Board from Games where GameID=@GameID", conn, trans);
                    command.Parameters.AddWithValue("@GameID", gameID);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            player1 = (string)reader["Player1"];
                            player2 = (reader["Player2"] is DBNull) ? null : (string)reader["Player2"];
                            timeLimit = (int)reader["TimeLimit"];
                            object o = reader["StartTime"];
                            startTime = (reader["StartTime"] is DBNull) ? DateTime.Now : (DateTime)reader["StartTime"];
                            board = (reader["Board"] is DBNull) ? null : (string)reader["Board"];
                        }
                        else
                        {
                            SetStatus(Forbidden);
                            return null;
                        }
                    }

                    Status status = new Status();
                    if (player2 == null)
                    {
                        status.GameState = "pending";
                        return status;
                    }

                    status.TimeLeft = BoggleGame.GetTimeLeft(startTime, timeLimit);
                    if (status.TimeLeft == 0)
                    {
                        status.GameState = "completed";
                    }
                    else
                    {
                        status.GameState = "active";
                    }

                    command = new SqlCommand("select GameID, Player, Word, Score from Words where GameID=@GameID and Player=@Player order by Id", conn, trans);
                    command.Parameters.AddWithValue("@GameID", gameID);
                    command.Parameters.AddWithValue("@Player", player1);

                    List<WordAndScore> words1 = null;
                    if (brief != "yes")
                    {
                        words1 = new List<WordAndScore>();
                    }
                    int score1 = 0;

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string word = (string)reader["Word"];
                            int score = (int)reader["Score"];
                            score1 += score;
                            if (brief != "yes")
                            {
                                words1.Add(new WordAndScore() { Word = word, Score = score });
                            }
                        }
                    }

                    command = new SqlCommand("select GameID, Player, Word, Score from Words where GameID=@GameID and Player=@Player order by Id", conn, trans);
                    command.Parameters.AddWithValue("@GameID", gameID);
                    command.Parameters.AddWithValue("@Player", player2);
                    List<WordAndScore> words2 = null;
                    if (brief != "yes")
                    {
                        words2 = new List<WordAndScore>();
                    }
                    int score2 = 0;

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string word = (string)reader["Word"];
                            int score = (int)reader["Score"];
                            score2 += score;
                            if (brief != "yes")
                            {
                                words2.Add(new WordAndScore() { Word = word, Score = score });
                            }
                        }
                    }

                    status.Player1 = new Player();
                    status.Player1.Score = score1;
                    status.Player2 = new Player();
                    status.Player2.Score = score2;

                    if (brief != "yes")
                    {
                        status.Board = board;
                        status.TimeLimit = timeLimit;

                        command = new SqlCommand("select Users1.Nickname as Nickname1, Users2.Nickname as Nickname2 from Users as Users1, Users as Users2 where Users1.UserToken=@Player1 and Users2.UserToken=@Player2", conn, trans);
                        command.Parameters.AddWithValue("@Player1", player1);
                        command.Parameters.AddWithValue("@Player2", player2);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                status.Player1.Nickname = (string)reader["Nickname1"];
                                status.Player2.Nickname = (string)reader["Nickname2"];
                            }
                        }

                        if (status.GameState != "active")
                        {
                            status.Player1.WordsPlayed = words1;
                            status.Player2.WordsPlayed = words2;
                        }
                    }
                    return status;
                }
                catch (Exception)
                {
                    trans.Rollback();
                    SetStatus(InternalServerError);
                    return null;
                }
                finally
                {
                    if (trans.Connection != null)
                    {
                        trans.Commit();
                    }
                }
            }
        }
    }
}
