using System.Net;
using Microsoft.Extensions.DependencyInjection;
using PlanningPoker.Common.Constants;
using PlanningPoker.Controller.Models;
using PlanningPoker.Service.Services;

namespace PlanningPoker.UnitTests.Api;

public class ForwardedHeadersTrustTests
{
    private const string Forwarded = "203.0.113.50";

    private static HttpRequestMessage SignIn(string sender)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/admin/login")
        {
            Content = new StringContent($$"""{"username":"operator","password":"wrong password"}""", System.Text.Encoding.UTF8, "application/json")
        };
        request.Headers.Add(ForwardedHeadersApiFactory.SenderHeader, sender);
        request.Headers.Add("X-Forwarded-For", Forwarded);
        request.Headers.Add("X-Forwarded-Proto", "https");
        return request;
    }

    private static async Task<(HttpStatusCode Status, string? LoggedAddress)> SignInFrom(ForwardedHeadersApiFactory factory, string sender)
    {
        var response = await factory.CreateRaw().SendAsync(SignIn(sender));
        var logged = factory.Services.GetRequiredService<IIssueLog>().Recent(50).FirstOrDefault(issue => issue.IpAddress is not null)?.IpAddress;
        return (response.StatusCode, logged);
    }

    [Fact]
    public async Task Unset_AForwardedHeaderFromAnUntrustedAddress_IsIgnored()
    {
        using var factory = new ForwardedHeadersApiFactory(trustAnySender: false);

        var (status, logged) = await SignInFrom(factory, "198.51.100.7");

        Assert.Equal(HttpStatusCode.Unauthorized, status);
        Assert.Equal("198.51.100.7", logged);
    }

    [Fact]
    public async Task Unset_AForwardedHeaderFromLoopback_IsHonoured()
    {
        using var factory = new ForwardedHeadersApiFactory(trustAnySender: false);

        var (status, logged) = await SignInFrom(factory, "127.0.0.1");

        Assert.Equal(HttpStatusCode.Unauthorized, status);
        Assert.Equal(Forwarded, logged);
    }

    [Fact]
    public async Task TrustAnySender_HonoursAForwardedHeaderFromAnyAddress()
    {
        using var factory = new ForwardedHeadersApiFactory(trustAnySender: true);

        var (status, logged) = await SignInFrom(factory, "198.51.100.7");

        Assert.Equal(HttpStatusCode.Unauthorized, status);
        Assert.Equal(Forwarded, logged);
    }

    [Fact]
    public async Task AKnownProxy_IsHonoured_AndAnotherAddressIsNot()
    {
        using var trustedHost = new ForwardedHeadersApiFactory(trustAnySender: false, knownProxies: ["198.51.100.7"]);
        using var untrustedHost = new ForwardedHeadersApiFactory(trustAnySender: false, knownProxies: ["198.51.100.7"]);

        var trusted = await SignInFrom(trustedHost, "198.51.100.7");
        var untrusted = await SignInFrom(untrustedHost, "198.51.100.8");

        Assert.Equal(HttpStatusCode.Unauthorized, trusted.Status);
        Assert.Equal(Forwarded, trusted.LoggedAddress);
        Assert.Equal(HttpStatusCode.Unauthorized, untrusted.Status);
        Assert.Equal("198.51.100.8", untrusted.LoggedAddress);
    }

    [Fact]
    public async Task AKnownNetwork_IsHonoured_AndAddressesOutsideItAreNot()
    {
        using var trustedHost = new ForwardedHeadersApiFactory(trustAnySender: false, knownNetworks: ["198.51.100.0/24"]);
        using var untrustedHost = new ForwardedHeadersApiFactory(trustAnySender: false, knownNetworks: ["198.51.100.0/24"]);

        var trusted = await SignInFrom(trustedHost, "198.51.100.200");
        var untrusted = await SignInFrom(untrustedHost, "192.0.2.1");

        Assert.Equal(HttpStatusCode.Unauthorized, trusted.Status);
        Assert.Equal(Forwarded, trusted.LoggedAddress);
        Assert.Equal(HttpStatusCode.Unauthorized, untrusted.Status);
        Assert.Equal("192.0.2.1", untrusted.LoggedAddress);
    }

    [Fact]
    public void Defaults_TrustLoopbackOnly()
    {
        var options = new ForwardedHeadersTrustOptions();

        Assert.False(options.TrustAnySender);
        Assert.Empty(options.KnownProxies);
        Assert.Empty(options.KnownNetworks);
        Assert.Equal("ForwardedHeaders", ForwardedHeadersConstants.ConfigurationSection);
    }
}
