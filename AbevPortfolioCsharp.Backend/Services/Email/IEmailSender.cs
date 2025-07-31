using System.Threading;
using System.Threading.Tasks;
using AbevPortfolioCsharp.Backend.Models;

namespace AbevPortfolioCsharp.Backend.Services.Email
{
    /// <summary>
    /// Send an email for the given contact request.
    /// </summary>
    public interface IEmailSender
    {
        /// <param name="req">The user’s contact form data.</param>
        /// <param name="ct">The cancellation token.</param>
        Task<bool> SendAsync(MinimalContactRequest req, CancellationToken ct = default);
    }
}
