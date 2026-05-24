using FluentValidation;
using StravaExporter.Application.Exports;
using Xunit;

namespace StravaExporter.Application.Tests;

public sealed class CreateExportCommandValidatorTests
{
    [Fact]
    public async Task Validator_rejects_streams_for_mvp()
    {
        var validator = new CreateExportCommandValidator();
        var command = new CreateExportCommand { IncludeStreams = true };

        await Assert.ThrowsAsync<ValidationException>(() => validator.ValidateAndThrowAsync(command));
    }
}
