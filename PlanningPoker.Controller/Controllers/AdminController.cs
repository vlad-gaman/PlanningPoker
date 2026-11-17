using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PlanningPoker.Common.Constants;
using PlanningPoker.Common.Models;
using PlanningPoker.Controller.Filters;
using PlanningPoker.Controller.Mappers;
using PlanningPoker.Controller.Models;
using PlanningPoker.Controller.Validations;
using PlanningPoker.Service.Models;
using PlanningPoker.Service.Services;

namespace PlanningPoker.Controller.Controllers;

/// <summary>Operator sign-in, session and sign-out. Everything else under /api/admin requires the Operator session.</summary>
[ApiController]
[Route("api/admin")]
[Produces("application/json")]
[Authorize(Policy = OperatorConstants.PolicyName)]
public class AdminController(
    IOperatorAuthService auth,
    AdminLoginRequestValidation validation,
    IAntiforgery antiforgery,
    IAdminSessionResponseMapper sessionMapper,
    IApiResponseMapper apiResponseMapper) : ApiControllerBase(apiResponseMapper)
{
    /// <summary>The password step. With an authenticator configured a correct password answers 401 <c>admin_totp_required</c> with a challenge for <c>login/totp</c>. Failures say only that the username or password is wrong, or that sign-in is locked.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<ApiResponse<AdminSessionResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login([FromBody] AdminLoginRequest request)
    {
        var validated = validation.Validate(request);
        if (!validated.IsSuccess)
        {
            return ToResult(ServiceResponse<OperatorIdentity>.Failure(validated), _ => sessionMapper.MapSignedOut());
        }

        return await SignIn(await auth.AuthenticateAsync(request.Username, request.Password, HttpContext.Connection.RemoteIpAddress?.ToString()));
    }

    /// <summary>The code step of a sign-in with an authenticator: the challenge from the password step and a current code.</summary>
    [HttpPost("login/totp")]
    [AllowAnonymous]
    [ProducesResponseType<ApiResponse<AdminSessionResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> LoginTotp([FromBody] AdminTotpRequest request)
    {
        var validated = validation.Validate(request);
        if (!validated.IsSuccess)
        {
            return ToResult(ServiceResponse<OperatorIdentity>.Failure(validated), _ => sessionMapper.MapSignedOut());
        }

        return await SignIn(await auth.CompleteTotpAsync(request.Challenge, request.Code, HttpContext.Connection.RemoteIpAddress?.ToString()));
    }

    /// <summary>The current Operator session. When signed in, includes a fresh anti-forgery token.</summary>
    [HttpGet("session")]
    [AllowAnonymous]
    [ProducesResponseType<ApiResponse<AdminSessionResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Session()
    {
        var authenticated = await HttpContext.AuthenticateAsync(OperatorConstants.AuthenticationScheme);
        if (!authenticated.Succeeded)
        {
            return ToResult(ServiceResponse<AdminSessionResponse>.Success(sessionMapper.MapSignedOut()), session => session);
        }

        HttpContext.User = authenticated.Principal!;
        var username = authenticated.Principal!.Identity!.Name!;
        return ToResult(ServiceResponse<AdminSessionResponse>.Success(sessionMapper.MapSignedIn(username, IssueCsrfToken())), session => session);
    }

    /// <summary>Ends the Operator session. Needs the anti-forgery token.</summary>
    [HttpPost("logout")]
    [ValidateAdminAntiforgery]
    [ProducesResponseType<ApiResponse<AdminSessionResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(OperatorConstants.AuthenticationScheme);
        Response.Cookies.Delete(OperatorConstants.XsrfCookieName);
        return ToResult(ServiceResponse<AdminSessionResponse>.Success(sessionMapper.MapSignedOut()), session => session);
    }

    private async Task<IActionResult> SignIn(ServiceResponse<OperatorIdentity> result)
    {
        if (!result.IsSuccess)
        {
            return ToResult(result, identity => sessionMapper.MapSignedIn(identity.Username, string.Empty));
        }

        var principal = CreatePrincipal(result.Data!);
        await HttpContext.SignInAsync(OperatorConstants.AuthenticationScheme, principal);
        HttpContext.User = principal;
        return ToResult(result, identity => sessionMapper.MapSignedIn(identity.Username, IssueCsrfToken()));
    }

    private static ClaimsPrincipal CreatePrincipal(OperatorIdentity identity)
    {
        var claims = new[] { new Claim(ClaimTypes.Name, identity.Username) };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, OperatorConstants.AuthenticationScheme));
    }

    /// <summary>
    /// Mints the request token for the current identity, and also sets it in the script-readable XSRF-TOKEN cookie
    /// that Angular's built-in XSRF support copies into the X-XSRF-TOKEN header.
    /// </summary>
    private string IssueCsrfToken()
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        Response.Cookies.Append(OperatorConstants.XsrfCookieName, tokens.RequestToken!, new CookieOptions
        {
            HttpOnly = false,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Path = "/"
        });
        return tokens.RequestToken!;
    }
}
