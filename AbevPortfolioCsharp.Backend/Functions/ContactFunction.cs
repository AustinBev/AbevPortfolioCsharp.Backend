using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using AbevPortfolioCsharp.Backend.Models;
using AbevPortfolioCsharp.Backend.Services.Email;
using AbevPortfolioCsharp.Backend.Services.RateLimiting;
using AbevPortfolioCsharp.Backend.Services;

namespace AbevPortfolioCsharp.Backend.Functions
{
    public class ContactFunction
    {
        private readonly ILogger<ContactFunction> _log;
        private readonly ITurnstileVerifier _turnstile;
        private readonly IRateLimiter _limiter;
        private readonly IEmailSender _emailSender;

        public ContactFunction(
            ILogger<ContactFunction> log,
            ITurnstileVerifier turnstile,
            IRateLimiter limiter,
            IEmailSender emailSender)
        {
            _log = log;
            _turnstile = turnstile;
            _limiter = limiter;
            _emailSender = emailSender;
        }

        [Function("contact")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "contact")]
            HttpRequestData req,
            FunctionContext context)
        {
            var ct = context.CancellationToken;
            _log.LogInformation("Contact endpoint hit");

            // 1) Deserialize
            var dto = await req.ReadFromJsonAsync<MinimalContactRequest>(cancellationToken: ct);
            if (dto is null)
                return req.CreateResponse(HttpStatusCode.BadRequest);

            // 2) Honeypot & rate-limit
            if (!string.IsNullOrEmpty(dto.Hp) || !await _limiter.AllowAsync(req))
                return req.CreateResponse(HttpStatusCode.OK);

            // 3) Captcha: pass token + client-IP + cancellation
            var clientIp = GetClientIp(req);
            if (!await _turnstile.VerifyAsync(dto.TurnstileToken, clientIp, ct))
            {
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync("Captcha failed");
                return bad;
            }

            // 4) Send email: pass DTO + cancellation
            var success = await _emailSender.SendAsync(dto, ct);
            if (!success)
            {
                var err = req.CreateResponse(HttpStatusCode.InternalServerError);
                await err.WriteStringAsync("Email send failed");
                return err;
            }

            // 5) All good
            var ok = req.CreateResponse(HttpStatusCode.OK);
            await ok.WriteStringAsync("{\"ok\":true}");
            return ok;
        }

        private static string GetClientIp(HttpRequestData req)
        {
            if (req.Headers.TryGetValues("X-Forwarded-For", out var h))
                return h.First().Split(',')[0].Trim();
            return req.Url.Host;
        }
    }
}
