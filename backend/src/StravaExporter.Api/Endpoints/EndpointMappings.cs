using StravaExporter.Application.Activities;
using StravaExporter.Application.Auth;
using StravaExporter.Application.Exports;
using StravaExporter.Domain.Exports;

namespace StravaExporter.Api.Endpoints;

public static class EndpointMappings
{
    public static IEndpointRouteBuilder MapStravaAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/strava").WithTags("Strava Auth");

        group.MapGet("/connect", (IStravaOAuthService oauth, HttpContext context) =>
        {
            var state = Guid.NewGuid().ToString("N");
            context.Response.Cookies.Append("strava_oauth_state", state, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Secure = false,
                MaxAge = TimeSpan.FromMinutes(10)
            });
            return Results.Redirect(oauth.BuildAuthorizationUrl(state));
        });

        group.MapGet("/callback", async (string code, string state, IStravaOAuthService oauth, IConfiguration configuration, HttpContext context, CancellationToken cancellationToken) =>
        {
            if (!context.Request.Cookies.TryGetValue("strava_oauth_state", out var expectedState) || expectedState != state)
            {
                return Results.BadRequest(new { error = "InvalidOAuthState", message = "OAuth state did not match." });
            }

            await oauth.CompleteCallbackAsync(code, state, cancellationToken);
            context.Response.Cookies.Delete("strava_oauth_state");
            return Results.Redirect(configuration["Frontend:RedirectAfterConnect"] ?? "http://localhost:5173/connected");
        });

        group.MapDelete("/disconnect", async (IStravaTokenStore store, CancellationToken cancellationToken) =>
        {
            var token = await store.GetDefaultAsync(cancellationToken);
            if (token is not null)
            {
                await store.DeleteAsync(token.AthleteId, cancellationToken);
            }

            return Results.NoContent();
        });

        group.MapGet("/status", async (IStravaTokenStore store, CancellationToken cancellationToken) =>
        {
            var token = await store.GetDefaultAsync(cancellationToken);
            return Results.Ok(new { connected = token is not null, athleteId = token?.AthleteId });
        });

        return app;
    }

    public static IEndpointRouteBuilder MapAthleteEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/athlete", async (IStravaService strava, CancellationToken cancellationToken) =>
            Results.Ok(await strava.GetCurrentAthleteAsync(cancellationToken))).WithTags("Athlete");
        return app;
    }

    public static IEndpointRouteBuilder MapActivityEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/activities").WithTags("Activities");
        group.MapGet("", async (DateOnly? after, DateOnly? before, string? type, IStravaService strava, CancellationToken cancellationToken) =>
        {
            var activities = await strava.GetActivitiesAsync(new GetActivitiesQuery
            {
                After = after,
                Before = before,
                ActivityTypes = string.IsNullOrWhiteSpace(type) ? [] : [type]
            }, cancellationToken);

            return Results.Ok(activities.Select(x => x.ToSummary()));
        });

        group.MapGet("/{activityId:long}", async (long activityId, IStravaService strava, CancellationToken cancellationToken) =>
            Results.Ok((await strava.GetActivityAsync(activityId, cancellationToken)).ToSummary()));
        return app;
    }

    public static IEndpointRouteBuilder MapExportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/exports").WithTags("Exports");
        group.MapPost("/", async (CreateExportCommand command, IStravaExportService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.CreateExportAsync(command, cancellationToken)));
        group.MapGet("/", async (IStravaExportService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetExportsAsync(cancellationToken)));
        group.MapGet("/{exportId:guid}", async (Guid exportId, IStravaExportService service, CancellationToken cancellationToken) =>
            await service.GetExportAsync(exportId, cancellationToken) is { } export ? Results.Ok(export) : Results.NotFound());
        group.MapGet("/{exportId:guid}/download", async (Guid exportId, string format, IStravaExportService service, CancellationToken cancellationToken) =>
        {
            if (!Enum.TryParse<ExportFormat>(format, true, out var parsed))
            {
                return Results.BadRequest(new { error = "InvalidExportFormat", message = "Format must be json, csv, or markdown." });
            }

            var download = await service.DownloadExportAsync(exportId, parsed, cancellationToken);
            return Results.File(download.Content, download.ContentType, download.FileName);
        });
        return app;
    }
}
