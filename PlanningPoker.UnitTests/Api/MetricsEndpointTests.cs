using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.SignalR.Client;
using PlanningPoker.Common.Constants;

namespace PlanningPoker.UnitTests.Api;

[Collection("Metrics host")]
public class MetricsEndpointTests(TokenMetricsApiFactory withToken) : IClassFixture<TokenMetricsApiFactory>
{
    private static HttpRequestMessage Scrape(string? authorization)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/metrics");
        if (authorization is not null)
        {
            request.Headers.TryAddWithoutValidation("Authorization", authorization);
        }

        return request;
    }

    private static string Bearer => $"Bearer {MetricsApiFactory.ScrapeToken}";

    private static async Task<double> SampleValue(HttpResponseMessage response, string seriesPattern)
    {
        var body = await response.Content.ReadAsStringAsync();
        var match = Regex.Match(body, $@"^{seriesPattern} (?<value>\S+)", RegexOptions.Multiline);
        Assert.True(match.Success, $"No series matching {seriesPattern} in:\n{body}");
        return double.Parse(match.Groups["value"].Value, System.Globalization.CultureInfo.InvariantCulture);
    }

    [Fact]
    public async Task Metrics_NoToken_Returns401WithChallenge()
    {
        var response = await withToken.CreateClient().SendAsync(Scrape(null));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("Bearer", response.Headers.WwwAuthenticate.Select(h => h.Scheme));
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(ErrorCodes.Unauthorized, body.GetProperty("errors")[0].GetProperty("code").GetString());
    }

    [Fact]
    public async Task Metrics_WrongToken_Returns401()
    {
        var response = await withToken.CreateClient().SendAsync(Scrape("Bearer wrong"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Metrics_CorrectToken_ReturnsPrometheusText()
    {
        var client = withToken.CreateClient();
        await client.GetAsync("/api/card-sets");

        var response = await client.SendAsync(Scrape(Bearer));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var mediaType = response.Content.Headers.ContentType!.MediaType;
        Assert.Equal("text/plain", mediaType);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("<html", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("planningpoker_rooms_in_memory", body);
        Assert.Contains("http_server_request_duration_seconds", body);
    }

    [Fact]
    public async Task Metrics_ScrapeRequestsAreNotCountedAsHttpRequests()
    {
        var client = withToken.CreateClient();
        await client.SendAsync(Scrape(Bearer));

        var body = await (await client.SendAsync(Scrape(Bearer))).Content.ReadAsStringAsync();

        Assert.DoesNotContain("http_route=\"/metrics\"", body);
    }

    [Fact]
    public async Task Metrics_EndToEnd_CreatingARoomAndVotingAreCounted()
    {
        var cookie = await withToken.MintCookie("Ann");
        var roomId = await withToken.CreateRoom(cookie);
        await using var connection = withToken.Connect(cookie);
        await connection.StartAsync();
        var joined = await connection.InvokeAsync<JsonElement>("JoinRoom", roomId);
        Assert.Equal(JsonValueKind.Null, joined.GetProperty("errors").ValueKind);
        var voted = await connection.InvokeAsync<JsonElement>("Vote", "5");
        Assert.Equal(JsonValueKind.Null, voted.GetProperty("errors").ValueKind);

        var client = withToken.CreateClient();
        client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(Bearer);
        var response = await client.GetAsync("/metrics");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(await SampleValue(response, @"planningpoker_rooms_created_total\{[^}]*\}") >= 1);
        Assert.True(await SampleValue(response, @"planningpoker_votes_cast_total\{[^}]*card_set=""linear""[^}]*\}") >= 1);
    }
}
