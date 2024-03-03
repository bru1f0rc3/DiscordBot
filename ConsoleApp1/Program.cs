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
                (List<string> bluePlayers, List<string> redPlayers) = await GetPlayerIdsFromLatestMatches();
                List<string> bluePlayerNicknames = await GetPlayerNicknames(bluePlayers);
                List<string> redPlayerNicknames = await GetPlayerNicknames(redPlayers);

                if (!isFirstRun)
                {
                    string blueTeamMessage = FormatPlayerList("blue", bluePlayerNicknames);
                    string redTeamMessage = FormatPlayerList("red", redPlayerNicknames);

                    var guild = client.GetGuild(1206137807043694632);
                    var textChannels = guild.TextChannels;

                    foreach (var textChannel in textChannels)
                    {
                        await textChannel.SendMessageAsync(blueTeamMessage);
                        await textChannel.SendMessageAsync(redTeamMessage);
                    }
                }

                isFirstRun = false;
                await Task.Delay(TimeSpan.FromSeconds(5)); // 1:30 minutes
            }
        }

        private static string FormatPlayerList(string teamName, List<string> playerNicknames)
        {
            string messageContent = $"Никнеймы игроков из новых матчей EGGWARS команды {teamName}:\n";
            foreach (string nickname in playerNicknames)
            {
                messageContent += $"```FIX\n{nickname}\n```";
            }

            return messageContent;
        }

        private static async Task<(List<string>, List<string>)> GetPlayerIdsFromLatestMatches()
        {
            List<string> bluePlayers = new List<string>();
            List<string> redPlayers = new List<string>();

            using (HttpClient client = new HttpClient())
            {
                string url = "https://api.vimeworld.ru/match/latest?count=100";
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();
                dynamic result = JsonConvert.DeserializeObject<dynamic>(responseBody);

                foreach (var match in result)
                {
                    if (match.game.ToString() == "EGGWARS")
                    {
                        string matchId = match.id.ToString();
                        (List<string> matchBluePlayers, List<string> matchRedPlayers) = await GetPlayerIdsFromMatch(matchId);
                        bluePlayers.AddRange(matchBluePlayers);
                        redPlayers.AddRange(matchRedPlayers);
                    }
                }
            }

            return (bluePlayers, redPlayers);
        }

        private static async Task<(List<string>, List<string>)> GetPlayerIdsFromMatch(string matchId)
        {
            List<string> bluePlayers = new List<string>();
            List<string> redPlayers = new List<string>();

            using (HttpClient client = new HttpClient())
            {
                string url = $"https://api.vimeworld.ru/match/{matchId}";
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();
                dynamic result = JsonConvert.DeserializeObject<dynamic>(responseBody);

                foreach (var team in result.teams)
                {
                    foreach (var memberId in team.members)
                    {
                        string playerId = memberId.ToString();
                        if (team.id.ToString() == "blue")
                        {
                            bluePlayers.Add(playerId);
                        }
                        else if (team.id.ToString() == "red")
                        {
                            redPlayers.Add(playerId);
                        }
                    }
                }
            }

            return (bluePlayers, redPlayers);
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

                    if (result != null && result.HasValues && result[0] != null && result[0].username != null)
                    {
                        string nickname = result[0].username.ToString();
                        playerNicknames.Add(nickname);
                    }
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