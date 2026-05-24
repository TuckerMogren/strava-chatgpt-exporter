namespace StravaExporter.Domain.Exports;

public class ExportRequest
{
    public DateOnly? After { get; init; }
    public DateOnly? Before { get; init; }
    public IReadOnlyCollection<string> ActivityTypes { get; init; } = [];
    public bool IncludeActivityDetails { get; init; }
    public bool IncludeStreams { get; init; }
    public bool IncludePrivateNotes { get; init; }
}
