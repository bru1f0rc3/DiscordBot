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

namespace ConsoleApp1
{
    internal class Program
    {
        static DiscordSocketClient client;
        static List<SocketTextChannel> textChannels;
        static TimeZoneInfo moscowTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");
        static List<string> lastCheckedMatchIds = new List<string>();

        static async Task Main()
        {
            client = new DiscordSocketClient();

            client.Ready += ClientReadyAsync;

            var token = "MTIwNjEzODcwODY3MjM4NTAzNA.GH4-DE.lR7QgmAX1MbI5uneNX04DgfMY-k-Yobvz5RRVM";

            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();

            while (true)
            {
                List<string> newMatchIds = await GetNewMatchIds();
                if (newMatchIds.Count > 0)
                {
                    List<string> playerIds = await GetPlayerIdsFromMatches(newMatchIds);
                    if (playerIds.Count > 0)
                    {
                        DateTimeOffset currentTime = DateTimeOffset.UtcNow;
                        DateTimeOffset moscowTime = TimeZoneInfo.ConvertTime(currentTime, moscowTimeZone);

                        string messageContent = $"Никнеймы игроков из новых матчей EGGWARS ({moscowTime}):";
                        List<string> playerNicknames = await GetPlayerNicknames(playerIds);
                        foreach (string nickname in playerNicknames)
                        {
                            messageContent += $"\n{nickname}";
                        }

                        foreach (var textChannel in textChannels)
                        {
                            await textChannel.SendMessageAsync(messageContent);
                        }

                        Console.WriteLine("Сообщение отправлено в Discord и выведено в консоли.");
                    }
                    else
                    {
                        Console.WriteLine("Не удалось получить никнеймы игроков.");
                    }

                    lastCheckedMatchIds.AddRange(newMatchIds);
                }
                else
                {
                    Console.WriteLine("Новые матчи не найдены.");
                }
                await Task.Delay(1000);
            }
        }

        private static async Task<List<string>> GetNewMatchIds()
        {
            List<string> newMatchIds = new List<string>();

            using (HttpClient client = new HttpClient())
            {
                DateTimeOffset currentTime = DateTimeOffset.UtcNow;
                long unixTimestamp = currentTime.ToUnixTimeMilliseconds();

                string vimeUrl = $"https://api.vimeworld.com/match/list?count=1&after={unixTimestamp}";
                HttpResponseMessage response = await client.GetAsync(vimeUrl);
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();
                JArray result = JArray.Parse(responseBody);

                foreach (JObject match in result)
                {
                    JToken gameToken = match["game"];
                    if (gameToken != null && gameToken.ToString() == "EGGWARS")
                    {
                        string matchId = match["id"].ToString();
                        if (!lastCheckedMatchIds.Contains(matchId))
                        {
                            newMatchIds.Add(matchId);
                        }
                    }
                }
            }

            return newMatchIds;
        }

        private static async Task<List<string>> GetPlayerIdsFromMatches(List<string> matchIds)
        {
            List<string> playerIds = new List<string>();

            using (HttpClient client = new HttpClient())
            {
                foreach (string matchId in matchIds)
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
                            playerIds.Add(memberId.ToString());
                        }
                    }
                }
            }

            return playerIds;
        }

        private static async Task ClientReadyAsync()
        {
            var guild = client.GetGuild(1206137807043694632);
            textChannels = guild.TextChannels.ToList();
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