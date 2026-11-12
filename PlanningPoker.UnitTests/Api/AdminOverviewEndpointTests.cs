using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using PlanningPoker.Repository.Models;
using PlanningPoker.Repository.Repositories;

namespace PlanningPoker.UnitTests.Api;

public class AdminOverviewEndpointTests(AdminApiFactory factory) : IClassFixture<AdminApiFactory>
{
    private async Task<HttpClient> SignedIn()
    {
        var client = factory.CreateBrowser();
        var response = await client.PostAsJsonAsync("/api/admin/login", new { username = AdminApiFactory.Username, password = AdminApiFactory.Password });
        response.EnsureSuccessStatusCode();
        return client;
    }

    private void AddRoomWithVotes(string id)
    {
        var owner = Guid.NewGuid();
        var member = Guid.NewGuid();
        var people = factory.Services.GetRequiredService<IPersonRepository>();
        people.TryAdd(new PersonEntity { Guid = owner, Name = "Ann", PersonType = "dev" });
        people.TryAdd(new PersonEntity { Guid = member, Name = "Bob", PersonType = "test" });
        factory.Services.GetRequiredService<IRoomRepository>().TryAdd(new RoomEntity
        {
            Id = id,
            OwnerGuid = owner,
            MemberGuids = [owner, member],
            VotingEnabled = true,
            Votes = new Dictionary<Guid, string> { [owner] = "SECRET-13" },
            DepartedVotes = new Dictionary<Guid, string> { [member] = "SECRET-8" },
            CreatedAt = DateTimeOffset.UtcNow,
            LastActivityAt = DateTimeOffset.UtcNow
        });
    }

    [Fact]
    public async Task Overview_NotSignedIn_Returns401()
    {
        var response = await factory.CreateBrowser().GetAsync("/api/admin/overview");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("unauthorized", body.GetProperty("errors")[0].GetProperty("code").GetString());
    }

    [Fact]
    public async Task Overview_SignedIn_ReturnsStatsRoomsAndIssues()
    {
        AddRoomWithVotes("overview-room");
        var client = await SignedIn();

        var response = await client.GetAsync("/api/admin/overview");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
        Assert.True(data.GetProperty("stats").GetProperty("rooms").GetProperty("total").GetInt32() >= 1);
        Assert.True(data.GetProperty("stats").TryGetProperty("uptimeSeconds", out _));
        Assert.True(data.GetProperty("stats").GetProperty("lastStoreWrite").TryGetProperty("succeeded", out _));
        var room = data.GetProperty("rooms").EnumerateArray().Single(r => r.GetProperty("id").GetString() == "overview-room");
        Assert.Equal("voting", room.GetProperty("state").GetString());
        Assert.Equal(["Ann", "Bob"], room.GetProperty("people").EnumerateArray().Select(p => p.GetProperty("name").GetString()!));
        Assert.Equal(JsonValueKind.Array, data.GetProperty("issues").ValueKind);
    }

    [Fact]
    public async Task Overview_NeverContainsVoteData()
    {
        AddRoomWithVotes("secret-room");
        var client = await SignedIn();

        var text = await (await client.GetAsync("/api/admin/overview")).Content.ReadAsStringAsync();

        Assert.DoesNotContain("SECRET", text);
        Assert.DoesNotContain("vote", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Overview_AfterSigningOut_Returns401Again()
    {
        var client = await SignedIn();
        var session = await (await client.GetAsync("/api/admin/session")).Content.ReadFromJsonAsync<JsonElement>();
        var token = session.GetProperty("data").GetProperty("csrfToken").GetString()!;
        var logout = new HttpRequestMessage(HttpMethod.Post, "/api/admin/logout") { Content = JsonContent.Create(new { }) };
        logout.Headers.Add("X-XSRF-TOKEN", token);
        await client.SendAsync(logout);

        var response = await client.GetAsync("/api/admin/overview");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
