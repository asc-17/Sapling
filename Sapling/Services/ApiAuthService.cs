using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components.Authorization;

namespace Sapling.Services;

public sealed record AuthResult(bool Succeeded, string? Error);

/// <summary>
/// Talks to the Identity API endpoints with bearer tokens and keeps them in SecureStorage,
/// which is the transport the browser head gets for free from the auth cookie.
/// </summary>
public sealed class ApiAuthService(IHttpClientFactory factory, TokenStore tokens) : AuthenticationStateProvider
{
    private ClaimsPrincipal _user = new(new ClaimsIdentity());

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (_user.Identity?.IsAuthenticated == true)
        {
            return new AuthenticationState(_user);
        }

        var token = await tokens.GetAccessTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            return new AuthenticationState(_user);
        }

        var email = await tokens.GetEmailAsync();
        _user = Build(email ?? "student", await tokens.GetFullNameAsync());
        return new AuthenticationState(_user);
    }

    public async Task<AuthResult> SignInAsync(string email, string password)
    {
        var client = factory.CreateClient(SaplingApi.Anonymous);
        var response = await client.PostAsJsonAsync("api/identity/login?useCookies=false", new { email, password });

        if (!response.IsSuccessStatusCode)
        {
            return new AuthResult(false, "That email and password combination did not match.");
        }

        var payload = await response.Content.ReadFromJsonAsync<LoginResponse>();
        if (payload is null || string.IsNullOrEmpty(payload.AccessToken))
        {
            return new AuthResult(false, "The server did not return a session. Try again.");
        }

        await tokens.SaveAsync(payload.AccessToken, payload.RefreshToken, email);
        _user = Build(email, await FetchFullNameAsync());
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_user)));
        return new AuthResult(true, null);
    }

    private async Task<string?> FetchFullNameAsync()
    {
        try
        {
            var client = factory.CreateClient(SaplingApi.Authenticated);
            var profile = await client.GetFromJsonAsync<Shared.Contracts.StudentProfileDto>("api/profile");
            if (!string.IsNullOrWhiteSpace(profile?.FullName))
            {
                await tokens.SaveFullNameAsync(profile.FullName);
                return profile.FullName;
            }
        }
        catch (Exception)
        {
            // The shell falls back to the email when the name is unavailable.
        }

        return null;
    }

    public async Task<AuthResult> RegisterAsync(string email, string password)
    {
        var client = factory.CreateClient(SaplingApi.Anonymous);
        var response = await client.PostAsJsonAsync("api/identity/register", new { email, password });

        if (response.IsSuccessStatusCode)
        {
            return await SignInAsync(email, password);
        }

        var problem = await response.Content.ReadAsStringAsync();
        var message = problem.Contains("DuplicateUserName", StringComparison.OrdinalIgnoreCase)
            ? "An account with that email already exists."
            : "Could not create the account. Passwords need 8 characters with an uppercase letter and a digit.";

        return new AuthResult(false, message);
    }

    public async Task SignOutAsync()
    {
        await tokens.ClearAsync();
        _user = new ClaimsPrincipal(new ClaimsIdentity());
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_user)));
    }

    private static ClaimsPrincipal Build(string email, string? fullName)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, email),
            new(ClaimTypes.Email, email),
        };

        if (!string.IsNullOrWhiteSpace(fullName))
        {
            claims.Add(new Claim("full_name", fullName));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "sapling-bearer"));
    }

    private sealed record LoginResponse(
        [property: JsonPropertyName("accessToken")] string AccessToken,
        [property: JsonPropertyName("refreshToken")] string? RefreshToken);
}

public sealed class TokenStore
{
    private const string AccessKey = "sapling.access";
    private const string RefreshKey = "sapling.refresh";
    private const string EmailKey = "sapling.email";
    private const string NameKey = "sapling.name";

    public async Task<string?> GetAccessTokenAsync() => await SafeGetAsync(AccessKey);

    public async Task<string?> GetEmailAsync() => await SafeGetAsync(EmailKey);

    public async Task<string?> GetFullNameAsync() => await SafeGetAsync(NameKey);

    public Task SaveFullNameAsync(string fullName) => SecureStorage.Default.SetAsync(NameKey, fullName);

    public async Task SaveAsync(string accessToken, string? refreshToken, string email)
    {
        await SecureStorage.Default.SetAsync(AccessKey, accessToken);
        await SecureStorage.Default.SetAsync(EmailKey, email);
        if (!string.IsNullOrEmpty(refreshToken))
        {
            await SecureStorage.Default.SetAsync(RefreshKey, refreshToken);
        }
    }

    public Task ClearAsync()
    {
        SecureStorage.Default.Remove(AccessKey);
        SecureStorage.Default.Remove(RefreshKey);
        SecureStorage.Default.Remove(EmailKey);
        SecureStorage.Default.Remove(NameKey);
        return Task.CompletedTask;
    }

    private static async Task<string?> SafeGetAsync(string key)
    {
        try
        {
            return await SecureStorage.Default.GetAsync(key);
        }
        catch (Exception)
        {
            // A corrupted keystore entry should log the user out, not crash the app.
            return null;
        }
    }
}

public sealed class BearerTokenHandler(TokenStore tokens) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await tokens.GetAccessTokenAsync();
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
