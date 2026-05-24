namespace StravaExporter.Infrastructure.Strava;

public sealed class StravaOptions
{
    public const string SectionName = "Strava";
    public required string ClientId { get; init; }
    public required string ClientSecret { get; init; }
    public required string RedirectUri { get; init; }
    public string AuthorizationBaseUrl { get; init; } = "https://www.strava.com/oauth/authorize";
    public string TokenUrl { get; init; } = "https://www.strava.com/oauth/token";
    public string ApiBaseUrl { get; init; } = "https://www.strava.com/api/v3";
}
