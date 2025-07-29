namespace AbevPortfolioCsharp.Backend.Services.RateLimiting;
public interface IRateLimiter
{
    Task<bool> IsAllowedAsync(string ip, CancellationToken ct = default);
}
