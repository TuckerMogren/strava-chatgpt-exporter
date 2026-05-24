using System.Text.Json;
using FluentValidation;
using StravaExporter.Application.Activities;
using StravaExporter.Domain.Activities;
using StravaExporter.Domain.Exports;

namespace StravaExporter.Application.Exports;

public interface IStravaExportService
{
    Task<ExportResult> CreateExportAsync(CreateExportCommand command, CancellationToken cancellationToken);
    Task<ExportDownload> DownloadExportAsync(Guid exportId, ExportFormat format, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ExportJobSummary>> GetExportsAsync(CancellationToken cancellationToken);
    Task<ExportJobSummary?> GetExportAsync(Guid exportId, CancellationToken cancellationToken);
}

public interface IExportJobStore
{
    Task SaveAsync(ExportJob job, IReadOnlyCollection<ExportFile> files, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ExportJobSummary>> ListAsync(CancellationToken cancellationToken);
    Task<ExportJobSummary?> GetAsync(Guid exportId, CancellationToken cancellationToken);
    Task<ExportDownload?> GetFileAsync(Guid exportId, ExportFormat format, CancellationToken cancellationToken);
}

public interface IExportFileSink
{
    Task SaveAsync(IReadOnlyCollection<ExportFile> files, CancellationToken cancellationToken);
}

public interface IExportWriter
{
    ExportFormat Format { get; }
    string ContentType { get; }
    string FileExtension { get; }
    byte[] Write(ExportDocument document);
}

public sealed class CreateExportCommand : ExportRequest
{
}

public sealed class CreateExportCommandValidator : AbstractValidator<CreateExportCommand>
{
    public CreateExportCommandValidator()
    {
        RuleFor(x => x).Must(x => x.After is null || x.Before is null || x.After <= x.Before)
            .WithMessage("After must be on or before Before.");
        RuleFor(x => x.PageSize()).InclusiveBetween(1, 200);
        RuleFor(x => x.IncludeStreams).Equal(false)
            .WithMessage("Activity streams are outside the MVP scope.");
    }
}

public static class ExportRequestExtensions
{
    public static int PageSize(this ExportRequest _) => 50;
}

public sealed record ExportResult(Guid ExportId, DateTimeOffset CreatedAtUtc, IReadOnlyDictionary<string, string> Downloads);
public sealed record ExportDownload(string FileName, string ContentType, byte[] Content);
public sealed record ExportJob(Guid Id, long AthleteId, ExportRequest Request, DateTimeOffset CreatedAtUtc);
public sealed record ExportFile(Guid ExportId, ExportFormat Format, string FileName, string ContentType, byte[] Content, DateTimeOffset CreatedAtUtc);
public sealed record ExportJobSummary(Guid Id, long AthleteId, ExportRequest Request, DateTimeOffset CreatedAtUtc);

public sealed record ExportDocument(
    DateTimeOffset ExportedAtUtc,
    ExportRequest Filters,
    ExportTotals Totals,
    IReadOnlyCollection<ActivitySummary> Activities);

public sealed record ExportTotals(
    int ActivityCount,
    double TotalDistanceMiles,
    int TotalMovingTimeSeconds,
    double? AveragePaceSecondsPerMile,
    double? AverageHeartRate);

public sealed class ExportSummaryCalculator
{
    public ExportDocument Create(DateTimeOffset exportedAtUtc, ExportRequest request, IReadOnlyCollection<StravaActivity> activities)
    {
        var summaries = activities.Select(x => x.ToSummary()).ToArray();
        var totalDistance = summaries.Sum(x => x.DistanceMiles);
        var totalMovingTime = summaries.Sum(x => x.MovingTimeSeconds);
        var heartRates = summaries.Where(x => x.AverageHeartRate is not null).Select(x => x.AverageHeartRate!.Value).ToArray();

        return new ExportDocument(
            exportedAtUtc,
            request,
            new ExportTotals(
                summaries.Length,
                Math.Round(totalDistance, 2),
                totalMovingTime,
                totalDistance > 0 ? Math.Round(totalMovingTime / totalDistance, 1) : null,
                heartRates.Length == 0 ? null : Math.Round(heartRates.Average(), 1)),
            summaries);
    }
}

public sealed class StravaExportService(
    IStravaService stravaService,
    IExportJobStore store,
    IEnumerable<IExportFileSink> fileSinks,
    IEnumerable<IExportWriter> writers,
    ExportSummaryCalculator calculator) : IStravaExportService
{
    public async Task<ExportResult> CreateExportAsync(CreateExportCommand command, CancellationToken cancellationToken)
    {
        await new CreateExportCommandValidator().ValidateAndThrowAsync(command, cancellationToken);
        var athlete = await stravaService.GetCurrentAthleteAsync(cancellationToken);
        var activities = await stravaService.GetActivitiesAsync(new GetActivitiesQuery
        {
            After = command.After,
            Before = command.Before,
            ActivityTypes = command.ActivityTypes,
            PageSize = command.PageSize()
        }, cancellationToken);

        var exportId = Guid.NewGuid();
        var createdAtUtc = DateTimeOffset.UtcNow;
        var request = new ExportRequest
        {
            After = command.After,
            Before = command.Before,
            ActivityTypes = command.ActivityTypes,
            IncludeActivityDetails = command.IncludeActivityDetails,
            IncludeStreams = false,
            IncludePrivateNotes = command.IncludePrivateNotes
        };
        var document = calculator.Create(createdAtUtc, request, activities);
        var files = writers.Select(writer => new ExportFile(
            exportId,
            writer.Format,
            $"strava-export-{createdAtUtc:yyyyMMdd-HHmmss}.{writer.FileExtension}",
            writer.ContentType,
            writer.Write(document),
            createdAtUtc)).ToArray();

        await store.SaveAsync(new ExportJob(exportId, athlete.Id, request, createdAtUtc), files, cancellationToken);
        foreach (var fileSink in fileSinks)
        {
            await fileSink.SaveAsync(files, cancellationToken);
        }

        var downloads = files.ToDictionary(
            x => x.Format.ToString().ToLowerInvariant(),
            x => $"/api/exports/{exportId}/download?format={x.Format.ToString().ToLowerInvariant()}");

        return new ExportResult(exportId, createdAtUtc, downloads);
    }

    public async Task<ExportDownload> DownloadExportAsync(Guid exportId, ExportFormat format, CancellationToken cancellationToken) =>
        await store.GetFileAsync(exportId, format, cancellationToken)
        ?? throw new KeyNotFoundException("Export file was not found.");

    public Task<IReadOnlyCollection<ExportJobSummary>> GetExportsAsync(CancellationToken cancellationToken) =>
        store.ListAsync(cancellationToken);

    public Task<ExportJobSummary?> GetExportAsync(Guid exportId, CancellationToken cancellationToken) =>
        store.GetAsync(exportId, cancellationToken);
}

public static class ExportJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };
}
