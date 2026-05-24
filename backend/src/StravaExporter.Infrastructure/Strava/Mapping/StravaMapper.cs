using StravaExporter.Domain.Activities;
using StravaExporter.Domain.Athletes;
using StravaExporter.Infrastructure.Strava.Client;

namespace StravaExporter.Infrastructure.Strava.Mapping;

public static class StravaMapper
{
    public static StravaAthlete ToDomain(this StravaAthleteResponse response) =>
        new()
        {
            Id = response.Id,
            Username = response.Username,
            FirstName = response.FirstName,
            LastName = response.LastName
        };

    public static StravaActivity ToDomain(this StravaActivityResponse response) =>
        new()
        {
            Id = response.Id,
            Name = response.Name ?? "Untitled activity",
            Type = response.SportType ?? response.Type ?? "Unknown",
            StartDateUtc = response.StartDateUtc,
            StartDateLocal = response.StartDateLocal,
            DistanceMeters = response.DistanceMeters,
            MovingTimeSeconds = response.MovingTimeSeconds,
            ElapsedTimeSeconds = response.ElapsedTimeSeconds,
            TotalElevationGainMeters = response.TotalElevationGainMeters,
            AverageSpeedMetersPerSecond = response.AverageSpeedMetersPerSecond,
            MaxSpeedMetersPerSecond = response.MaxSpeedMetersPerSecond,
            AverageHeartRate = response.AverageHeartRate,
            MaxHeartRate = response.MaxHeartRate,
            AverageWatts = response.AverageWatts
        };
}
