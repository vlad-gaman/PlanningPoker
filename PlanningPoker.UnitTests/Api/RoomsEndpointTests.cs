using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace PlanningPoker.UnitTests.Api;

public class RoomsEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private async Task<HttpResponseMessage> Post(object body, string? cookie)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/rooms") { Content = JsonContent.Create(body) };
        if (cookie is not null)
        {
            request.Headers.Add("Cookie", cookie);
        }

        return await factory.CreateClient().SendAsync(request);
    }

    private static object Body(string cardSet = "linear", int countdown = 5, int max = 50, bool fun = false)
    {
        return new { useFunRoomName = fun, configuration = new { cardSet, countdownSeconds = countdown, showFireworks = true, maxParticipants = max } };
    }

    private static async Task<JsonElement> Json(HttpResponseMessage response)
    {
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    [Fact]
    public async Task Post_WithGuidName_Returns201AndGuidId()
    {
        var response = await Post(Body(), await factory.MintCookie());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var id = (await Json(response)).GetProperty("data").GetProperty("id").GetString();
        Assert.True(Guid.TryParse(id, out _));
    }

    [Fact]
    public async Task Post_WithFunName_Returns201AndReadableId()
    {
        var response = await Post(Body(fun: true), await factory.MintCookie());

        var id = (await Json(response)).GetProperty("data").GetProperty("id").GetString()!;
        Assert.Matches(@"^\d{1,2}_[A-Za-z0-9-]+_[A-Za-z0-9-]+_[A-Za-z0-9-]+$", id);
    }

    [Fact]
    public async Task Post_WithoutSession_Returns401()
    {
        var response = await Post(Body(), null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("no_session", (await Json(response)).GetProperty("errors")[0].GetProperty("code").GetString());
    }

    [Theory]
    [InlineData("tarot", 5, 50, "configuration.cardSet")]
    [InlineData("linear", -1, 50, "configuration.countdownSeconds")]
    [InlineData("linear", 31, 50, "configuration.countdownSeconds")]
    [InlineData("linear", 5, 0, "configuration.maxParticipants")]
    public async Task Post_InvalidConfiguration_Returns400WithField(string cardSet, int countdown, int max, string field)
    {
        var response = await Post(Body(cardSet, countdown, max), await factory.MintCookie());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = (await Json(response)).GetProperty("errors")[0];
        Assert.Equal("validation_failed", error.GetProperty("code").GetString());
        Assert.Equal(field, error.GetProperty("metadata").GetProperty("field").GetString());
    }

    [Fact]
    public async Task Get_ExistingRoom_Returns200()
    {
        var id = await factory.CreateRoom(await factory.MintCookie());

        var response = await factory.CreateClient().GetAsync($"/api/rooms/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = (await Json(response)).GetProperty("data");
        Assert.Equal(id, data.GetProperty("id").GetString());
        Assert.True(data.GetProperty("exists").GetBoolean());
    }

    [Fact]
    public async Task Get_UnknownRoom_Returns404RoomNotFound()
    {
        var response = await factory.CreateClient().GetAsync("/api/rooms/nope");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("room_not_found", (await Json(response)).GetProperty("errors")[0].GetProperty("code").GetString());
    }
}
