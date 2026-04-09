using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace RVM.HealthGuard.API.Auth;

public class ApiKeyAuthHandler(
    IOptionsMonitor<ApiKeyAuthOptions> options,
    ILoggerFactory loggerFactory,
    UrlEncoder encoder) : AuthenticationHandler<ApiKeyAuthOptions>(options, loggerFactory, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ApiKeyAuthOptions.HeaderName, out var rawKey) || string.IsNullOrWhiteSpace(rawKey))
            return Task.FromResult(AuthenticateResult.NoResult());

        var key = rawKey.ToString().Trim();

        var entry = Options.Keys.FirstOrDefault(k =>
            !string.IsNullOrEmpty(k.Key) &&
            string.Equals(k.Key, key, StringComparison.Ordinal));

        if (entry is null)
        {
            Logger.LogWarning("Invalid API key presented for path {Path}", Request.Path);
            return Task.FromResult(AuthenticateResult.Fail("Invalid API Key"));
        }

        Context.Items["AppId"] = entry.AppId;

        var claims = new[]
        {
            new Claim("appid", entry.AppId),
            new Claim("appname", entry.Name)
        };

        var identity = new ClaimsIdentity(claims, ApiKeyAuthOptions.Scheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, ApiKeyAuthOptions.Scheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.Headers["WWW-Authenticate"] = $"ApiKey realm=\"RVM.HealthGuard\", header=\"{ApiKeyAuthOptions.HeaderName}\"";
        return Task.CompletedTask;
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    }
}
