using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using OtpNet;
using PlanningPoker.Common.Constants;
using PlanningPoker.Service.Models;
using PlanningPoker.Service.Services;
using PlanningPoker.UnitTests.Fakes;

namespace PlanningPoker.UnitTests.Service;

/// <summary>Helpers for computing the codes an authenticator app would show.</summary>
public static class TestTotp
{
    public const string Secret = "JBSWY3DPEHPK3PXP";

    public static string CodeAt(DateTimeOffset time, string secret = Secret)
    {
        return new Totp(Base32Encoding.ToBytes(secret)).ComputeTotp(time.UtcDateTime);
    }
}

public class TotpServiceTests
{
    private static readonly DateTimeOffset Start = new(2026, 3, 1, 12, 0, 10, TimeSpan.Zero);
    private readonly FakeTimeProvider _time = new(Start);

    private TotpService Create(string? secret = TestTotp.Secret)
    {
        return new TotpService(Options.Create(new OperatorOptions { TotpSecret = secret }), _time);
    }

    [Fact]
    public void Verify_TheCurrentCode_Succeeds()
    {
        Assert.True(Create().Verify(TestTotp.CodeAt(Start)));
    }

    [Fact]
    public void Verify_ACodeWithSpaces_Succeeds()
    {
        var code = TestTotp.CodeAt(Start);

        Assert.True(Create().Verify($"{code[..3]} {code[3..]}"));
    }

    [Theory]
    [InlineData(-30)]
    [InlineData(30)]
    public void Verify_OneStepEitherSide_Succeeds(int seconds)
    {
        Assert.True(Create().Verify(TestTotp.CodeAt(Start.AddSeconds(seconds))));
    }

    [Theory]
    [InlineData(-90)]
    [InlineData(90)]
    public void Verify_TwoOrMoreStepsAway_Fails(int seconds)
    {
        Assert.False(Create().Verify(TestTotp.CodeAt(Start.AddSeconds(seconds))));
    }

    [Fact]
    public void Verify_TheSameCodeTwice_FailsTheSecondTime()
    {
        var service = Create();
        var code = TestTotp.CodeAt(Start);

        Assert.True(service.Verify(code));
        Assert.False(service.Verify(code));
    }

    [Fact]
    public void Verify_AnEarlierStepAfterALaterOne_Fails()
    {
        var service = Create();

        Assert.True(service.Verify(TestTotp.CodeAt(Start.AddSeconds(30))));
        Assert.False(service.Verify(TestTotp.CodeAt(Start)));
    }

    [Fact]
    public void Verify_ALaterStep_SucceedsAfterAnEarlierOne()
    {
        var service = Create();

        Assert.True(service.Verify(TestTotp.CodeAt(Start)));
        _time.Advance(TimeSpan.FromSeconds(60));
        Assert.True(service.Verify(TestTotp.CodeAt(Start.AddSeconds(60))));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("1234567")]
    [InlineData("abcdef")]
    [InlineData("12 34 5a")]
    public void Verify_MalformedCodes_Fail(string? code)
    {
        Assert.False(Create().Verify(code));
    }

    [Fact]
    public void Verify_AWrongCode_FailsAndDoesNotBurnTheRightOne()
    {
        var service = Create();

        Assert.False(service.Verify("000000" == TestTotp.CodeAt(Start) ? "000001" : "000000"));
        Assert.True(service.Verify(TestTotp.CodeAt(Start)));
    }

    [Fact]
    public void IsEnabled_FollowsTheSecret()
    {
        Assert.True(Create().IsEnabled);
        Assert.False(Create(null).IsEnabled);
        Assert.False(Create("  ").IsEnabled);
    }

    [Fact]
    public void Verify_NoSecret_AlwaysFails()
    {
        Assert.False(Create(null).Verify(TestTotp.CodeAt(Start)));
    }

    [Fact]
    public void Verify_ASecretThatIsNotBase32_FailsClosed()
    {
        var service = Create("not base32 !!!");

        Assert.True(service.IsEnabled);
        Assert.False(service.Verify(TestTotp.CodeAt(Start)));
    }
}

