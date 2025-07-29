using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AbevPortfolioCsharp.Backend.Services.Email;

public class ResendEmailSender : IEmailSender
{
    private readonly HttpClient _http;
    private readonly string _apiKey;

    public ResendEmailSender(IHttpClientFactory factory, IConfiguration cfg)
    {
        _http = factory.CreateClient();
        _apiKey = cfg["RESEND_API_KEY"] ?? throw new InvalidOperationException("RESEND_API_KEY missing");
    }

    public async Task<bool> SendAsync(string to, string from, string replyTo, string subject, string html, CancellationToken ct = default)
    {
        var payload = new
        {
            from,
            to = new[] { to },
            subject,
            html,
            reply_to = new[] { replyTo }
        };

        var req = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var resp = await _http.SendAsync(req, ct);

        // (Optional) Log failures for debugging
        if (!resp.IsSuccessStatusCode)
        {
            var text = await resp.Content.ReadAsStringAsync(ct);
            // TODO: log text somewhere (Console, App Insights)
        }

        return resp.IsSuccessStatusCode;
    }
}
