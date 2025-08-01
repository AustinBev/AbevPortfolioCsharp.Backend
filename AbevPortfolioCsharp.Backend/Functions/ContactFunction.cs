using System.Net;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using AbevPortfolioCsharp.Backend.Models;
using AbevPortfolioCsharp.Backend.Services.RateLimiting;
using AbevPortfolioCsharp.Backend.Services.Email;
using AbevPortfolioCsharp.Backend.Services;

namespace AbevPortfolioCsharp.Backend.Functions
{
    public class ContactFunction
    {
        private readonly ILogger<ContactFunction> _logger;
        private readonly IConfiguration _configuration;
        private readonly IRateLimiter _limiter;
        private readonly ITurnstileVerifier _turnstile;
        private readonly IEmailSender _emailSender;

        public ContactFunction(
            ILogger<ContactFunction> logger,
            IConfiguration configuration,
            IRateLimiter limiter,
            ITurnstileVerifier turnstile,
            IEmailSender emailSender)
        {
            _logger = logger;
            _configuration = configuration;
            _limiter = limiter;
            _turnstile = turnstile;
            _emailSender = emailSender;
        }

        [Function("contact")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "contact")]
            HttpRequestData req,
            FunctionContext context)
        {
            var ct = context.CancellationToken;
            _logger.LogInformation("Processing contact form submission.");

            // 1) Deserialize payload
            MinimalContactRequest dto;
            try
            {
                dto = await req.ReadFromJsonAsync<MinimalContactRequest>(cancellationToken: ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse request body.");
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync("Invalid JSON payload.", ct);
                return bad;
            }

            if (dto == null)
            {
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync("Request body missing.", ct);
                return bad;
            }

            // 2) Honeypot check
            if (!string.IsNullOrEmpty(dto.Hp))
            {
                _logger.LogWarning("Honeypot triggered for IP: {Ip}", GetClientIp(req));
                return req.CreateResponse(HttpStatusCode.OK);
            }

            // 3) Rate limiting
            var clientIp = GetClientIp(req);
            if (!await _limiter.AllowAsync(clientIp, ct))
            {
                _logger.LogWarning("Rate limit exceeded for IP: {Ip}", clientIp);
                return req.CreateResponse(HttpStatusCode.TooManyRequests);
            }

            // 4) CAPTCHA verification
            var verified = await _turnstile.VerifyAsync(dto.TurnstileToken, clientIp, ct);
            if (!verified)
            {
                var resp = req.CreateResponse(HttpStatusCode.BadRequest);
                await resp.WriteStringAsync("Captcha verification failed.", ct);
                return resp;
            }

            // 5) Send the email
            var sent = await _emailSender.SendAsync(dto, ct);
            if (!sent)
            {
                _logger.LogError("Email sending failed for: {Email}", dto.Email);
                var err = req.CreateResponse(HttpStatusCode.InternalServerError);
                await err.WriteStringAsync("Failed to send email.", ct);
                return err;
            }

            // 6) Success response
            var ok = req.CreateResponse(HttpStatusCode.OK);
            await ok.WriteStringAsync("{\"ok\":true}", ct);
            return ok;
        }

        private static string GetClientIp(HttpRequestData req)
        {
            if (req.Headers.TryGetValues("X-Forwarded-For", out var vals))
                return vals.First().Split(',')[0].Trim();
            return req.Url.Host;
        }
    }
}
