// Tester for PS9 written by Dave Heyborne (U0459350) and Hoon Ik Cho (U0713654).

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Threading;
using System.IO;
using Boggle;

namespace BoggleServiceTest
{
    public partial class BoggleServiceGrader
    {
        /// <summary>
        ///     Creates a generic client for communicating with the <see cref = "BoggleService" /> on port 3000.
        /// </summary>
        /// <returns>An HttpClient which is set to use port 3000 for communication.</returns>
        private static HttpClient CreateClient()
        {
            HttpClient client = new HttpClient
            {
                BaseAddress = new Uri(@"http://localhost:23055/")
            };
            return client;
        }

        /// <summary>
        ///     A struct that represents the response code and <see cref = "MakeUserResponse" /> returned from a /makeuser POST call to a <see cref = "BoggleService" />.
        /// </summary>
        public struct MakeUserTestResponse
        {
            internal HttpStatusCode ResponseCode;
            internal MakeUserResponse Response;
        }

        /// <summary>
        ///     A struct that represents the response code and <see cref = "JoinGameResponse" /> returned from a /joingame POST call to a <see cref = "BoggleService" />.
        /// </summary>
        public struct JoinGamePOSTTestResponse
        {
            internal HttpStatusCode ResponseCode;
            internal JoinGameResponse Response;
        }

        /// <summary>
        ///     A struct that represents the response code returned from a /joingame DELETE call to a <see cref = "BoggleService" />.
        /// </summary>
        public struct JoinGameDELETETestResponse
        {
            internal HttpStatusCode ResponseCode;
        }

        /// <summary>
        ///     A struct that represents the response code and <see cref = "FullStatus" /> returned from a /status GET call to a <see cref = "BoggleService" />.
        /// </summary>
        public struct FullStatusTestResponse
        {
            internal HttpStatusCode ResponseCode;
            internal FullStatus Response;
        }

        /// <summary>
        ///     A struct that represents the response code and <see cref = "BriefStatus" /> returned from a /briefstatus GET call to a <see cref = "BoggleService" />.
        /// </summary>
        public struct BriefStatusTestResponse
        {
            internal HttpStatusCode ResponseCode;
            internal BriefStatus Response;
        }

        /// <summary>
        ///     A struct that represents the response code and <see cref = "PlayWordResponse" /> returned from a /playword POST call to a <see cref = "BoggleService" />.
        /// </summary>
        public struct PlayWordTestResponse
        {
            internal HttpStatusCode ResponseCode;
            internal PlayWordResponse Response;
        }

