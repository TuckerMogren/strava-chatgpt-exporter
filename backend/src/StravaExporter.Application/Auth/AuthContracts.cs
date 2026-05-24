using StravaExporter.Domain.Auth;

namespace StravaExporter.Application.Auth;

public interface IStravaOAuthService
{
    string BuildAuthorizationUrl(string state);
    Task<OAuthCallbackResult> CompleteCallbackAsync(string code, string state, CancellationToken cancellationToken);
}

public interface IStravaTokenStore
{
    Task<StravaTokenSet?> GetDefaultAsync(CancellationToken cancellationToken);
    Task<StravaTokenSet?> GetByAthleteIdAsync(long athleteId, CancellationToken cancellationToken);
    Task SaveAsync(StravaTokenSet tokenSet, CancellationToken cancellationToken);
    Task DeleteAsync(long athleteId, CancellationToken cancellationToken);
}

public interface IStravaAccessTokenProvider
{
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken);
}

public sealed record OAuthCallbackResult(long AthleteId);

public sealed class StravaNotConnectedException : Exception
{
    public StravaNotConnectedException() : base("Strava is not connected.") { }
}
