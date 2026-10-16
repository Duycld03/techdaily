using FluentAssertions;
using Pgvector;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;
using Xunit;

namespace DeepPace.Tests.Domain;

public class CatalogDomainTests
{
    private static CatalogDocument CreateValidReadyCatalogDocument()
    {
        var doc = new CatalogDocument
        {
            Id = Guid.NewGuid(),
            Title = "Designing Data-Intensive Applications",
            Description = "The big ideas behind reliable, scalable, and maintainable systems.",
            CategoryId = CategoryDefaults.DatabaseStorageId,
            Language = "en",
            SourceType = SourceType.PdfBook,
            AuthorOrSourceUrl = "Martin Kleppmann",
            AttributionText = "Copyright 2017 Martin Kleppmann. Used under educational permission.",
            ProcessingStatus = ProcessingStatus.Ready,
            TotalSlices = 4,
            Revision = 1
        };

        for (int i = 1; i <= 4; i++)
        {
            doc.Slices.Add(new CatalogSlice
            {
                Id = Guid.NewGuid(),
                CatalogDocumentId = doc.Id,
                SliceOrder = i,
                ChapterTitle = $"Chapter {i}",
                OriginalTextMarkdown = $"Source content for chapter {i}",
                SummaryMarkdown = $"Summary for chapter {i}",
                KeyTakeaways = new List<string> { $"Takeaway {i}.1", $"Takeaway {i}.2" },
                Language = "en",
                EstimatedReadMinutes = 5,
                IsAiFormatted = i <= 3, // Lean 3-slice ingestion: first 3 formatted, remaining readable source
                Embedding = new Vector(new float[] { 0.1f * i, 0.2f * i })
            });
        }

        return doc;
    }

    [Fact]
    public void Publish_ValidReadyDocument_ShouldSucceedAndRecordPublishData()
    {
        var doc = CreateValidReadyCatalogDocument();
        var adminId = Guid.NewGuid();

        doc.Review(adminId);
        doc.Publish(adminId);

        doc.Visibility.Should().Be(CatalogVisibility.Published);
        doc.PublishedByUserId.Should().Be(adminId);
        doc.PublishedAt.Should().NotBeNull();
        doc.ReviewedRevision.Should().Be(doc.Revision);
    }

    [Fact]
    public void Publish_WithoutPriorReview_ShouldSucceedDirectly()
    {
        var doc = CreateValidReadyCatalogDocument();
        var adminId = Guid.NewGuid();

        doc.Publish(adminId);

        doc.Visibility.Should().Be(CatalogVisibility.Published);
        doc.PublishedByUserId.Should().Be(adminId);
    }

    [Fact]
    public void Publish_AfterMetadataUpdate_ShouldSucceedDirectly()
    {
        var doc = CreateValidReadyCatalogDocument();
        var adminId = Guid.NewGuid();

        doc.UpdateMetadata("Updated Title", doc.Description, doc.CategoryId, doc.Language, doc.AuthorOrSourceUrl, doc.AttributionText);
        doc.Publish(adminId);

        doc.Visibility.Should().Be(CatalogVisibility.Published);
    }

    [Fact]
    public void Publish_WithEmptySlices_ShouldThrowInvalidOperationException()
    {
        var doc = CreateValidReadyCatalogDocument();
        doc.Slices.Clear();
        var adminId = Guid.NewGuid();
        doc.Review(adminId);

        var act = () => doc.Publish(adminId);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*slice*");
    }

    [Fact]
    public void Publish_WithEmptySliceContent_ShouldThrowInvalidOperationException()
    {
        var doc = CreateValidReadyCatalogDocument();
        doc.Slices.First().OriginalTextMarkdown = "   ";
        var adminId = Guid.NewGuid();
        doc.Review(adminId);

        var act = () => doc.Publish(adminId);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*content*");
    }

