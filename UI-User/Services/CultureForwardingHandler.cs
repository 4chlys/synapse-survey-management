using System.Globalization;

namespace SynapseEnqueteManagement.UI.User.Services;

public class CultureForwardingHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var culture = CultureInfo.CurrentUICulture.Name;
        if (!string.IsNullOrEmpty(culture))
            request.Headers.AcceptLanguage.TryParseAdd(culture);
        return base.SendAsync(request, ct);
    }
}
