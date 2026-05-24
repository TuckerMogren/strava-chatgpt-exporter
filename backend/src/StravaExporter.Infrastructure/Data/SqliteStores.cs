using System.Data;
using System.Globalization;
using System.Text.Json;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using StravaExporter.Application.Auth;
using StravaExporter.Application.Exports;
using StravaExporter.Domain.Auth;
using StravaExporter.Domain.Exports;

namespace StravaExporter.Infrastructure.Data;

public sealed class SqliteConnectionFactory(IConfiguration configuration)
{
    public IDbConnection Create()
    {
        var connectionString = configuration.GetConnectionString("AppDb") ?? "Data Source=strava-exporter.db";
        return new SqliteConnection(connectionString);
    }
}

public sealed class SqliteSchemaInitializer(SqliteConnectionFactory factory)
{
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        using var connection = factory.Create();
        await connection.ExecuteAsync(new CommandDefinition("""
            CREATE TABLE IF NOT EXISTS StravaTokens (
                AthleteId INTEGER PRIMARY KEY,
                AccessToken TEXT NOT NULL,
                RefreshToken TEXT NOT NULL,
                ExpiresAtUtc TEXT NOT NULL,
                CreatedAtUtc TEXT NOT NULL,
                UpdatedAtUtc TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS ExportJobs (
                Id TEXT PRIMARY KEY,
                AthleteId INTEGER NOT NULL,
                Format TEXT NOT NULL,
                RequestJson TEXT NOT NULL,
                CreatedAtUtc TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS ExportFiles (
                ExportId TEXT NOT NULL,
                Format TEXT NOT NULL,
                FileName TEXT NOT NULL,
                ContentType TEXT NOT NULL,
                Content BLOB NOT NULL,
                CreatedAtUtc TEXT NOT NULL,
                PRIMARY KEY (ExportId, Format)
            );
            """, cancellationToken: cancellationToken));
    }
}

public sealed class SqliteStravaTokenStore(SqliteConnectionFactory factory) : IStravaTokenStore
{
    public async Task<StravaTokenSet?> GetDefaultAsync(CancellationToken cancellationToken)
    {
        using var connection = factory.Create();
        var row = await connection.QuerySingleOrDefaultAsync<StravaTokenRow>(new CommandDefinition("""
            SELECT AthleteId, AccessToken, RefreshToken, ExpiresAtUtc
            FROM StravaTokens
            ORDER BY UpdatedAtUtc DESC
            LIMIT 1
            """, cancellationToken: cancellationToken));
        return row is null ? null : ToTokenSet(row);
    }

    public async Task<StravaTokenSet?> GetByAthleteIdAsync(long athleteId, CancellationToken cancellationToken)
    {
        using var connection = factory.Create();
        var row = await connection.QuerySingleOrDefaultAsync<StravaTokenRow>(new CommandDefinition("""
            SELECT AthleteId, AccessToken, RefreshToken, ExpiresAtUtc
            FROM StravaTokens
            WHERE AthleteId = @athleteId
            """, new { athleteId }, cancellationToken: cancellationToken));
        return row is null ? null : ToTokenSet(row);
    }

    public async Task SaveAsync(StravaTokenSet tokenSet, CancellationToken cancellationToken)
    {
        using var connection = factory.Create();
        var now = DateTimeOffset.UtcNow;
        await connection.ExecuteAsync(new CommandDefinition("""
            INSERT INTO StravaTokens (AthleteId, AccessToken, RefreshToken, ExpiresAtUtc, CreatedAtUtc, UpdatedAtUtc)
            VALUES (@AthleteId, @AccessToken, @RefreshToken, @ExpiresAtUtc, @Now, @Now)
            ON CONFLICT(AthleteId) DO UPDATE SET
                AccessToken = excluded.AccessToken,
                RefreshToken = excluded.RefreshToken,
                ExpiresAtUtc = excluded.ExpiresAtUtc,
                UpdatedAtUtc = excluded.UpdatedAtUtc
            """, new
        {
            tokenSet.AthleteId,
            tokenSet.AccessToken,
            tokenSet.RefreshToken,
            ExpiresAtUtc = tokenSet.ExpiresAtUtc.ToString("O"),
            Now = now.ToString("O")
        }, cancellationToken: cancellationToken));
    }

