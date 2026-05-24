using Microsoft.Extensions.Configuration;
using StravaExporter.Domain.Auth;
using StravaExporter.Infrastructure.Data;
using Xunit;

namespace StravaExporter.Infrastructure.Tests;

public sealed class SqliteStravaTokenStoreTests
{
    [Fact]
    public async Task Save_and_get_default_round_trips_expiration()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");
        try
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:AppDb"] = $"Data Source={databasePath}"
                })
                .Build();
            var factory = new SqliteConnectionFactory(configuration);
            await new SqliteSchemaInitializer(factory).InitializeAsync(CancellationToken.None);
            var store = new SqliteStravaTokenStore(factory);
            var expiresAt = new DateTimeOffset(2026, 5, 25, 1, 43, 12, TimeSpan.Zero);

            await store.SaveAsync(new StravaTokenSet
            {
                AthleteId = 123,
                AccessToken = "access",
                RefreshToken = "refresh",
                ExpiresAtUtc = expiresAt
            }, CancellationToken.None);

            var loaded = await store.GetDefaultAsync(CancellationToken.None);

            Assert.NotNull(loaded);
            Assert.Equal(123, loaded.AthleteId);
            Assert.Equal(expiresAt, loaded.ExpiresAtUtc);
        }
        finally
        {
            File.Delete(databasePath);
        }
    }
}
