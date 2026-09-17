using FluentAssertions;
using TechDaily.Domain.Entities;
using Xunit;

namespace TechDaily.Tests.Domain;

public class UserHighlightTests
{
    [Fact]
    public void Update_ShouldUpdateNoteTagsAndUpdatedAt()
    {
        // Arrange
        var highlight = new UserHighlight
        {
            UserId = Guid.NewGuid(),
            DocumentChunkId = Guid.NewGuid(),
            SelectedText = "Sample excerpt",
            Note = "Old note",
            Tags = new List<string> { "old-tag" }
        };
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);

        // Act
        highlight.Update("New reflection note", new List<string> { "csharp", "dotnet" });

        // Assert
        highlight.Note.Should().Be("New reflection note");
        highlight.Tags.Should().ContainInOrder("csharp", "dotnet");
        highlight.UpdatedAt.Should().NotBeNull();
        highlight.UpdatedAt.Should().BeAfter(before);
    }

    [Fact]
    public void Update_WhenTagsNull_ShouldKeepExistingTags()
    {
        // Arrange
        var highlight = new UserHighlight
        {
            UserId = Guid.NewGuid(),
            DocumentChunkId = Guid.NewGuid(),
            SelectedText = "Sample excerpt",
            Note = "Initial note",
            Tags = new List<string> { "preserved-tag" }
        };

        // Act
        highlight.Update("Updated note only", null);

        // Assert
        highlight.Note.Should().Be("Updated note only");
        highlight.Tags.Should().ContainSingle().Which.Should().Be("preserved-tag");
        highlight.UpdatedAt.Should().NotBeNull();
    }
}
