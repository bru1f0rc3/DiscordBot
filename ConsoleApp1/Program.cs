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
using System.Linq;
using System.IO;

namespace ConsoleApp1
{
    internal class Program
    {
        static DiscordSocketClient client;
        static bool isFirstRun = true;

        static async Task Main()
        {
            client = new DiscordSocketClient();
            client.Log += LogAsync;
            client.Ready += ClientReadyAsync; 

            string path = "tokens.txt";
            string token = File.ReadAllText(path);

            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();

            await Task.Delay(-1);
        }

        private static async Task ClientReadyAsync()
        {
            while (true)
            {
                List<string> playerIds = await GetPlayerIdsFromLatestMatches();
                List<string> playerNicknames = await GetPlayerNicknames(playerIds);

                if (!isFirstRun)
                {
                    string messageContent = "Никнеймы игроков из новых матчей EGGWARS:\n";
                    foreach (string nickname in playerNicknames)
                    {
                        messageContent += $"```FIX\n{nickname}\n```";
                    }

                    var guild = client.GetGuild(1206137807043694632);
                    var textChannels = guild.TextChannels;

                    foreach (var textChannel in textChannels)
                    {
                        await textChannel.SendMessageAsync(messageContent);
                    }
                }

                isFirstRun = false;
                await Task.Delay(TimeSpan.FromSeconds(90)); // 1:30 минуты
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

        private static Task LogAsync(LogMessage logMessage)
        {
            Console.WriteLine(logMessage.ToString());
            return Task.CompletedTask;
        }
    }
}