using System.Buffers.Text;
using PlanningPoker.Common.Constants;
using PlanningPoker.Service.Services;

namespace PlanningPoker.UnitTests.Service;

public class SessionTokenGeneratorTests
{
    private readonly SessionTokenGenerator _generator = new();

    [Fact]
    public void Generate_ReturnsUnpaddedBase64UrlOfTheConfiguredLength()
    {
        var token = _generator.Generate();

        Assert.Equal(SessionTokenConstants.EncodedLength, token.Length);
        Assert.Matches("^[A-Za-z0-9_-]+$", token);
        Assert.Equal(SessionTokenConstants.ByteLength, Base64Url.DecodeFromChars(token).Length);
    }

    [Fact]
    public void Generate_ReturnsADifferentTokenEachTime()
    {
        var tokens = Enumerable.Range(0, 100).Select(_ => _generator.Generate()).ToHashSet();

        Assert.Equal(100, tokens.Count);
    }
}
