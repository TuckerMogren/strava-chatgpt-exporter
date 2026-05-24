using StravaExporter.Infrastructure.Strava.Client;
using StravaExporter.Infrastructure.Strava.Mapping;

namespace StravaExporter.Infrastructure.Tests;

public sealed class StravaActivityMapperTests
{
    [Fact]
    public void ToDomain_prefers_sport_type()
    {
        var activity = new StravaActivityResponse
        {
            Id = 123,
            Name = "Morning Run",
            Type = "Workout",
            SportType = "Run",
            DistanceMeters = 1609.344
        }.ToDomain();

        Assert.Equal("Run", activity.Type);
        Assert.Equal(1, activity.DistanceMiles);
    }
}
