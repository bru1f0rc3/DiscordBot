using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ConsoleApp1
{
    internal class Program
    {
        static async Task Main()
        {
            List<string> playerIds = await GetPlayerIdsFromLatestMatches();
            List<string> playerNicknames = await GetPlayerNicknames(playerIds);

            Console.WriteLine("Никнеймы игроков из матчей EGGWARS:");
            foreach (string nickname in playerNicknames)
            {
                Console.WriteLine(nickname);
            }
        }

        private static async Task<List<string>> GetPlayerIdsFromLatestMatches()
        {
            using (HttpClient client = new HttpClient())
            {
                string url = "https://api.vimeworld.ru/match/latest?count=100";
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();
                dynamic result = JsonConvert.DeserializeObject<dynamic>(responseBody);

                List<string> playerIds = new List<string>();
                foreach (var match in result)
                {
                    if (match.game.ToString() == "EGGWARS")
                    {
                        string matchId = match.id.ToString();
                        List<string> matchPlayerIds = await GetPlayerIdsFromMatch(matchId);
                        playerIds.AddRange(matchPlayerIds);
                    }
                }

                return playerIds;
            }
        }

        private static async Task<List<string>> GetPlayerIdsFromMatch(string matchId)
        {
            using (HttpClient client = new HttpClient())
            {
                string url = $"https://api.vimeworld.ru/match/{matchId}";
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();
                dynamic result = JsonConvert.DeserializeObject<dynamic>(responseBody);

                List<string> playerIds = new List<string>();
                foreach (var team in result.teams)
                {
                    foreach (var memberId in team.members)
                    {
                        playerIds.Add(memberId.ToString());
                    }
                }

                return playerIds;
            }
        }

        private static async Task<List<string>> GetPlayerNicknames(List<string> playerIds)
        {
            using (HttpClient client = new HttpClient())
            {
                string baseUrl = "https://api.vimeworld.ru/user/";

                List<string> playerNicknames = new List<string>();
                foreach (string playerId in playerIds)
                {
                    string url = baseUrl + playerId;
                    HttpResponseMessage response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    string responseBody = await response.Content.ReadAsStringAsync();
                    dynamic result = JsonConvert.DeserializeObject<dynamic>(responseBody);

                    await Task.Delay(500);

                    string nickname = result[0].username.ToString();
                    playerNicknames.Add(nickname);
                }

                return playerNicknames;
            }
        }
    }
}