        /// <summary>
        ///     Issues a /makeuser POST call to a <see cref = "BoggleService" /> and returns the result.
        /// </summary>
        /// <param name = "makeUser">A <see cref = "MakeUserPOST" /> object representing the user to be created.</param>
        /// <returns>A <see cref = "MakeUserTestResponse" /> object indicating the result of the /makeuser request.</returns>
        public static async Task<MakeUserTestResponse> POST_MakeUser(Name makeUser)
        {
            using (HttpClient client = CreateClient())
            {
                StringContent content = new StringContent(JsonConvert.SerializeObject(makeUser), Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync("/Boggle.svc/makeuser", content);

                if (!response.IsSuccessStatusCode)
                {
                    return new MakeUserTestResponse
                    {
                        ResponseCode = response.StatusCode,
                        Response = null
                    };
                }

                return new MakeUserTestResponse
                {
                    ResponseCode = response.StatusCode,
                    Response = JsonConvert.DeserializeObject<MakeUserResponse>(await response.Content.ReadAsStringAsync())
                };
            }
        }

        /// <summary>
        ///     Issues a /joingame POST call to a <see cref = "BoggleService" /> and returns the result.
        /// </summary>
        /// <param name = "joinGame">A <see cref = "JoinGamePOST" /> object representing the user to add to a game.</param>
        /// <returns>A <see cref = "JoinGamePOSTTestResponse" /> object indicating the result of the /joingame request.</returns>
        public static async Task<JoinGamePOSTTestResponse> POST_JoinGame(JoinGamePOST joinGame)
        {
            using (HttpClient client = CreateClient())
            {
                StringContent content = new StringContent(JsonConvert.SerializeObject(joinGame), Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync("/Boggle.svc/joingame", content);

                if (!response.IsSuccessStatusCode)
                {
                    return new JoinGamePOSTTestResponse
                    {
                        ResponseCode = response.StatusCode,
                        Response = null
                    };
                }

                return new JoinGamePOSTTestResponse
                {
                    ResponseCode = response.StatusCode,
                    Response = JsonConvert.DeserializeObject<JoinGameResponse>(await response.Content.ReadAsStringAsync())
                };
            }
        }

        /// <summary>
        ///     Issues a /joingame DELETE call to a <see cref = "BoggleService" /> and returns the result.
        /// </summary>
        /// <param name = "userToken">A token corresponding to an existing user in the <see cref = "BoggleService" />.</param>
        /// <param name = "gameToken">A token corresponding to an existing game in the <see cref = "BoggleService" />.</param>
        /// <returns>A <see cref = "JoinGameDELETETestResponse" /> object indicating the result of the /joingame request.</returns>
        public static async Task<JoinGameDELETETestResponse> DELETE_JoinGame(string userToken, string gameToken)
        {
            using (HttpClient client = CreateClient())
            {
                HttpResponseMessage response = await client.DeleteAsync(String.Format("/Boggle.svc/joingame/{0}/{1}", Uri.EscapeDataString(userToken), Uri.EscapeDataString(gameToken)));

                return new JoinGameDELETETestResponse
                {
                    ResponseCode = response.StatusCode,
                };
            }
        }

        /// <summary>
        ///     Issues a /status GET call to a <see cref = "BoggleService" /> and returns the result.
        /// </summary>
        /// <param name = "gameToken">A token corresponding to an existing game in the <see cref = "BoggleService" />.</param>
        /// <returns>A <see cref = "FullStatusTestResponse" /> object indicating the result of the /status request.</returns>
        public static async Task<FullStatusTestResponse> GET_FullStatus(string gameToken)
        {
            using (HttpClient client = CreateClient())
            {
                String url = String.Format("/Boggle.svc/status?gameToken={0}", Uri.EscapeDataString(gameToken));
                HttpResponseMessage response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    return new FullStatusTestResponse
                    {
                        ResponseCode = response.StatusCode,
                        Response = null
                    };
                }

                return new FullStatusTestResponse
                {
                    ResponseCode = response.StatusCode,
                    Response = JsonConvert.DeserializeObject<FullStatus>(await response.Content.ReadAsStringAsync())
                };
            }
        }

        /// <summary>
        ///     Issues a /briefstatus GET call to a <see cref = "BoggleService" /> and returns the result.
        /// </summary>
        /// <param name = "gameToken">A token corresponding to an existing game in the <see cref = "BoggleService" />.</param>
        /// <returns>A <see cref = "BriefStatusTestResponse" /> object indicating the result of the /briefstatus request.</returns>
        public static async Task<BriefStatusTestResponse> GET_BriefStatus(string gameToken)
        {
            using (HttpClient client = CreateClient())
            {
                String url = String.Format("/Boggle.svc/briefstatus?gameToken={0}", Uri.EscapeDataString(gameToken));
                HttpResponseMessage response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    return new BriefStatusTestResponse
                    {
                        ResponseCode = response.StatusCode,
                        Response = null
                    };
                }

                return new BriefStatusTestResponse
                {
                    ResponseCode = response.StatusCode,
                    Response = JsonConvert.DeserializeObject<BriefStatus>(await response.Content.ReadAsStringAsync())
                };
            }
        }

        /// <summary>
        ///     Issues a /playword POST call to a <see cref = "BoggleService" /> and returns the result.
        /// </summary>
        /// <param name = "playWord">A <see cref = "PlayWordPOST" /> object representing the word to be played.</param>
        /// <returns>A <see cref = "PlayWordTestResponse" /> object indicating the result of the /playword request.</returns>
        public static async Task<PlayWordTestResponse> POST_PlayWord(PlayWordPOST playWord)
        {
            using (HttpClient client = CreateClient())
            {
                StringContent content = new StringContent(JsonConvert.SerializeObject(playWord), Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync("/Boggle.svc/playword", content);

                if (!response.IsSuccessStatusCode)
                {
                    return new PlayWordTestResponse
                    {
                        ResponseCode = response.StatusCode,
                        Response = null
                    };
                }

                return new PlayWordTestResponse
                {
                    ResponseCode = response.StatusCode,
                    Response = JsonConvert.DeserializeObject<PlayWordResponse>(await response.Content.ReadAsStringAsync())
                };
            }
        }
    }
}
