using Microsoft.Azure.Functions.Worker.Http;
using System.Threading.Tasks;

namespace AbevPortfolioCsharp.Backend.Services.RateLimiting
{
    /// <summary>
    /// Returns true if this request should be allowed; false to silently throttle.
    /// </summary>
    public interface IRateLimiter
    {
        Task<bool> AllowAsync(HttpRequestData req);
    }
}
