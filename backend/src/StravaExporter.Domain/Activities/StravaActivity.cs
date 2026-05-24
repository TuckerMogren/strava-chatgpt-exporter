namespace StravaExporter.Domain.Activities;

public sealed class StravaActivity
{
    public required long Id { get; init; }
    public required string Name { get; init; }
    public required string Type { get; init; }
    public DateTimeOffset StartDateUtc { get; init; }
    public DateTimeOffset StartDateLocal { get; init; }
    public double DistanceMeters { get; init; }
    public int MovingTimeSeconds { get; init; }
    public int ElapsedTimeSeconds { get; init; }
    public double? TotalElevationGainMeters { get; init; }
    public double? AverageSpeedMetersPerSecond { get; init; }
    public double? MaxSpeedMetersPerSecond { get; init; }
    public double? AverageHeartRate { get; init; }
    public double? MaxHeartRate { get; init; }
    public double? AverageWatts { get; init; }
    public double DistanceMiles => DistanceMeters / 1609.344;
    public double? AveragePaceSecondsPerMile => DistanceMiles > 0 ? MovingTimeSeconds / DistanceMiles : null;
    public double? ElevationGainFeet => TotalElevationGainMeters * 3.280839895;
}
