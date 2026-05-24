using StravaExporter.Application.Exports;
using StravaExporter.Domain.Activities;
using StravaExporter.Domain.Exports;

namespace StravaExporter.Application.Tests;

public sealed class ExportSummaryCalculatorTests
{
    [Fact]
    public void Create_calculates_totals_and_average_pace()
    {
        var calculator = new ExportSummaryCalculator();
        var document = calculator.Create(DateTimeOffset.UnixEpoch, new ExportRequest(), [
            new StravaActivity
            {
                Id = 1,
                Name = "Morning Run",
                Type = "Run",
                DistanceMeters = 1609.344,
                MovingTimeSeconds = 600,
                ElapsedTimeSeconds = 650
            },
            new StravaActivity
            {
                Id = 2,
                Name = "Evening Run",
                Type = "Run",
                DistanceMeters = 3218.688,
                MovingTimeSeconds = 1200,
                ElapsedTimeSeconds = 1300
            }
        ]);

        Assert.Equal(2, document.Totals.ActivityCount);
        Assert.Equal(3, document.Totals.TotalDistanceMiles);
        Assert.Equal(1800, document.Totals.TotalMovingTimeSeconds);
        Assert.Equal(600, document.Totals.AveragePaceSecondsPerMile);
    }
}
