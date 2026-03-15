using System.Security.Cryptography;
using System.Text;

namespace SynapseEnqueteManagement.API.Middleware;

/// <summary>
/// Validates the X-Api-Key header on every request.
/// Rejects with 401 if the key is missing or incorrect.
/// </summary>
public class ApiKeyMiddleware
{
    private const string ApiKeyHeaderName = "X-Api-Key";
    private readonly RequestDelegate _next;
    private readonly byte[] _apiKeyBytes;
    private readonly ILogger<ApiKeyMiddleware> _logger;

    public ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<ApiKeyMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        var apiKey = configuration["ApiKey"]
                     ?? throw new InvalidOperationException(
                         "ApiKey is not configured. Set it in appsettings.json or as an environment variable.");
        _apiKeyBytes = Encoding.UTF8.GetBytes(apiKey);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey)
            || !CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(extractedApiKey.ToString()),
                _apiKeyBytes))
        {
            _logger.LogWarning("Rejected request with invalid or missing API key from {IP} to {Path}",
                context.Connection.RemoteIpAddress, context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Invalid or missing API key.");
            return;
        }

        await _next(context);
    }
}