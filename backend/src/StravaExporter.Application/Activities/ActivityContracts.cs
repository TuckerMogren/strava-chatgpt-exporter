using StravaExporter.Domain.Activities;
using StravaExporter.Domain.Athletes;

namespace StravaExporter.Application.Activities;

public interface IStravaService
{
    Task<StravaAthlete> GetCurrentAthleteAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<StravaActivity>> GetActivitiesAsync(GetActivitiesQuery query, CancellationToken cancellationToken);
    Task<StravaActivity> GetActivityAsync(long activityId, CancellationToken cancellationToken);
}

public sealed class GetActivitiesQuery
{
    public DateOnly? After { get; init; }
    public DateOnly? Before { get; init; }
    public IReadOnlyCollection<string> ActivityTypes { get; init; } = [];
    public int PageSize { get; init; } = 50;
}

public sealed record ActivitySummary(
    long Id,
    string Name,
    string Type,
    DateTimeOffset StartDateLocal,
    double DistanceMeters,
    double DistanceMiles,
    int MovingTimeSeconds,
    int ElapsedTimeSeconds,
    double? AveragePaceSecondsPerMile,
    double? AverageHeartRate,
    double? MaxHeartRate,
    double? ElevationGainFeet,
    double? AverageSpeedMetersPerSecond,
    double? MaxSpeedMetersPerSecond);

public static class ActivitySummaryMapper
{
    public static ActivitySummary ToSummary(this StravaActivity activity) =>
        new(
            activity.Id,
            activity.Name,
            activity.Type,
            activity.StartDateLocal,
            activity.DistanceMeters,
            Math.Round(activity.DistanceMiles, 3),
            activity.MovingTimeSeconds,
            activity.ElapsedTimeSeconds,
            activity.AveragePaceSecondsPerMile is null ? null : Math.Round(activity.AveragePaceSecondsPerMile.Value, 1),
            activity.AverageHeartRate,
            activity.MaxHeartRate,
            activity.ElevationGainFeet is null ? null : Math.Round(activity.ElevationGainFeet.Value, 1),
            activity.AverageSpeedMetersPerSecond,
            activity.MaxSpeedMetersPerSecond);
}
