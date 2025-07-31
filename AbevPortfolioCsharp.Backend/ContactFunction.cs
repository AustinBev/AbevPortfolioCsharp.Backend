using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace AbevPortfolioCsharp.Backend;

public record MinimalContactRequest(
    string Name,
    string Email,
    string Message,
    string VerificationUrl,
    string? Hp,
    int SecondsToSubmit,
    string? TurnstileToken);

public class ContactFunction
{
    private readonly ILogger<ContactFunction> _log;

    public ContactFunction(ILogger<ContactFunction> log) => _log = log;

    // ▶ 1. Worker attribute — shows as “contact” in the portal
    [Function("contact")]
    public async Task<HttpResponseData> Run(
        // ▶ 2. Route == "contact"  →  /api/contact
        [HttpTrigger(AuthorizationLevel.Function,
                     "post",
                     Route = "contact")] HttpRequestData req)
    {
        _log.LogInformation("Contact endpoint hit");

        var dto = await req.ReadFromJsonAsync<MinimalContactRequest>();
        if (dto is null)
            return req.CreateResponse(HttpStatusCode.BadRequest);

        // ---- TODO: verify Turnstile token & send email here ----
        // (keep your SendGrid / Resend code)

        var res = req.CreateResponse(HttpStatusCode.OK);
        await res.WriteAsJsonAsync(new { ok = true });
        return res;
    }
}
