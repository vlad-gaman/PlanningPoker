using PlanningPoker.Service.Services;

namespace PlanningPoker.UnitTests.Service;

public class PasswordHashingServiceTests
{
    private readonly PasswordHashingService _service = new();

    [Fact]
    public void Verify_TheSamePassword_Matches()
    {
        var hash = _service.Hash("a password");

        Assert.True(_service.Verify(hash, "a password"));
    }

    [Fact]
    public void Verify_ADifferentPassword_DoesNotMatch()
    {
        var hash = _service.Hash("a password");

        Assert.False(_service.Verify(hash, "another password"));
    }

    [Fact]
    public void Hash_SamePasswordTwice_GivesDifferentHashes()
    {
        Assert.NotEqual(_service.Hash("a password"), _service.Hash("a password"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not a hash")]
    [InlineData("AQAAAAEAACcQAAAAEA==")]
    public void Verify_AMalformedHash_NeverMatches(string hash)
    {
        Assert.False(_service.Verify(hash, "a password"));
    }
}

public class PasswordHashCommandTests
{
    [Fact]
    public void Run_APasswordOnInput_PrintsAHashThatVerifies()
    {
        var hashing = new PasswordHashingService();
        var output = new StringWriter();

        var exitCode = PasswordHashCommand.Run(hashing, new StringReader("hunter2\n"), output, new StringWriter());

        Assert.Equal(0, exitCode);
        Assert.True(hashing.Verify(output.ToString().Trim(), "hunter2"));
    }

    [Fact]
    public void Run_NoPassword_ExplainsAndFails()
    {
        var output = new StringWriter();
        var error = new StringWriter();

        var exitCode = PasswordHashCommand.Run(new PasswordHashingService(), new StringReader(string.Empty), output, error);

        Assert.Equal(1, exitCode);
        Assert.Equal(string.Empty, output.ToString());
        Assert.Contains("standard input", error.ToString());
    }
}
