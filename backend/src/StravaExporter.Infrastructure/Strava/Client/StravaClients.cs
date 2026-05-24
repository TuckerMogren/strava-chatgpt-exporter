using System.Text.Json.Serialization;
using Refit;

namespace StravaExporter.Infrastructure.Strava.Client;

public interface IStravaApiClient
{
    [Get("/athlete")]
    Task<StravaAthleteResponse> GetAthleteAsync([Header("Authorization")] string authorization, CancellationToken cancellationToken);

    [Get("/athlete/activities")]
    Task<IReadOnlyCollection<StravaActivityResponse>> GetActivitiesAsync(
        [Header("Authorization")] string authorization,
        [Query] long? after,
        [Query] long? before,
        [Query] int page,
        [Query] int per_page,
        CancellationToken cancellationToken);

    [Get("/activities/{activityId}")]
    Task<StravaActivityResponse> GetActivityAsync([Header("Authorization")] string authorization, long activityId, CancellationToken cancellationToken);
}

public interface IStravaOAuthClient
{
    [Post("/oauth/token")]
    Task<StravaTokenResponse> ExchangeCodeAsync([Body(BodySerializationMethod.UrlEncoded)] Dictionary<string, string> request, CancellationToken cancellationToken);

    [Post("/oauth/token")]
    Task<StravaTokenResponse> RefreshTokenAsync([Body(BodySerializationMethod.UrlEncoded)] Dictionary<string, string> request, CancellationToken cancellationToken);
}

public sealed record StravaAthleteResponse(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("username")] string? Username,
    [property: JsonPropertyName("firstname")] string? FirstName,
    [property: JsonPropertyName("lastname")] string? LastName);

public sealed record StravaTokenResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("refresh_token")] string RefreshToken,
    [property: JsonPropertyName("expires_at")] long ExpiresAt,
    [property: JsonPropertyName("athlete")] StravaAthleteResponse Athlete);

public sealed record StravaActivityResponse
{
    [JsonPropertyName("id")] public long Id { get; init; }
    [JsonPropertyName("name")] public string? Name { get; init; }
    [JsonPropertyName("type")] public string? Type { get; init; }
    [JsonPropertyName("sport_type")] public string? SportType { get; init; }
    [JsonPropertyName("start_date")] public DateTimeOffset StartDateUtc { get; init; }
    [JsonPropertyName("start_date_local")] public DateTimeOffset StartDateLocal { get; init; }
    [JsonPropertyName("distance")] public double DistanceMeters { get; init; }
    [JsonPropertyName("moving_time")] public int MovingTimeSeconds { get; init; }
    [JsonPropertyName("elapsed_time")] public int ElapsedTimeSeconds { get; init; }
    [JsonPropertyName("total_elevation_gain")] public double? TotalElevationGainMeters { get; init; }
    [JsonPropertyName("average_speed")] public double? AverageSpeedMetersPerSecond { get; init; }
    [JsonPropertyName("max_speed")] public double? MaxSpeedMetersPerSecond { get; init; }
    [JsonPropertyName("average_heartrate")] public double? AverageHeartRate { get; init; }
    [JsonPropertyName("max_heartrate")] public double? MaxHeartRate { get; init; }
    [JsonPropertyName("average_watts")] public double? AverageWatts { get; init; }
}