public class TotpChallengeServiceTests
{
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 3, 1, 12, 0, 0, TimeSpan.Zero));

    private TotpChallengeService Create(string hash = "hash-one", string secret = TestTotp.Secret)
    {
        return new TotpChallengeService(Options.Create(new OperatorOptions { PasswordHash = hash, TotpSecret = secret }), _time);
    }

    [Fact]
    public void Read_AFreshChallenge_GivesTheUsername()
    {
        var service = Create();

        Assert.Equal("operator", service.Read(service.Issue("operator")));
    }

    [Fact]
    public void Read_AUsernameWithSeparators_RoundTrips()
    {
        var service = Create();

        Assert.Equal("a|b.c", service.Read(service.Issue("a|b.c")));
    }

    [Fact]
    public void Read_AfterTheLifetime_IsNull()
    {
        var service = Create();
        var challenge = service.Issue("operator");

        _time.Advance(OperatorConstants.ChallengeLifetime + TimeSpan.FromSeconds(1));

        Assert.Null(service.Read(challenge));
    }

    [Fact]
    public void Read_JustBeforeTheLifetimeEnds_StillWorks()
    {
        var service = Create();
        var challenge = service.Issue("operator");

        _time.Advance(OperatorConstants.ChallengeLifetime - TimeSpan.FromSeconds(1));

        Assert.Equal("operator", service.Read(challenge));
    }

    [Fact]
    public void Read_ATamperedChallenge_IsNull()
    {
        var service = Create();
        var challenge = service.Issue("operator");
        var tampered = (challenge[0] == 'A' ? "B" : "A") + challenge[1..];

        Assert.Null(service.Read(tampered));
    }

    [Fact]
    public void Read_AChallengeFromOtherSecrets_IsNull()
    {
        var challenge = Create().Issue("operator");

        Assert.Null(Create(hash: "another-hash").Read(challenge));
        Assert.Null(Create(secret: "GEZDGNBVGY3TQOJQ").Read(challenge));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("no-dot")]
    [InlineData("a.b.c")]
    [InlineData("....")]
    public void Read_Garbage_IsNull(string? challenge)
    {
        Assert.Null(Create().Read(challenge));
    }
}

public class OperatorAuthTotpTests
{
    private const string Username = "operator";
    private const string Password = "correct horse battery staple";
    private const string Address = "203.0.113.7";

