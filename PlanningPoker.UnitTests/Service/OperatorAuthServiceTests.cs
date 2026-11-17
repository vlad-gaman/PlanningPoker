using System.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using PlanningPoker.Common.Constants;
using PlanningPoker.Service.Models;
using PlanningPoker.Service.Services;
using PlanningPoker.UnitTests.Fakes;

namespace PlanningPoker.UnitTests.Service;

public class OperatorAuthServiceTests
{
    private const string Username = "operator";
    private const string Password = "correct horse battery staple";
    private const string Address = "203.0.113.7";

    private readonly PasswordHashingService _hashing = new();
    private readonly FakeIssueLog _issues = new();
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero));

    private OperatorAuthService Create(bool configured = true, TimeSpan? failureDelay = null)
    {
        var options = Options.Create(new OperatorOptions
        {
            Username = configured ? Username : null,
            PasswordHash = configured ? _hashing.Hash(Password) : null,
            FailureDelay = failureDelay ?? TimeSpan.Zero
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

    [Fact]
    public async Task Authenticate_CorrectCredentials_ReturnsTheOperator()
    {
        var result = await Create().AuthenticateAsync(Username, Password, Address);

        Assert.True(result.IsSuccess);
        Assert.Equal(Username, result.Data!.Username);
    }

    [Fact]
    public async Task Authenticate_WrongPasswordAndUnknownUser_FailWithTheSameError()
    {
        var service = Create();

        var wrongPassword = await service.AuthenticateAsync(Username, "nope", Address);
        var unknownUser = await service.AuthenticateAsync("someone-else", Password, "198.51.100.1");

        Assert.Equal(ErrorCodes.AdminInvalidCredentials, wrongPassword.Errors[0].Code);
        Assert.Equal(wrongPassword.Errors[0].Code, unknownUser.Errors[0].Code);
        Assert.Equal(wrongPassword.Errors[0].Message, unknownUser.Errors[0].Message);
    }

    [Fact]
    public async Task Authenticate_NothingConfigured_RefusesEvenTheRightLookingCredentials()
    {
        var service = Create(configured: false);

        var result = await service.AuthenticateAsync(Username, Password, Address);

        Assert.False(service.IsConfigured);
        Assert.Equal(ErrorCodes.AdminInvalidCredentials, result.Errors[0].Code);
    }

    [Fact]
    public async Task Authenticate_FiveFailures_LockSignInEvenForTheRightPassword()
    {
        var service = Create();
        for (var attempt = 0; attempt < OperatorConstants.MaxFailedLogins; attempt++)
        {
            await service.AuthenticateAsync(Username, "nope", Address);
        }

        var result = await service.AuthenticateAsync(Username, Password, Address);

        Assert.Equal(ErrorCodes.AdminLoginLocked, result.Errors[0].Code);
    }

    [Fact]
    public async Task Authenticate_AfterTheLockoutEnds_SucceedsAgain()
    {
        var service = Create();
        for (var attempt = 0; attempt < OperatorConstants.MaxFailedLogins; attempt++)
        {
            await service.AuthenticateAsync(Username, "nope", Address);
        }

        _time.Advance(OperatorConstants.LockoutDuration + TimeSpan.FromSeconds(1));
        var result = await service.AuthenticateAsync(Username, Password, Address);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Authenticate_FailuresFromManyAddresses_LockTheAccountForEveryone()
    {
        var service = Create();
        for (var attempt = 0; attempt < OperatorConstants.MaxFailedLogins; attempt++)
        {
            await service.AuthenticateAsync(Username, "nope", $"198.51.100.{attempt}");
        }

        var result = await service.AuthenticateAsync(Username, Password, "192.0.2.1");

        Assert.Equal(ErrorCodes.AdminLoginLocked, result.Errors[0].Code);
    }

    [Fact]
    public async Task Authenticate_AFailure_WaitsTheConfiguredDelay()
    {
        var service = Create(failureDelay: TimeSpan.FromMilliseconds(150));

        var stopwatch = Stopwatch.StartNew();
        await service.AuthenticateAsync(Username, "nope", Address);
        stopwatch.Stop();

        Assert.True(stopwatch.ElapsedMilliseconds >= 120, $"Waited only {stopwatch.ElapsedMilliseconds} ms.");
    }

    [Fact]
    public async Task Authenticate_ASuccess_DoesNotWait()
    {
        var service = Create(failureDelay: TimeSpan.FromSeconds(5));

        var stopwatch = Stopwatch.StartNew();
        var result = await service.AuthenticateAsync(Username, Password, Address);
        stopwatch.Stop();

        Assert.True(result.IsSuccess);
        Assert.True(stopwatch.ElapsedMilliseconds < 2000);
    }

    [Fact]
    public async Task Authenticate_ASuccess_RecordsAnInfoEntryWithoutTheAddress()
    {
        await Create().AuthenticateAsync(Username, Password, Address);

        var entry = Assert.Single(_issues.Recorded);
        Assert.Equal(PlanningPoker.Common.Models.IssueLevel.Info, entry.Level);
        Assert.Equal(IssueLogConstants.AdminSource, entry.Source);
        Assert.DoesNotContain(Address, entry.Message);
        Assert.Empty(_issues.FailedLogins);
    }

    [Fact]
    public async Task Authenticate_AFailure_RecordsAFailedLoginWithTheAddress()
    {
        await Create().AuthenticateAsync(Username, "nope", Address);

        Assert.Equal([(Address, false)], _issues.FailedLogins);
        Assert.Empty(_issues.Recorded);
    }

    [Fact]
    public async Task Authenticate_WhenLocked_RecordsALockedFailedLogin()
    {
        var service = Create();
        for (var attempt = 0; attempt < OperatorConstants.MaxFailedLogins; attempt++)
        {
            await service.AuthenticateAsync(Username, "nope", Address);
        }

        await service.AuthenticateAsync(Username, Password, Address);

        Assert.Equal((Address, true), _issues.FailedLogins[^1]);
    }
}