    [Fact]
    public void Publish_WithMissingInitialFormatting_ShouldThrowInvalidOperationException()
    {
        var doc = CreateValidReadyCatalogDocument();
        // Unformat slice 2
        doc.Slices.First(s => s.SliceOrder == 2).IsAiFormatted = false;
        var adminId = Guid.NewGuid();
        doc.Review(adminId);

        var act = () => doc.Publish(adminId);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*initial formatting*");
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("   ", "   ")]
    public void Publish_WithMissingAttribution_ShouldThrowInvalidOperationException(
        string attribution, string authorUrl)
    {
        var doc = CreateValidReadyCatalogDocument();
        doc.AttributionText = attribution;
        doc.AuthorOrSourceUrl = authorUrl;

        var adminId = Guid.NewGuid();
        doc.Review(adminId);

        var act = () => doc.Publish(adminId);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Hide_PublishedDocument_ShouldSucceedAndRecordHiddenData()
    {
        var doc = CreateValidReadyCatalogDocument();
        var adminId = Guid.NewGuid();
        doc.Review(adminId);
        doc.Publish(adminId);

        doc.Hide(adminId);

        doc.Visibility.Should().Be(CatalogVisibility.Hidden);
        doc.HiddenByUserId.Should().Be(adminId);
        doc.HiddenAt.Should().NotBeNull();
    }

    [Fact]
    public void UpdateMetadata_WhenPublished_ShouldThrowInvalidOperationException()
    {
        var doc = CreateValidReadyCatalogDocument();
        var adminId = Guid.NewGuid();
        doc.Review(adminId);
        doc.Publish(adminId);

        var act = () => doc.UpdateMetadata("New Title", doc.Description, doc.CategoryId, doc.Language, doc.AuthorOrSourceUrl, doc.AttributionText);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*hidden first*");
    }

    [Fact]
    public void CreateFromCatalogSnapshot_ValidPublishedDocument_ShouldCreateIndependentBookAndChunks()
    {
        var doc = CreateValidReadyCatalogDocument();
        var adminId = Guid.NewGuid();
        doc.Review(adminId);
        doc.Publish(adminId);

        var learnerId = Guid.NewGuid();
        var slug = "designing-data-intensive-applications-user-1";

        var book = DocumentBook.CreateFromCatalogSnapshot(doc, learnerId, slug);

        book.Id.Should().NotBe(doc.Id);
        book.CreatedByUserId.Should().Be(learnerId);
        book.Title.Should().Be(doc.Title);
        book.Slug.Should().Be(slug);
        book.CategoryId.Should().Be(doc.CategoryId);
        book.SourceType.Should().Be(doc.SourceType);
        book.AuthorOrSourceUrl.Should().Be(doc.AuthorOrSourceUrl);
        book.SourceCatalogDocumentId.Should().Be(doc.Id);
        book.SourceCatalogRevision.Should().Be(doc.Revision);
        book.SourceAttribution.Should().Be(doc.AttributionText);
        book.Status.Should().Be(ProcessingStatus.Ready);
        book.ProgressPercentage.Should().Be(100);
        book.TotalChunks.Should().Be(doc.TotalSlices);
        book.Chunks.Should().HaveCount(4);

        foreach (var chunk in book.Chunks)
        {
            var correspondingSlice = doc.Slices.First(s => s.SliceOrder == chunk.ChunkOrder);
            chunk.Id.Should().NotBe(correspondingSlice.Id);
            chunk.DocumentBookId.Should().Be(book.Id);
            chunk.ChapterTitle.Should().Be(correspondingSlice.ChapterTitle);
            chunk.OriginalTextMarkdown.Should().Be(correspondingSlice.OriginalTextMarkdown);
            chunk.SummaryMarkdown.Should().Be(correspondingSlice.SummaryMarkdown);
            chunk.KeyTakeaways.Should().BeEquivalentTo(correspondingSlice.KeyTakeaways);
            chunk.Language.Should().Be(correspondingSlice.Language);
            chunk.EstimatedReadMinutes.Should().Be(correspondingSlice.EstimatedReadMinutes);
            chunk.IsAiFormatted.Should().Be(correspondingSlice.IsAiFormatted);

            if (correspondingSlice.Embedding != null)
            {
                chunk.Embedding.Should().NotBeNull();
                chunk.Embedding!.ToArray().Should().BeEquivalentTo(correspondingSlice.Embedding.ToArray());
            }

            // Invariant: No personal learning records copied
            chunk.DailyDrills.Should().BeEmpty();
            chunk.InterviewQuestions.Should().BeEmpty();
            chunk.Highlights.Should().BeEmpty();
            chunk.Audios.Should().BeEmpty();
        }
    }

    [Fact]
    public void CreateFromCatalogSnapshot_UnpublishedDocument_ShouldThrowInvalidOperationException()
    {
        var doc = CreateValidReadyCatalogDocument();
        var learnerId = Guid.NewGuid();

        var act = () => DocumentBook.CreateFromCatalogSnapshot(doc, learnerId, "test-slug");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*published*");
    }
}
