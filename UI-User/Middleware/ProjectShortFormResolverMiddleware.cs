using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using SynapseEnqueteManagement.UI.User.Services.Project;

namespace SynapseEnqueteManagement.UI.User.Middleware;

public class ProjectShortFormResolverMiddleware(
    RequestDelegate next,
    IMemoryCache cache,
    IConfiguration configuration)
{
    private static readonly string[] HardReserved = ["admin", "api", "www"];

    private const string NotFoundSentinel = "__notfound__";

    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks = new();

    private HashSet<string> GetReservedSet()
    {
        var configured = configuration.GetSection("Subdomains:Reserved").Get<string[]>()
                         ?? ["admin", "api", "www", "app", "mail", "static", "assets", "cdn", "auth", "health"];
        var set = new HashSet<string>(configured, StringComparer.OrdinalIgnoreCase);
        foreach (var r in HardReserved)
            set.Add(r);
        return set;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";
        if (path.StartsWith("/_") || path.StartsWith("/dist/") || Path.HasExtension(path))
        {
            await next(context);
            return;
        }

        var host = context.Request.Host.Host;
        var baseDomain = configuration["Subdomains:BaseDomain"] ?? "synapse-ip.xyz";
        var subdomain = ExtractSubdomain(host, baseDomain);

        if (subdomain == null || GetReservedSet().Contains(subdomain))
        {
            await next(context);
            return;
        }

        var cacheKey = $"sf:{subdomain}";
        var staleKey = $"sf-last:{subdomain}";

        if (!cache.TryGetValue(cacheKey, out string cachedValue))
        {
            // Single-flight: under a burst on a cold cache only one request calls the API,
            // the rest wait and read the cached value — otherwise N requests fan out and trip its rate limit.
            var gate = Locks.GetOrAdd(subdomain, _ => new SemaphoreSlim(1, 1));
            await gate.WaitAsync();
            try
            {
                if (!cache.TryGetValue(cacheKey, out cachedValue))
                {
                    var projectService = context.RequestServices.GetRequiredService<IProjectApiService>();
                    try
                    {
                        var lookup = await projectService.GetByShortFormAsync(subdomain);

                        if (lookup == null)
                        {
                            cachedValue = NotFoundSentinel;
                            cache.Set(cacheKey, cachedValue, TimeSpan.FromSeconds(10));
                        }
                        else
                        {
                            cachedValue = $"{lookup.ProjectId}|{lookup.ProjectState}";
                            cache.Set(cacheKey, cachedValue, TimeSpan.FromSeconds(60));
                            cache.Set(staleKey, cachedValue, TimeSpan.FromHours(1));
                        }
                    }
                    catch (HttpRequestException)
                    {
                        // API down or rate-limiting us: serve the last-known-good mapping if we have one,
                        // otherwise fail soft with 503 instead of bubbling an unhandled 500.
                        if (!cache.TryGetValue(staleKey, out cachedValue))
                        {
                            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                            context.Response.Headers.RetryAfter = "5";
                            return;
                        }
                    }
                }
            }
            finally
            {
                gate.Release();
            }
        }

        if (cachedValue == NotFoundSentinel)
        {
            context.Request.Path = "/Home/ShortFormNotFound";
            await next(context);
            return;
        }

        var parts = cachedValue.Split('|');
        var projectId = int.Parse(parts[0]);
        var projectState = parts[1];

        if (projectState != "Active")
        {
            context.Request.Path = "/Home/ShortFormInactive";
            await next(context);
            return;
        }

        var projectPrefix = $"/{projectId}";
        var currentPath = context.Request.Path.Value ?? "/";

        if (!currentPath.StartsWith(projectPrefix))
            context.Request.Path = projectPrefix + context.Request.Path;

        context.Items["ResolvedProject"] = new { ProjectId = projectId, ShortForm = subdomain, ProjectState = projectState };

        await next(context);
    }

    private static string ExtractSubdomain(string host, string baseDomain)
    {
        if (string.IsNullOrEmpty(host))
            return null;

        if (System.Net.IPAddress.TryParse(host, out _))
            return null;

        var labels = host.Split('.');

        if (labels.Length < 2)
            return null;

        if (string.Equals(host, baseDomain, StringComparison.OrdinalIgnoreCase))
            return null;

        if (host.EndsWith("." + baseDomain, StringComparison.OrdinalIgnoreCase))
            return labels[0].ToLowerInvariant();

        return null;
    }
}
