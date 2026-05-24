using Serilog;
using StravaExporter.Api.Endpoints;
using StravaExporter.Api.Middleware;
using StravaExporter.Application.Exports;
using StravaExporter.Infrastructure;
using StravaExporter.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) => configuration.ReadFrom.Configuration(context.Configuration).WriteTo.Console());
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy
        .WithOrigins(builder.Configuration["Frontend:Origin"] ?? "http://localhost:5173")
        .AllowAnyHeader()
        .AllowAnyMethod());
});
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<ExportSummaryCalculator>();
builder.Services.AddScoped<IStravaExportService, StravaExportService>();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseCors();
app.UseMiddleware<ApiExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<SqliteSchemaInitializer>().InitializeAsync(CancellationToken.None);
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapStravaAuthEndpoints();
app.MapAthleteEndpoints();
app.MapActivityEndpoints();
app.MapExportEndpoints();

await app.RunAsync();
