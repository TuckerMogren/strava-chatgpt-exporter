namespace StravaExporter.Application.Common;

public sealed class StravaRateLimitException : Exception
{
    public DateTimeOffset? RetryAfterUtc { get; init; }

    public StravaRateLimitException(DateTimeOffset? retryAfterUtc = null)
        : base("Strava API rate limit was reached. Try again later.")
    {
        RetryAfterUtc = retryAfterUtc;
    }
}
