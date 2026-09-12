using System.Reflection;
using FluentAssertions;
using TechDaily.Application.Features.Library.ImportDocument;
using Xunit;

namespace TechDaily.Tests.Application;

public class ImportDocumentHandlerTests
{
    [Fact]
    public void SplitIntoChunks_ShouldNotSplitInsideCodeFenceWithHashComments()
    {
        // Arrange
        var markdown = @"# Chapter 1: Introduction

Here is some explanation text.

```bash
# First configure endpoint
dotnet add package Microsoft.AspNetCore.OpenApi
# Run the application
dotnet run
```

This is concluding text after the code block.

# Chapter 2: Next Section

Content for chapter 2.";

        // Act
        var splitMethod = typeof(ImportDocumentHandler).GetMethod(
            "SplitIntoChunks",
            BindingFlags.NonPublic | BindingFlags.Static
        );

        var chunks = (List<string>)splitMethod!.Invoke(null, new object[] { markdown })!;

        // Assert: Should have 2 chunks, not 4 chunks
        chunks.Should().HaveCount(2);
        chunks[0].Should().Contain("Chapter 1: Introduction");
        chunks[0].Should().Contain("# First configure endpoint");
        chunks[0].Should().Contain("# Run the application");
        chunks[0].Should().Contain("```bash");
        chunks[1].Should().Contain("Chapter 2: Next Section");
    }

    [Fact]
    public void SplitIntoChunks_ShouldSplitOnTopLevelHeadingsOutsideCode()
    {
        // Arrange
        var markdown = @"## Section 1: Overview
Text in overview.

### Details A
Details text.

## Section 2: Architecture
Architecture text.";

        // Act
        var splitMethod = typeof(ImportDocumentHandler).GetMethod(
            "SplitIntoChunks",
            BindingFlags.NonPublic | BindingFlags.Static
        );

        var chunks = (List<string>)splitMethod!.Invoke(null, new object[] { markdown })!;

        // Assert
        chunks.Should().HaveCount(3);
        chunks[0].Should().Contain("Section 1: Overview");
        chunks[1].Should().Contain("Details A");
        chunks[2].Should().Contain("Section 2: Architecture");
    }
}
