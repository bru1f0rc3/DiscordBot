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
using System.Text;

namespace ConsoleApp1
{
    internal class Program
    {
        static DiscordSocketClient client;
        static bool isFirstRun = true;
        static Dictionary<string, Tuple<List<string>, List<string>>> matchPlayerIds = new Dictionary<string, Tuple<List<string>, List<string>>>();

        static async Task Main()
        {
            client = new DiscordSocketClient();
            client.Log += LogAsync;
            client.Ready += ClientReadyAsync;

            string path = "tokens.txt";
            string token = File.ReadAllText(path).Trim();

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

                foreach (var match in matchPlayerIds)
                {
                    string matchId = match.Key;
                    var (matchBluePlayers, matchRedPlayers) = match.Value;

                    matchBluePlayers.AddRange(bluePlayerNicknames.Where(p => matchBluePlayers.Contains(p)));
                    matchRedPlayers.AddRange(redPlayerNicknames.Where(p => matchRedPlayers.Contains(p)));

                    string blueTeamMessage = FormatPlayerList("blue", matchBluePlayers);
                    string redTeamMessage = FormatPlayerList("red", matchRedPlayers);

                    var guild = client.GetGuild(1206137807043694632);
                    var textChannels = guild.TextChannels;

                    foreach (var textChannel in textChannels)
                    {
                        await textChannel.SendMessageAsync($"Match ID: {matchId}");
                        await textChannel.SendMessageAsync($"```FIX\n{blueTeamMessage}\n```");
                        await textChannel.SendMessageAsync($"```md\n#{redTeamMessage}\n```");
                    }

                    // Очистка списков matchBluePlayers и matchRedPlayers
                    matchBluePlayers.Clear();
                    matchRedPlayers.Clear();
                }

                if (!isFirstRun)
                {
                    foreach (var match in matchPlayerIds)
                    {
                        string matchId = match.Key;
                        var (matchBluePlayers, matchRedPlayers) = match.Value;

                        List<string> matchBluePlayerNicknames = await GetPlayerNicknames(matchBluePlayers);
                        List<string> matchRedPlayerNicknames = await GetPlayerNicknames(matchRedPlayers);

                        string blueTeamMessage = FormatPlayerList("blue", matchBluePlayerNicknames);
                        string redTeamMessage = FormatPlayerList("red", matchRedPlayerNicknames);

                        var guild = client.GetGuild(1206137807043694632);
                        var textChannels = guild.TextChannels;

                        foreach (var textChannel in textChannels)
                        {
                            await textChannel.SendMessageAsync($"Match ID: {matchId}");
                            await textChannel.SendMessageAsync($"```FIX\n{blueTeamMessage}\n```");
                            await textChannel.SendMessageAsync($"```md\n#{redTeamMessage}\n```");
                        }
                    }
                }

                isFirstRun = false;

                await Task.Delay(TimeSpan.FromSeconds(90));
            }
        }

        private static string FormatPlayerList(string team, List<string> players)
        {
            StringBuilder messageContent = new StringBuilder();
            messageContent.AppendLine($"{team.ToUpper()} Team:");

            foreach (var player in players)
            {
                messageContent.AppendLine((string)player);
            }

            return messageContent.ToString();
        }

        private static async Task<(List<string>, List<string>)> GetPlayerIdsFromMatch(string matchId)
        {
            List<string> bluePlayers = new List<string>();
            List<string> redPlayers = new List<string>();

            using (HttpClient httpClient = new HttpClient())
            {
                string url = $"https://api.vimeworld.ru/match/{matchId}";
                HttpResponseMessage response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();
                dynamic result = JsonConvert.DeserializeObject<dynamic>(responseBody);

                foreach (var team in result.teams)
                {
                    string teamId = team.id.ToString();

                    foreach (var memberId in team.members)
                    {
                        string playerId = memberId.ToString();

                        if (teamId == "blue")
                        {
                            bluePlayers.Add(playerId);
                        }
                        else if (teamId == "red")
                        {
                            redPlayers.Add(playerId);
                        }
                    }
                }
            }

            return (bluePlayers, redPlayers);
        }

        private static async Task<(List<string>, List<string>)> GetPlayerIdsFromLatestMatches()
        {
            List<string> bluePlayers = new List<string>();
            List<string> redPlayers = new List<string>();

            using (HttpClient httpClient = new HttpClient())
            {
                string url = "https://api.vimeworld.ru/match/latest?count=100";
                HttpResponseMessage response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();
                dynamic result = JsonConvert.DeserializeObject<dynamic>(responseBody);

                foreach (var match in result)
                {
                    string gameId = match.game.ToString();

                    if (gameId == "EGGWARS")
                    {
                        string matchId = match.id.ToString();
                        (List<string> matchBluePlayers, List<string> matchRedPlayers) = await GetPlayerIdsFromMatch(matchId);
                        bluePlayers.AddRange(matchBluePlayers);
                        redPlayers.AddRange(matchRedPlayers);

                        // Если матча нет в словаре, добавляем его
                        if (!matchPlayerIds.ContainsKey(matchId))
                        {
                            matchPlayerIds.Add(matchId, Tuple.Create(matchBluePlayers, matchRedPlayers));
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
                    dynamic result = JsonConvert.DeserializeObject(responseBody);

                    if (result != null && result[0] != null && result[0].username != null)
                    {
                        string nickname = result[0].username.ToString();
                        playerNicknames.Add(nickname);
                    }
                }

                return playerNicknames;
            }
        }

        private static Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }
    }
}