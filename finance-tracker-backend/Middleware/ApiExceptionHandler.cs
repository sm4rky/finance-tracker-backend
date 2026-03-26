using System.Net.Mime;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Supabase.Postgrest.Exceptions;

namespace finance_tracker_backend.Middleware;

/// <summary>JSON <c>{ "message": "..." }</c> for API clients (e.g. frontend parseApiErrorMessage).</summary>
public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException)
            return false;

        if (exception is BadHttpRequestException)
            return false;

        if (exception is PostgrestException pg)
        {
            logger.LogError(pg, "Supabase PostgREST error");
            var status = pg.StatusCode is >= 400 and < 600
                ? pg.StatusCode
                : StatusCodes.Status502BadGateway;
            await WriteMessageAsync(httpContext, status, pg.Message, cancellationToken).ConfigureAwait(false);
            return true;
        }

        if (exception is ArgumentException ae)
        {
            logger.LogWarning(ae, "Bad request");
            await WriteMessageAsync(httpContext, StatusCodes.Status400BadRequest, ae.Message, cancellationToken)
                .ConfigureAwait(false);
            return true;
        }

        if (exception is InvalidOperationException ioe)
        {
            logger.LogWarning(ioe, "Invalid operation during request");
            await WriteMessageAsync(httpContext, StatusCodes.Status500InternalServerError, ioe.Message, cancellationToken)
                .ConfigureAwait(false);
            return true;
        }

        logger.LogError(exception, "Unhandled exception");
        await WriteMessageAsync(
                httpContext,
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred.",
                cancellationToken)
            .ConfigureAwait(false);
        return true;
    }

    private static async Task WriteMessageAsync(
        HttpContext httpContext,
        int statusCode,
        string message,
        CancellationToken cancellationToken)
    {
        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = MediaTypeNames.Application.Json;
        await httpContext.Response.WriteAsJsonAsync(new { message }, JsonOptions, cancellationToken)
            .ConfigureAwait(false);
    }
}
