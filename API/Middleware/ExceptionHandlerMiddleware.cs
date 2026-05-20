using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Localization;
using SynapseEnqueteManagement.BL;
using SynapseEnqueteManagement.BL.Exceptions;

namespace SynapseEnqueteManagement.API.Middleware;

public class ExceptionHandlerMiddleware(RequestDelegate next, ILogger<ExceptionHandlerMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, IStringLocalizer<SharedResource> localizer)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex, localizer);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception, IStringLocalizer<SharedResource> localizer)
    {
        var (statusCode, message) = exception switch
        {
            LocalizedValidationException lve => (HttpStatusCode.BadRequest, Translate(localizer, lve)),
            ValidationException ve => (HttpStatusCode.BadRequest, ve.Message),
            KeyNotFoundException => (HttpStatusCode.NotFound, "The requested resource was not found."),
            ArgumentException => (HttpStatusCode.BadRequest, "The request was invalid."),
            UnauthorizedAccessException => (HttpStatusCode.Forbidden, "Access denied."),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred.")
        };

        if (statusCode == HttpStatusCode.InternalServerError)
            logger.LogError(exception, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
        else
            logger.LogWarning(exception, "Handled exception ({StatusCode}) for {Method} {Path}", (int)statusCode, context.Request.Method, context.Request.Path);

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var response = JsonSerializer.Serialize(new { error = message });
        await context.Response.WriteAsync(response);
    }

    private static string Translate(IStringLocalizer<SharedResource> localizer, LocalizedValidationException ex)
    {
        var entry = ex.Args.Length == 0 ? localizer[ex.Code] : localizer[ex.Code, ex.Args];
        return entry.ResourceNotFound ? ex.Code : entry.Value;
    }
}
