using System.Net;
using SynapseEnqueteManagement.UI.Admin.Services.Email;

namespace SynapseEnqueteManagement.UI.Admin.Tests.Unit;

public class EmailApiServiceTests
{
    [Fact]
    public async Task RetryAsync_ReturnsNull_WhenApiReturnsNotFound()
    {
        // A scoped retry on a job outside the caller's platform comes back 404;
        // the service must swallow it (return null) so the page redirects rather
        // than throwing on EnsureSuccessStatusCode.
        var http = new HttpClient(new StubHandler(HttpStatusCode.NotFound))
        {
            BaseAddress = new Uri("http://localhost")
        };
        var service = new EmailApiService(http);

        var result = await service.RetryAsync(7, projectPlatformId: 3);

        Assert.Null(result);
    }

    private sealed class StubHandler(HttpStatusCode status) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(status));
    }
}
