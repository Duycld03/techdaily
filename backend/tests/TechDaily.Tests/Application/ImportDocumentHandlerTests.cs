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

    [Fact]
    public void SplitIntoChunks_ShouldSubdivideLongMonolithicDocumentWithoutHeadings_IntoDigestibleSlices()
    {
        // Arrange: Monolithic 4,000-word article without # headings, containing 20 paragraphs and a code block
        var paragraphs = new List<string>();
        for (int p = 1; p <= 20; p++)
        {
            if (p == 10)
            {
                // Code block inside paragraph 10
                paragraphs.Add(@"This paragraph contains an important production snippet for database pooling:
```csharp
public class ConnectionPoolManager
{
    private readonly ConcurrentBag<DbConnection> _pool = new();
    public DbConnection Acquire() => _pool.TryTake(out var conn) ? conn : CreateNew();
}
```
Followed by conclusion on pool recycling and connection exhaustion prevention in high-load scenarios.");
            }
            else
            {
                // Generate a paragraph with ~200 words
                var words = Enumerable.Range(1, 200)
                    .Select(i => $"word{i}_{p}")
                    .ToArray();
                paragraphs.Add(string.Join(" ", words));
            }
        }

        var monolithicMarkdown = string.Join("\n\n", paragraphs);

        // Act
        var splitMethod = typeof(ImportDocumentHandler).GetMethod(
            "SplitIntoChunks",
            BindingFlags.NonPublic | BindingFlags.Static
        );
        var chunks = (List<string>)splitMethod!.Invoke(null, new object[] { monolithicMarkdown })!;

        // Assert: Sliced into 3-4 chunks, each <= 1500 words
        chunks.Count.Should().BeInRange(3, 4);

        for (int i = 0; i < chunks.Count; i++)
        {
            var wordCount = chunks[i].Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
            wordCount.Should().BeLessThanOrEqualTo(1500);
            chunks[i].Should().Contain($"Part {i + 1}");
        }

        // Code block must remain intact
        var chunkWithCode = chunks.FirstOrDefault(c => c.Contains("ConnectionPoolManager"));
        chunkWithCode.Should().NotBeNull();
        chunkWithCode!.Should().Contain("```csharp");
        chunkWithCode.Should().Contain("Acquire() => _pool.TryTake(out var conn)");
    }

    [Fact]
    public void SplitIntoChunks_ShouldSubdivideLongSectionWithHeading_PreservingHeadingTitleInParts()
    {
        // Arrange: Monolithic section with an initial heading exceeding 1500 words
        var paragraphs = new List<string> { "# Comprehensive Guide to Distributed Locks" };
        for (int p = 1; p <= 12; p++)
        {
            var words = Enumerable.Range(1, 200)
                .Select(i => $"distributed_term_{i}_{p}")
                .ToArray();
            paragraphs.Add(string.Join(" ", words));
        }

        var longMarkdown = string.Join("\n\n", paragraphs);

        // Act
        var splitMethod = typeof(ImportDocumentHandler).GetMethod(
            "SplitIntoChunks",
            BindingFlags.NonPublic | BindingFlags.Static
        );
        var chunks = (List<string>)splitMethod!.Invoke(null, new object[] { longMarkdown })!;

        // Assert: Sliced into parts titled "# Comprehensive Guide to Distributed Locks (Part X)"
        chunks.Count.Should().BeGreaterThan(1);
        chunks[0].Should().StartWith("# Comprehensive Guide to Distributed Locks (Part 1)");
        chunks[1].Should().StartWith("# Comprehensive Guide to Distributed Locks (Part 2)");
    }
}
