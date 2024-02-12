using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Discord;
using Discord.WebSocket;

namespace ConsoleApp1
{
    internal class Program
    {
        static DiscordSocketClient client;

        static async Task Main()
        {
            List<string> playerIds = await GetPlayerIdsFromLatestMatches();
            List<string> playerNicknames = await GetPlayerNicknames(playerIds);

            Console.WriteLine("Никнеймы игроков из матчей EGGWARS:");
            foreach (string nickname in playerNicknames)
            {
                Console.WriteLine(nickname);
            }

            client = new DiscordSocketClient();

            client.Ready += ClientReadyAsync;

            var token = "MTIwNjEzODcwODY3MjM4NTAzNA.GH4-DE.lR7QgmAX1MbI5uneNX04DgfMY-k-Yobvz5RRVM";

            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();

            await Task.Delay(-1);
        }

        private static async Task ClientReadyAsync()
        {
            var guild = client.GetGuild(1206137807043694632);

            var textChannels = guild.TextChannels;

            string messageContent = "Никнеймы игроков из матчей EGGWARS:\n";
            List<string> playerIds = await GetPlayerIdsFromLatestMatches();
            List<string> playerNicknames = await GetPlayerNicknames(playerIds);
            foreach (string nickname in playerNicknames)
            {
                messageContent += $"{nickname}\n";
            }

            foreach (var textChannel in textChannels)
            {
                await textChannel.SendMessageAsync(messageContent);
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

                    await Task.Delay(25);

                    string nickname = result[0].username.ToString();
                    playerNicknames.Add(nickname);
                }

                return playerNicknames;
            }
        }
    }
}