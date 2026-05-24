using System.Net;
using System.Web;
using Microsoft.Extensions.Options;
using Refit;
using StravaExporter.Application.Activities;
using StravaExporter.Application.Auth;
using StravaExporter.Application.Common;
using StravaExporter.Domain.Activities;
using StravaExporter.Domain.Athletes;
using StravaExporter.Domain.Auth;
using StravaExporter.Infrastructure.Strava.Client;
using StravaExporter.Infrastructure.Strava.Mapping;

namespace StravaExporter.Infrastructure.Strava;

public sealed class StravaOAuthService(
    IOptions<StravaOptions> options,
    IStravaOAuthClient client,
    IStravaTokenStore tokenStore) : IStravaOAuthService
{
    private readonly StravaOptions _options = options.Value;

    public string BuildAuthorizationUrl(string state)
    {
        var builder = new UriBuilder(_options.AuthorizationBaseUrl);
        var query = HttpUtility.ParseQueryString(builder.Query);
        query["client_id"] = _options.ClientId;
        query["redirect_uri"] = _options.RedirectUri;
        query["response_type"] = "code";
        query["approval_prompt"] = "auto";
        query["scope"] = "read,activity:read_all";
        query["state"] = state;
        builder.Query = query.ToString();
        return builder.ToString();
    }

    public async Task<OAuthCallbackResult> CompleteCallbackAsync(string code, string state, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            throw new InvalidOperationException("OAuth state was missing.");
        }

        var response = await client.ExchangeCodeAsync(new Dictionary<string, string>
        {
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret,
            ["code"] = code,
            ["grant_type"] = "authorization_code"
        }, cancellationToken);

        await tokenStore.SaveAsync(new StravaTokenSet
        {
            AthleteId = response.Athlete.Id,
            AccessToken = response.AccessToken,
            RefreshToken = response.RefreshToken,
            ExpiresAtUtc = DateTimeOffset.FromUnixTimeSeconds(response.ExpiresAt)
        }, cancellationToken);

        return new OAuthCallbackResult(response.Athlete.Id);
    }
}

public sealed class StravaAccessTokenProvider(
    IStravaTokenStore tokenStore,
    IStravaOAuthClient client,
    IOptions<StravaOptions> options) : IStravaAccessTokenProvider
{
    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        var tokenSet = await tokenStore.GetDefaultAsync(cancellationToken) ?? throw new StravaNotConnectedException();
        if (!tokenSet.IsExpired(DateTimeOffset.UtcNow))
        {
            return tokenSet.AccessToken;
        }

        var refreshed = await client.RefreshTokenAsync(new Dictionary<string, string>
        {
            ["client_id"] = options.Value.ClientId,
            ["client_secret"] = options.Value.ClientSecret,
            ["refresh_token"] = tokenSet.RefreshToken,
            ["grant_type"] = "refresh_token"
        }, cancellationToken);

        var next = new StravaTokenSet
        {
            AthleteId = tokenSet.AthleteId,
            AccessToken = refreshed.AccessToken,
            RefreshToken = refreshed.RefreshToken,
            ExpiresAtUtc = DateTimeOffset.FromUnixTimeSeconds(refreshed.ExpiresAt)
        };
        await tokenStore.SaveAsync(next, cancellationToken);
        return next.AccessToken;
    }
}

public sealed class StravaService(IStravaApiClient client, IStravaAccessTokenProvider tokenProvider) : IStravaService
{
    public async Task<StravaAthlete> GetCurrentAthleteAsync(CancellationToken cancellationToken)
    {
        var token = await tokenProvider.GetAccessTokenAsync(cancellationToken);
        return (await Send(() => client.GetAthleteAsync(Bearer(token), cancellationToken))).ToDomain();
    }

    public async Task<IReadOnlyCollection<StravaActivity>> GetActivitiesAsync(GetActivitiesQuery query, CancellationToken cancellationToken)
    {
        var token = await tokenProvider.GetAccessTokenAsync(cancellationToken);
        long? after = query.After is null ? null : ToUnix(query.After.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
        long? before = query.Before is null ? null : ToUnix(query.Before.Value.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc));
        var page = 1;
        var activities = new List<StravaActivity>();

        while (true)
        {
            var pageItems = await Send(() => client.GetActivitiesAsync(Bearer(token), after, before, page, query.PageSize, cancellationToken));
            if (pageItems.Count == 0)
            {
                break;
            }

            activities.AddRange(pageItems.Select(x => x.ToDomain()));
            page++;
        }

        if (query.ActivityTypes.Count == 0)
        {
            return activities;
        }

        var allowed = query.ActivityTypes.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return activities.Where(x => allowed.Contains(x.Type)).ToArray();
    }

    public async Task<StravaActivity> GetActivityAsync(long activityId, CancellationToken cancellationToken)
    {
        var token = await tokenProvider.GetAccessTokenAsync(cancellationToken);
        return (await Send(() => client.GetActivityAsync(Bearer(token), activityId, cancellationToken))).ToDomain();
    }

    private static string Bearer(string token) => $"Bearer {token}";
    private static long ToUnix(DateTime date) => new DateTimeOffset(date).ToUnixTimeSeconds();

    private static async Task<T> Send<T>(Func<Task<T>> send)
    {
        try
        {
            return await send();
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
        {
            throw new StravaRateLimitException();
        }
    }
}