    private readonly PasswordHashingService _hashing = new();
    private readonly FakeIssueLog _issues = new();
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 3, 1, 12, 0, 10, TimeSpan.Zero));

    private OperatorAuthService Create(string? secret = TestTotp.Secret)
    {
        var options = Options.Create(new OperatorOptions
        {
            Username = Username,
            PasswordHash = _hashing.Hash(Password),
            TotpSecret = secret,
            FailureDelay = TimeSpan.Zero
        });
        return new OperatorAuthService(
            options,
            _hashing,
            new LoginLockoutService(options, _time),
            NullLogger<OperatorAuthService>.Instance,
            _issues,
            new TotpService(options, _time),
            new TotpChallengeService(options, _time));
    }

    private static string ChallengeOf(PlanningPoker.Common.Models.ServiceResponse<OperatorIdentity> response)
    {
        return response.Errors[0].Metadata["challenge"];
    }

    [Fact]
    public async Task Authenticate_NoSecret_SignsInOnThePasswordAlone()
    {
        var result = await Create(secret: null).AuthenticateAsync(Username, Password, Address);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Authenticate_WithASecret_AskForTheCodeInsteadOfSigningIn()
    {
        var result = await Create().AuthenticateAsync(Username, Password, Address);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.AdminTotpRequired, result.Errors[0].Code);
        Assert.False(string.IsNullOrEmpty(ChallengeOf(result)));
        Assert.Empty(_issues.Recorded);
        Assert.Empty(_issues.FailedLogins);
    }

    [Fact]
    public async Task Authenticate_WithASecret_AWrongPasswordIsStillAnOrdinaryFailure()
    {
        var result = await Create().AuthenticateAsync(Username, "nope", Address);

        Assert.Equal(ErrorCodes.AdminInvalidCredentials, result.Errors[0].Code);
        Assert.Equal([(Address, false)], _issues.FailedLogins);
    }

    [Fact]
    public async Task CompleteTotp_ACorrectCode_SignsInAndRecordsIt()
    {
        var service = Create();
        var challenge = ChallengeOf(await service.AuthenticateAsync(Username, Password, Address));

        var result = await service.CompleteTotpAsync(challenge, TestTotp.CodeAt(_time.GetUtcNow()), Address);

        Assert.True(result.IsSuccess);
        Assert.Equal(Username, result.Data!.Username);
        Assert.Equal(IssueLogConstants.AdminSource, Assert.Single(_issues.Recorded).Source);
    }

    [Fact]
    public async Task CompleteTotp_AWrongCode_FailsAndIsRecordedAsAFailedLogin()
    {
        var service = Create();
        var challenge = ChallengeOf(await service.AuthenticateAsync(Username, Password, Address));
        var wrong = TestTotp.CodeAt(_time.GetUtcNow().AddMinutes(10));

        var result = await service.CompleteTotpAsync(challenge, wrong, Address);

        Assert.Equal(ErrorCodes.AdminTotpInvalid, result.Errors[0].Code);
        Assert.Equal([(Address, false)], _issues.FailedLogins);
    }

    [Fact]
    public async Task CompleteTotp_AReusedCode_Fails()
    {
        var service = Create();
        var challenge = ChallengeOf(await service.AuthenticateAsync(Username, Password, Address));
        var code = TestTotp.CodeAt(_time.GetUtcNow());
        await service.CompleteTotpAsync(challenge, code, Address);

        var again = await service.CompleteTotpAsync(challenge, code, Address);

        Assert.Equal(ErrorCodes.AdminTotpInvalid, again.Errors[0].Code);
    }

    [Fact]
    public async Task CompleteTotp_AnExpiredChallenge_AsksToStartAgain_WithoutCountingAFailure()
    {
        var service = Create();
        var challenge = ChallengeOf(await service.AuthenticateAsync(Username, Password, Address));
        _time.Advance(OperatorConstants.ChallengeLifetime + TimeSpan.FromSeconds(1));

        var result = await service.CompleteTotpAsync(challenge, TestTotp.CodeAt(_time.GetUtcNow()), Address);

        Assert.Equal(ErrorCodes.AdminLoginExpired, result.Errors[0].Code);
        Assert.Empty(_issues.FailedLogins);
    }

    [Fact]
    public async Task CompleteTotp_ACodeWithoutAChallenge_IsRefused()
    {
        var result = await Create().CompleteTotpAsync(null, TestTotp.CodeAt(_time.GetUtcNow()), Address);

        Assert.Equal(ErrorCodes.AdminLoginExpired, result.Errors[0].Code);
    }

    [Fact]
    public async Task CompleteTotp_NoSecretConfigured_IsRefusedEvenWithAChallengeFromBefore()
    {
        var challenge = ChallengeOf(await Create().AuthenticateAsync(Username, Password, Address));

        var result = await Create(secret: null).CompleteTotpAsync(challenge, "123456", Address);

        Assert.Equal(ErrorCodes.AdminLoginExpired, result.Errors[0].Code);
    }

    [Fact]
    public async Task CompleteTotp_FiveWrongCodes_LockSignIn()
    {
        var service = Create();
        var challenge = ChallengeOf(await service.AuthenticateAsync(Username, Password, Address));
        for (var attempt = 0; attempt < OperatorConstants.MaxFailedLogins; attempt++)
        {
            await service.CompleteTotpAsync(challenge, "000000", Address);
        }

        var locked = await service.CompleteTotpAsync(challenge, TestTotp.CodeAt(_time.GetUtcNow()), Address);
        var password = await service.AuthenticateAsync(Username, Password, Address);

        Assert.Equal(ErrorCodes.AdminLoginLocked, locked.Errors[0].Code);
        Assert.Equal(ErrorCodes.AdminLoginLocked, password.Errors[0].Code);
    }

    [Fact]
    public async Task Authenticate_TheCorrectPasswordRepeatedlyWithoutCodes_DoesNotResetTheFailureCount()
    {
        var service = Create();
        for (var attempt = 0; attempt < OperatorConstants.MaxFailedLogins - 1; attempt++)
        {
            await service.AuthenticateAsync(Username, "nope", Address);
        }

        await service.AuthenticateAsync(Username, Password, Address);
        await service.AuthenticateAsync(Username, "nope", Address);

        var locked = await service.AuthenticateAsync(Username, Password, Address);
        Assert.Equal(ErrorCodes.AdminLoginLocked, locked.Errors[0].Code);
    }
}

public class TotpSecretCommandTests
{
    [Fact]
    public void Run_PrintsASecretThatTheTotpServiceAccepts()
    {
        var output = new StringWriter();

        var exitCode = TotpSecretCommand.Run("operator", output);

        Assert.Equal(0, exitCode);
        var secret = output.ToString().Split('\n')[0].Split('=')[1].Trim();
        var now = new DateTimeOffset(2026, 3, 1, 12, 0, 10, TimeSpan.Zero);
        var service = new TotpService(Options.Create(new OperatorOptions { TotpSecret = secret }), new FakeTimeProvider(now));
        Assert.True(service.Verify(TestTotp.CodeAt(now, secret)));
        Assert.Contains($"otpauth://totp/PlanningPoker:operator?secret={secret}", output.ToString());
    }

    [Fact]
    public void Run_TwiceGivesDifferentSecrets()
    {
        var first = new StringWriter();
        var second = new StringWriter();

        TotpSecretCommand.Run("operator", first);
        TotpSecretCommand.Run("operator", second);

        Assert.NotEqual(first.ToString(), second.ToString());
    }
}
