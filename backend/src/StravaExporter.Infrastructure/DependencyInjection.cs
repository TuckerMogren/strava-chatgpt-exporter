using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using StravaExporter.Application.Activities;
using StravaExporter.Application.Auth;
using StravaExporter.Application.Exports;
using StravaExporter.Infrastructure.Data;
using StravaExporter.Infrastructure.Exports;
using StravaExporter.Infrastructure.Strava;
using StravaExporter.Infrastructure.Strava.Client;

namespace StravaExporter.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<StravaOptions>(configuration.GetSection(StravaOptions.SectionName));
        services.AddSingleton<SqliteConnectionFactory>();
        services.AddSingleton<SqliteSchemaInitializer>();
        services.AddSingleton<IStravaTokenStore, SqliteStravaTokenStore>();
        services.AddSingleton<IExportJobStore, SqliteExportJobStore>();
        services.AddSingleton<IExportWriter, JsonExportWriter>();
        services.AddSingleton<IExportWriter, CsvExportWriter>();
        services.AddSingleton<IExportWriter, MarkdownExportWriter>();
        services.AddScoped<IStravaOAuthService, StravaOAuthService>();
        services.AddScoped<IStravaAccessTokenProvider, StravaAccessTokenProvider>();
        services.AddScoped<IStravaService, StravaService>();

        var stravaSection = configuration.GetSection(StravaOptions.SectionName);
        var apiBaseUrl = stravaSection["ApiBaseUrl"] ?? "https://www.strava.com/api/v3";
        var tokenUrl = stravaSection["TokenUrl"] ?? "https://www.strava.com/oauth/token";
        var tokenBaseUrl = new Uri(tokenUrl).GetLeftPart(UriPartial.Authority);

        services.AddRefitClient<IStravaApiClient>().ConfigureHttpClient(client => client.BaseAddress = new Uri(apiBaseUrl));
        services.AddRefitClient<IStravaOAuthClient>().ConfigureHttpClient(client => client.BaseAddress = new Uri(tokenBaseUrl));
        return services;
    }
}
