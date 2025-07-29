namespace AbevPortfolioCsharp.Backend.Services.Email;
public interface IEmailSender
{
    Task<bool> SendAsync(string to, string from, string replyTo, string subject, string html, CancellationToken ct = default);
}
