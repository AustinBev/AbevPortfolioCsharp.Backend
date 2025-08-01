using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using AbevPortfolioCsharp.Backend.Models;
using AbevPortfolioCsharp.Backend.Services.Email;

namespace AbevPortfolioCsharp.Backend.Services.Email
{
    public class ResendEmailSender : IEmailSender
    {
        private readonly HttpClient _http;
        private readonly string _apiKey;
        private readonly IConfiguration _cfg;

        public ResendEmailSender(IHttpClientFactory factory, IConfiguration cfg)
        {
            _http = factory.CreateClient();
            _cfg = cfg;
            _apiKey = cfg["RESEND_API_KEY"] ?? throw new InvalidOperationException("RESEND_API_KEY missing");
        }

        // Implements the interface method using the MinimalContactRequest DTO
        public async Task<bool> SendAsync(MinimalContactRequest req, CancellationToken ct = default)
        {
            var to = _cfg["DEST_EMAIL"] ?? throw new InvalidOperationException("DEST_EMAIL missing");
            var from = _cfg["MAIL_FROM"] ?? throw new InvalidOperationException("MAIL_FROM missing");
            var replyTo = req.Email;
            var subject = $"[Portfolio] {req.Name} (via contact form)";
            var html = EmailTemplates.BuildContactHtml(
                              req.Name,
                              req.Email,
                              req.VerificationUrl,
                              req.Message);

            return await SendAsync(to, from, replyTo, subject, html, ct);
        }

        // Existing overload used internally
        public async Task<bool> SendAsync(
            string to,
            string from,
            string replyTo,
            string subject,
            string html,
            CancellationToken ct = default)
        {
            var payload = new
            {
                from,
                to = new[] { to },
                subject,
                html,
                reply_to = new[] { replyTo }
            };

            var message = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails")
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await _http.SendAsync(message, ct);
            if (!response.IsSuccessStatusCode)
            {
                var text = await response.Content.ReadAsStringAsync(ct);
                // TODO: log failure details to App Insights or logger
            }

            return response.IsSuccessStatusCode;
        }
    }
}
