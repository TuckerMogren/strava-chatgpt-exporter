using Microsoft.Extensions.Configuration;
using StravaExporter.Application.Exports;

namespace StravaExporter.Infrastructure.Exports;

public sealed class FileSystemExportFileSink(IConfiguration configuration) : IExportFileSink
{
    public async Task SaveAsync(IReadOnlyCollection<ExportFile> files, CancellationToken cancellationToken)
    {
        var directory = configuration["Exports:OutputDirectory"];
        if (string.IsNullOrWhiteSpace(directory))
        {
            return;
        }

        Directory.CreateDirectory(directory);
        foreach (var file in files)
        {
            var path = Path.Combine(directory, $"{file.ExportId}-{file.FileName}");
            await File.WriteAllBytesAsync(path, file.Content, cancellationToken);
        }
    }
}
