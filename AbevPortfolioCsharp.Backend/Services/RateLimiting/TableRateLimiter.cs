using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;

namespace AbevPortfolioCsharp.Backend.Services.RateLimiting
{
    public class TableRateLimiter : IRateLimiter
    {
        private readonly TableClient _table;
        private readonly int _perHour;
        private readonly int _perDay;

        public TableRateLimiter(IConfiguration cfg, TableServiceClient svc)
        {
            _table = svc.GetTableClient("RateLimits");
            _table.CreateIfNotExists();
            _perHour = int.TryParse(cfg["RATE_LIMIT_PER_HOUR"], out var h) ? h : 3;
            _perDay = int.TryParse(cfg["RATE_LIMIT_PER_DAY"], out var d) ? d : 10;
        }

        /// <summary>
        /// Interface implementation – extract IP and delegate to your existing logic.
        /// </summary>
        public Task<bool> AllowAsync(HttpRequestData req)
        {
            var ip = GetClientIp(req);
            return IsAllowedAsync(ip);
        }

        /// <summary>
        /// Your original per‐hour / per‐day counter logic (fixed format strings).
        /// </summary>
        private async Task<bool> IsAllowedAsync(string ip, CancellationToken ct = default)
        {
            var now = DateTimeOffset.UtcNow;
            var hourKey = $"{ip}|{now:yyyyMMddHH}";
            var dayKey = $"{ip}|{now:yyyyMMdd}";

            var hourCount = await IncrementAsync("hour", hourKey, now.AddHours(1), ct);
            var dayCount = await IncrementAsync("day", dayKey, now.AddDays(1), ct);

            return hourCount <= _perHour && dayCount <= _perDay;
        }

        private static string GetClientIp(HttpRequestData req)
        {
            if (req.Headers.TryGetValues("X-Forwarded-For", out var vals))
                return vals.First().Split(',')[0].Trim();
            return req.Url.Host;
        }

        private async Task<int> IncrementAsync(
            string partition,
            string row,
            DateTimeOffset expires,
            CancellationToken ct)
        {
            var entity = new TableEntity(partition, row);
            try
            {
                var existing = await _table.GetEntityAsync<TableEntity>(
                    partition, row, cancellationToken: ct);
                var count = (existing.Value.GetInt32("Count") ?? 0) + 1;
                existing.Value["Count"] = count;
                existing.Value["ExpiresAt"] = expires;
                await _table.UpdateEntityAsync(
                    existing.Value,
                    existing.Value.ETag,
                    TableUpdateMode.Replace,
                    ct);
                return count;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                entity["Count"] = 1;
                entity["ExpiresAt"] = expires;
                await _table.AddEntityAsync(entity, ct);
                return 1;
            }
        }
    }
}
