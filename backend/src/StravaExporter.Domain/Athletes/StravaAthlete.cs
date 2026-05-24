namespace StravaExporter.Domain.Athletes;

public sealed class StravaAthlete
{
    public required long Id { get; init; }
    public string? Username { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
}
