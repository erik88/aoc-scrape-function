using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Aoc2023;

public class ScrapeLeaderboard
{
    private static HttpClient httpClient = new HttpClient();

    [FunctionName("ScrapeLeaderboard")]
    public async Task Run([TimerTrigger("0 0 * * * *")]TimerInfo myTimer, [Blob("aoc2023container/output.json", FileAccess.Write)] Stream outputStream)
    // public async Task Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")]HttpRequest reg, [Blob("aoc2023container/output.json", FileAccess.Write)] Stream outputStream)
    {
        var envSessionCookie = Environment.GetEnvironmentVariable("SESSION_COOKIE", EnvironmentVariableTarget.Process);
        var envUseDummyData = Environment.GetEnvironmentVariable("USE_DUMMY_DATA", EnvironmentVariableTarget.Process);

        var html = string.IsNullOrEmpty(envUseDummyData)
            ? await GetDataFromHttpRequest(envSessionCookie)
            : File.ReadAllText("sample-input.html");
        
        var players = ParseHtmlIntoPlayers(html);

        var jsonData = CreateJsonFromPlayers(players, DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        await WriteToBlob(jsonData, outputStream);
    }

    private async Task<String> GetDataFromHttpRequest(String sessionCookie) {
        var httpRequestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri("https://adventofcode.com/2023/leaderboard/private/view/1550443"),
            Headers = { 
                { "Cookie", sessionCookie }
            }
        };
        var response = await httpClient.SendAsync(httpRequestMessage);
        if (!response.IsSuccessStatusCode) {
            throw new Exception("Failed scraping "+response.StatusCode);
        }
        return await response.Content.ReadAsStringAsync();
    }

    private List<Player> ParseHtmlIntoPlayers(string html) {
        var parser = new HtmlParser();
        var document = parser.ParseDocument(html);

        var players = new List<Player>();

        var skippedFirst = false;
        foreach (var element in document.QuerySelectorAll(".privboard-row"))
        {
            if (!skippedFirst) {
                skippedFirst = true;
                continue;
            }

            var text = element.TextContent;

            var nameRegex = new Regex(@"\*\s+(.+)$");
            var nameMatch = nameRegex.Match(text);
            var name = nameMatch.Groups[1].Value.Split(" (AoC++)")[0];
            
		    var scoreRegex = new Regex(@"\ (\d+)\ \*");
            var scoreMatch = scoreRegex.Match(text);
		    var score = Int32.Parse(scoreMatch.Groups[1].Value);

		    var stars =
			    element.QuerySelectorAll(".privboard-star-both").Length * 2 +
			    element.QuerySelectorAll(".privboard-star-firstonly").Length;

		    players.Add(new Player(name, score, stars));
        }

        return players;
    }

    private string CreateJsonFromPlayers(List<Player> players, long lastUpdated) {
        return JsonSerializer.Serialize(new Output() {
            LastUpdated = lastUpdated,
            Players = players
        }, new JsonSerializerOptions(){
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    private async Task WriteToBlob(String jsonData, Stream outputStream) {
        var streamWriter = new StreamWriter(outputStream, Encoding.UTF8);
        await streamWriter.WriteAsync(jsonData);
        await streamWriter.FlushAsync();
    }
}

class Output {
    public long LastUpdated {get; set;}
    public ICollection<Player> Players {get; set;}
}

record Player(string Name, int Score, int Stars);
