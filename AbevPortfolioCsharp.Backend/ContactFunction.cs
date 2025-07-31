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

    public ContactFunction(ILogger<ContactFunction> log)
        => _log = log;

    [Function("contact")]  // ← this name appears in the portal
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "contact")]
        HttpRequestData req)
    {
        _log.LogInformation("Contact endpoint hit");

        var dto = await req.ReadFromJsonAsync<MinimalContactRequest>();
        if (dto is null)
            return req.CreateResponse(HttpStatusCode.BadRequest);

        // …your email-sending code here…

        var res = req.CreateResponse(HttpStatusCode.OK);
        await res.WriteAsJsonAsync(new { ok = true });
        return res;
    }
}
