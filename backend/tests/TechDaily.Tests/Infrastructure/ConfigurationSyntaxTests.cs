using System;
using System.IO;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace TechDaily.Tests.Infrastructure;

public class ConfigurationSyntaxTests
{
    [Fact]
    public void AppSettingsFiles_ShouldBeValidJson_WithoutThrowingJsonReaderException()
    {
        var apiDir = GetApiDirectory();
        var files = Directory.GetFiles(apiDir, "appsettings*.json");
        files.Should().NotBeEmpty("TechDaily.Api should contain at least appsettings.json");

        foreach (var filePath in files)
        {
            var content = File.ReadAllText(filePath);
            var act = () =>
            {
                using var doc = JsonDocument.Parse(content);
            };

            act.Should().NotThrow<JsonException>(
                $"Configuration file '{Path.GetFileName(filePath)}' must parse as valid JSON without throwing JsonReaderException");
        }
    }

    private static string GetApiDirectory()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            var candidate = Path.Combine(current.FullName, "src", "TechDaily.Api");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            var candidateDirect = Path.Combine(current.FullName, "TechDaily.Api");
            if (Directory.Exists(candidateDirect))
            {
                return candidateDirect;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException($"Could not locate TechDaily.Api directory starting from {AppContext.BaseDirectory}");
    }
}
