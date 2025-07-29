using System.Net;
using System.Net.Mime;
using System.Text.Json;
using AbevPortfolioCsharp.Backend.Models;
using AbevPortfolioCsharp.Backend.Services;
using AbevPortfolioCsharp.Backend.Services.Email;
using AbevPortfolioCsharp.Backend.Services.RateLimiting;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;

namespace AbevPortfolioCsharp.Backend.Endpoints;

public class ContactEndpoint
{
    private readonly ITurnstileVerifier _turnstile;
    private readonly IRateLimiter _limiter;
    private readonly IEmailSender _mailer;
    private readonly IConfiguration _cfg;

    public ContactEndpoint(ITurnstileVerifier turnstile, IRateLimiter limiter, IEmailSender mailer, IConfiguration cfg)
    {
        _turnstile = turnstile; _limiter = limiter; _mailer = mailer; _cfg = cfg;
    }

    [Function("contact")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "contact")] HttpRequestData req)
    {
        // Always respond 200 to avoid giving attackers feedback
        var ok = await HandleAsync(req);
        var res = req.CreateResponse(HttpStatusCode.OK);
        res.Headers.Add("Content-Type", MediaTypeNames.Application.Json);
        await res.WriteStringAsync("{\"ok\":true}");
        return res;
    }

    private async Task<bool> HandleAsync(HttpRequestData req)
    {
        MinimalContactRequest? body;
        try
        {
            body = await JsonSerializer.DeserializeAsync<MinimalContactRequest>(req.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch { return false; }
        if (body is null) return false;

        // Quick bot drops
        if (!string.IsNullOrWhiteSpace(body.Hp)) return false;
        if (body.SecondsToSubmit < 3) return false;

        var ip = req.Headers.TryGetValues("X-Forwarded-For", out var v) ? v.FirstOrDefault()?.Split(',')[0].Trim() : "0.0.0.0";
        if (string.IsNullOrWhiteSpace(ip)) ip = "0.0.0.0";

        // Rate limit
        if (!await _limiter.IsAllowedAsync(ip)) return false;

        // Optional Turnstile
        var requireTs = string.Equals(_cfg["REQUIRE_TURNSTILE"], "true", StringComparison.OrdinalIgnoreCase);
        if (requireTs)
        {
            var pass = await _turnstile.VerifyAsync(body.TurnstileToken, ip);
            if (!pass) return false;
        }

        // Sanity-check verification URL
        if (!IsValidVerificationUrl(body.VerificationUrl, out var host)) return false;

        // Compose email
        var to = _cfg["DEST_EMAIL"] ?? throw new InvalidOperationException("DEST_EMAIL missing");
        var from = _cfg["MAIL_FROM"] ?? throw new InvalidOperationException("MAIL_FROM missing");
        var subject = $"[Portfolio] {body.Name} — from {host}";
        var html = EmailTemplates.BuildContactHtml(body.Name, body.Email, body.VerificationUrl, body.Message);

        // Send
        var sent = await _mailer.SendAsync(to, from, body.Email, subject, html);
        return sent;
    }

    private static bool IsValidVerificationUrl(string? url, out string host)
    {
        host = "";
        if (string.IsNullOrWhiteSpace(url)) return false;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
        if (!uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase)) return false;

        host = uri.DnsSafeHost.ToLowerInvariant();
        string[] deny = { "bit.ly", "t.co", "tinyurl.com", "pastebin.com" };

        var h = host; // local copy so the lambda doesn't capture an out param
        if (deny.Any(d => h == d || h.EndsWith("." + d, StringComparison.Ordinal)))
            return false;

        if (!h.Contains('.')) return false;
        return true;
 
    }
}
