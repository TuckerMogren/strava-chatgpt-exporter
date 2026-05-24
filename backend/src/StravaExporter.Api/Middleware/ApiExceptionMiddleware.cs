using FluentValidation;
using StravaExporter.Application.Auth;
using StravaExporter.Application.Common;

namespace StravaExporter.Api.Middleware;

public sealed class ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (StravaNotConnectedException ex)
        {
            await Write(context, StatusCodes.Status409Conflict, new { error = "StravaNotConnected", message = ex.Message });
        }
        catch (StravaRateLimitException ex)
        {
            await Write(context, StatusCodes.Status429TooManyRequests, new
            {
                error = "StravaRateLimitExceeded",
                message = ex.Message,
                ex.RetryAfterUtc
            });
        }
        catch (ValidationException ex)
        {
            await Write(context, StatusCodes.Status400BadRequest, new
            {
                error = "ValidationFailed",
                message = "The request is invalid.",
                errors = ex.Errors.Select(x => new { x.PropertyName, x.ErrorMessage })
            });
        }
        catch (KeyNotFoundException ex)
        {
            await Write(context, StatusCodes.Status404NotFound, new { error = "NotFound", message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled API exception.");
            await Write(context, StatusCodes.Status500InternalServerError, new { error = "UnexpectedError", message = "An unexpected error occurred." });
        }
    }

    private static async Task Write(HttpContext context, int statusCode, object body)
    {
        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(body);
    }
}
