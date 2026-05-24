namespace StravaExporter.Domain.Auth;

public sealed class StravaTokenSet
{
    public required long AthleteId { get; init; }
    public required string AccessToken { get; init; }
    public required string RefreshToken { get; init; }
    public required DateTimeOffset ExpiresAtUtc { get; init; }

    public bool IsExpired(DateTimeOffset nowUtc) => ExpiresAtUtc <= nowUtc.AddMinutes(2);
}
