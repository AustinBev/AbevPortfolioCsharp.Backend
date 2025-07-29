using AbevPortfolioCsharp.Backend.Models;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;

namespace AbevPortfolioCsharp.Backend.Services;

public interface ITurnstileVerifier
{
    Task<bool> VerifyAsync(string? token, string? remoteIp, CancellationToken ct = default);
}

public class TurnstileVerifier : ITurnstileVerifier
{
    private readonly HttpClient _http;
    private readonly string _secret;

    public TurnstileVerifier(IHttpClientFactory f, IConfiguration cfg)
    {
        _http = f.CreateClient();
        _secret = cfg["TURNSTILE_SECRET"] ?? throw new InvalidOperationException("TURNSTILE_SECRET missing");
    }

    public async Task<bool> VerifyAsync(string? token, string? remoteIp, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token)) return false;
        var form = new FormUrlEncodedContent(new Dictionary<string, string?>
        {
            ["secret"] = _secret,
            ["response"] = token,
            ["remoteip"] = remoteIp
        }!);
        var resp = await _http.PostAsync("https://challenges.cloudflare.com/turnstile/v0/siteverify", form, ct);
        var body = await resp.Content.ReadFromJsonAsync<TurnstileVerifyResponse>(cancellationToken: ct);
        return body?.success == true;
    }
}