    public async Task DeleteAsync(long athleteId, CancellationToken cancellationToken)
    {
        using var connection = factory.Create();
        await connection.ExecuteAsync(new CommandDefinition(
            "DELETE FROM StravaTokens WHERE AthleteId = @athleteId",
            new { athleteId },
            cancellationToken: cancellationToken));
    }

    private static StravaTokenSet ToTokenSet(StravaTokenRow row) =>
        new()
        {
            AthleteId = row.AthleteId,
            AccessToken = row.AccessToken,
            RefreshToken = row.RefreshToken,
            ExpiresAtUtc = DateTimeOffset.Parse(row.ExpiresAtUtc, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal)
        };

    private sealed record StravaTokenRow(long AthleteId, string AccessToken, string RefreshToken, string ExpiresAtUtc);
}

public sealed class SqliteExportJobStore(SqliteConnectionFactory factory) : IExportJobStore
{
    public async Task SaveAsync(ExportJob job, IReadOnlyCollection<ExportFile> files, CancellationToken cancellationToken)
    {
        using var connection = factory.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        var requestJson = JsonSerializer.Serialize(job.Request, ExportJson.Options);

        await connection.ExecuteAsync(new CommandDefinition("""
            INSERT INTO ExportJobs (Id, AthleteId, Format, RequestJson, CreatedAtUtc)
            VALUES (@Id, @AthleteId, @Format, @RequestJson, @CreatedAtUtc)
            """, new
        {
            Id = job.Id.ToString(),
            job.AthleteId,
            Format = "all",
            RequestJson = requestJson,
            CreatedAtUtc = job.CreatedAtUtc.ToString("O")
        }, transaction, cancellationToken: cancellationToken));

        foreach (var file in files)
        {
            await connection.ExecuteAsync(new CommandDefinition("""
                INSERT INTO ExportFiles (ExportId, Format, FileName, ContentType, Content, CreatedAtUtc)
                VALUES (@ExportId, @Format, @FileName, @ContentType, @Content, @CreatedAtUtc)
                """, new
            {
                ExportId = file.ExportId.ToString(),
                Format = file.Format.ToString().ToLowerInvariant(),
                file.FileName,
                file.ContentType,
                file.Content,
                CreatedAtUtc = file.CreatedAtUtc.ToString("O")
            }, transaction, cancellationToken: cancellationToken));
        }

        transaction.Commit();
    }

    public async Task<IReadOnlyCollection<ExportJobSummary>> ListAsync(CancellationToken cancellationToken)
    {
        using var connection = factory.Create();
        var rows = await connection.QueryAsync<ExportJobRow>(new CommandDefinition("""
            SELECT Id, AthleteId, RequestJson, CreatedAtUtc
            FROM ExportJobs
            ORDER BY CreatedAtUtc DESC
            """, cancellationToken: cancellationToken));
        return rows.Select(ToSummary).ToArray();
    }

    public async Task<ExportJobSummary?> GetAsync(Guid exportId, CancellationToken cancellationToken)
    {
        using var connection = factory.Create();
        var row = await connection.QuerySingleOrDefaultAsync<ExportJobRow>(new CommandDefinition("""
            SELECT Id, AthleteId, RequestJson, CreatedAtUtc
            FROM ExportJobs
            WHERE Id = @Id
            """, new { Id = exportId.ToString() }, cancellationToken: cancellationToken));
        return row is null ? null : ToSummary(row);
    }

    public async Task<ExportDownload?> GetFileAsync(Guid exportId, ExportFormat format, CancellationToken cancellationToken)
    {
        using var connection = factory.Create();
        return await connection.QuerySingleOrDefaultAsync<ExportDownload>(new CommandDefinition("""
            SELECT FileName, ContentType, Content
            FROM ExportFiles
            WHERE ExportId = @ExportId AND Format = @Format
            """, new
        {
            ExportId = exportId.ToString(),
            Format = format.ToString().ToLowerInvariant()
        }, cancellationToken: cancellationToken));
    }

    private static ExportJobSummary ToSummary(ExportJobRow row) =>
        new(
            Guid.Parse(row.Id),
            row.AthleteId,
            JsonSerializer.Deserialize<ExportRequest>(row.RequestJson, ExportJson.Options) ?? new ExportRequest(),
            DateTimeOffset.Parse(row.CreatedAtUtc));

    private sealed record ExportJobRow(string Id, long AthleteId, string RequestJson, string CreatedAtUtc);
}
