using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using PlanningPoker.Common.Constants;
using PlanningPoker.Service.Services;

namespace PlanningPoker.UnitTests.Api;

public class SweepEndpointTests : IClassFixture<FakeClockApiFactory>
{
    private readonly FakeClockApiFactory _factory;

    public SweepEndpointTests(FakeClockApiFactory factory)
    {
        _factory = factory;
    }

    private Task<HttpResponseMessage> Get(string url, string cookie)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Cookie", cookie);
        return _factory.CreateClient().SendAsync(request);
    }

    private static async Task<string?> FirstErrorCode(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("errors")[0].GetProperty("code").GetString();
    }

    [Fact]
    public async Task Sweep_AbandonedPersonAndRoomPastTtl_RoomIs404AndMeIsNoSession()
    {
        var cookie = await _factory.MintCookie("Ann");
        var roomId = await _factory.CreateRoom(cookie);
        Assert.Equal(HttpStatusCode.OK, (await Get($"/api/rooms/{roomId}", cookie)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Get("/api/me", cookie)).StatusCode);

        _factory.Time.Advance(SweepConstants.AbandonedAfter + TimeSpan.FromMinutes(1));
        // The hosted loop also wakes when the clock moves, so it may have swept first; the outcome is the same either way.
        _factory.Services.GetRequiredService<ISweepService>().Sweep();

        var room = await Get($"/api/rooms/{roomId}", cookie);
        Assert.Equal(HttpStatusCode.NotFound, room.StatusCode);
        Assert.Equal("room_not_found", await FirstErrorCode(room));
        var me = await Get("/api/me", cookie);
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
        Assert.Equal("no_session", await FirstErrorCode(me));
    }

    [Fact]
    public async Task Sweep_BeforeTtl_KeepsRoomAndPerson()
    {
        var cookie = await _factory.MintCookie("Bea");
        var roomId = await _factory.CreateRoom(cookie);

        _factory.Time.Advance(SweepConstants.AbandonedAfter - TimeSpan.FromMinutes(1));
        _factory.Services.GetRequiredService<ISweepService>().Sweep();

        Assert.Equal(HttpStatusCode.OK, (await Get($"/api/rooms/{roomId}", cookie)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Get("/api/me", cookie)).StatusCode);
    }

    [Fact]
    public async Task BackgroundLoop_ClockPassesTtlAndInterval_SweepsWithoutBeingCalled()
    {
        var cookie = await _factory.MintCookie("Cy");
        var roomId = await _factory.CreateRoom(cookie);

        _factory.Time.Advance(SweepConstants.AbandonedAfter + SweepConstants.Interval);

        await TestSession.WaitUntilAsync(async () => (await Get($"/api/rooms/{roomId}", cookie)).StatusCode == HttpStatusCode.NotFound, TimeSpan.FromSeconds(10));
        await TestSession.WaitUntilAsync(async () => (await Get("/api/me", cookie)).StatusCode == HttpStatusCode.Unauthorized, TimeSpan.FromSeconds(10));
    }
}
