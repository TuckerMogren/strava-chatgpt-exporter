using System.Text;
using StravaExporter.Application.Activities;
using StravaExporter.Application.Exports;
using StravaExporter.Domain.Exports;
using StravaExporter.Infrastructure.Exports;
using Xunit;

namespace StravaExporter.Infrastructure.Tests;

public sealed class ExportWriterTests
{
    private static readonly ExportDocument Document = new(
        DateTimeOffset.UnixEpoch,
        new ExportRequest { ActivityTypes = ["Run"] },
        new ExportTotals(1, 5, 2400, 480, 150),
        [
            new ActivitySummary(1, "Morning, Run", "Run", DateTimeOffset.UnixEpoch, 8046.72, 5, 2400, 2500, 480, 150, 170, 100, null, null)
        ]);

    [Fact]
    public void Csv_writer_escapes_commas()
    {
        var csv = Encoding.UTF8.GetString(new CsvExportWriter().Write(Document));
        Assert.Contains("\"Morning, Run\"", csv, StringComparison.Ordinal);
    }

    [Fact]
    public void Markdown_writer_includes_prompt()
    {
        var markdown = Encoding.UTF8.GetString(new MarkdownExportWriter().Write(Document));
        Assert.Contains("## Prompt for ChatGPT", markdown, StringComparison.Ordinal);
    }

    [Fact]
    public void Json_writer_includes_totals()
    {
        var json = Encoding.UTF8.GetString(new JsonExportWriter().Write(Document));
        Assert.Contains("totalDistanceMiles", json, StringComparison.Ordinal);
    }
}
