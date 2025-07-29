using System.Web;

namespace AbevPortfolioCsharp.Backend.Services.Email;

public static class EmailTemplates
{
    public static string BuildContactHtml(string name, string email, string verificationUrl, string? message)
    {
        string Host(string url)
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out var u)) return u.Host;
            return "(invalid)";
        }
        string esc(string s) => HttpUtility.HtmlEncode(s);
        var host = Host(verificationUrl);
        var msg = string.IsNullOrWhiteSpace(message) ? "(no message)" : esc(message);

        return $@"
                <style>body{{font-family:Segoe UI,Arial,sans-serif;}}</style>
                <h2>New portfolio inquiry</h2>
                <p><b>Name:</b> {esc(name)}<br/>
                <b>Email:</b> {esc(email)}<br/>
                <b>Verification:</b> <a href=""{esc(verificationUrl)}"">{esc(verificationUrl)}</a> ({host})</p>
                <p><b>Message</b></p>
                <p style='white-space:pre-wrap'>{msg}</p>";
    }
}
