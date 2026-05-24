using System.Globalization;
using System.Text;
using System.Text.Json;
using StravaExporter.Application.Exports;
using StravaExporter.Domain.Exports;

namespace StravaExporter.Infrastructure.Exports;

public sealed class JsonExportWriter : IExportWriter
{
    public ExportFormat Format => ExportFormat.Json;
    public string ContentType => "application/json";
    public string FileExtension => "json";
    public byte[] Write(ExportDocument document) => JsonSerializer.SerializeToUtf8Bytes(document, ExportJson.Options);
}

public sealed class CsvExportWriter : IExportWriter
{
    public ExportFormat Format => ExportFormat.Csv;
    public string ContentType => "text/csv";
    public string FileExtension => "csv";

    public byte[] Write(ExportDocument document)
    {
        var builder = new StringBuilder();
        builder.AppendLine("ActivityId,Name,Type,StartDateLocal,DistanceMiles,MovingTimeSeconds,ElapsedTimeSeconds,AveragePaceSecondsPerMile,AverageHeartRate,MaxHeartRate,ElevationGainFeet,AverageSpeedMetersPerSecond,MaxSpeedMetersPerSecond");
        foreach (var activity in document.Activities)
        {
            builder.AppendLine(string.Join(",", [
                activity.Id.ToString(CultureInfo.InvariantCulture),
                Escape(activity.Name),
                Escape(activity.Type),
                activity.StartDateLocal.ToString("O", CultureInfo.InvariantCulture),
                Number(activity.DistanceMiles),
                activity.MovingTimeSeconds.ToString(CultureInfo.InvariantCulture),
                activity.ElapsedTimeSeconds.ToString(CultureInfo.InvariantCulture),
                Number(activity.AveragePaceSecondsPerMile),
                Number(activity.AverageHeartRate),
                Number(activity.MaxHeartRate),
                Number(activity.ElevationGainFeet),
                Number(activity.AverageSpeedMetersPerSecond),
                Number(activity.MaxSpeedMetersPerSecond)
            ]));
        }
        return Encoding.UTF8.GetBytes(builder.ToString());
    }

    private static string Number(double? value) => value?.ToString("0.###", CultureInfo.InvariantCulture) ?? "";

    private static string Escape(string value)
    {
        if (!value.Contains(',') && !value.Contains('"') && !value.Contains('\n'))
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }
}

public sealed class MarkdownExportWriter : IExportWriter
{
    public ExportFormat Format => ExportFormat.Markdown;
    public string ContentType => "text/markdown";
    public string FileExtension => "md";

    public byte[] Write(ExportDocument document)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Strava Export Summary");
        builder.AppendLine();
        builder.AppendLine($"Period: {document.Filters.After?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "all"} to {document.Filters.Before?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "all"}  ");
        builder.AppendLine($"Activity types: {(document.Filters.ActivityTypes.Count == 0 ? "All" : string.Join(", ", document.Filters.ActivityTypes))}");
        builder.AppendLine();
        builder.AppendLine("## Totals");
        builder.AppendLine();
        builder.AppendLine($"- Activities: {document.Totals.ActivityCount}");
        builder.AppendLine($"- Total distance: {document.Totals.TotalDistanceMiles:0.##} miles");
        builder.AppendLine($"- Total moving time: {FormatDuration(document.Totals.TotalMovingTimeSeconds)}");
        builder.AppendLine($"- Average pace: {FormatPace(document.Totals.AveragePaceSecondsPerMile)}");
        builder.AppendLine($"- Average heart rate: {(document.Totals.AverageHeartRate is null ? "n/a" : $"{document.Totals.AverageHeartRate:0} bpm")}");
        builder.AppendLine();
        builder.AppendLine("## Activity Table");
        builder.AppendLine();
        builder.AppendLine("| Date | Name | Distance | Pace | Avg HR | Elevation |");
        builder.AppendLine("|---|---:|---:|---:|---:|---:|");
        foreach (var activity in document.Activities)
        {
            builder.AppendLine($"| {activity.StartDateLocal:yyyy-MM-dd} | {EscapeCell(activity.Name)} | {activity.DistanceMiles:0.00} mi | {FormatPace(activity.AveragePaceSecondsPerMile)} | {(activity.AverageHeartRate is null ? "" : activity.AverageHeartRate.Value.ToString("0", CultureInfo.InvariantCulture))} | {(activity.ElevationGainFeet is null ? "" : $"{activity.ElevationGainFeet:0} ft")} |");
        }
        builder.AppendLine();
        builder.AppendLine("## Prompt for ChatGPT");
        builder.AppendLine();
        builder.AppendLine("Analyze this Strava running data.");
        builder.AppendLine();
        builder.AppendLine("Focus on:");
        builder.AppendLine("1. Weekly mileage trend");
        builder.AppendLine("2. Pace trend");
        builder.AppendLine("3. Heart-rate drift");
        builder.AppendLine("4. Possible overtraining signs");
        builder.AppendLine("5. Long-run progression");
        builder.AppendLine("6. Easy-run consistency");
        builder.AppendLine("7. Suggestions for the next 4 weeks");
        builder.AppendLine();
        builder.AppendLine("Constraints:");
        builder.AppendLine("- Do not assume missing data.");
        builder.AppendLine("- Treat heart-rate data as approximate.");
        builder.AppendLine("- Call out outliers separately.");
        builder.AppendLine("- Give practical recommendations.");
        return Encoding.UTF8.GetBytes(builder.ToString());
    }

    private static string EscapeCell(string value) => value.Replace("|", "\\|", StringComparison.Ordinal);

    private static string FormatDuration(int seconds)
    {
        var span = TimeSpan.FromSeconds(seconds);
        return $"{(int)span.TotalHours}h {span.Minutes}m";
    }

    private static string FormatPace(double? seconds)
    {
        if (seconds is null)
        {
            return "n/a";
        }

        var span = TimeSpan.FromSeconds(seconds.Value);
        return $"{(int)span.TotalMinutes}:{span.Seconds:00}/mi";
    }
}
