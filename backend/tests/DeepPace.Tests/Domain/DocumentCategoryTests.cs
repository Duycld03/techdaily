using FluentAssertions;
using DeepPace.Domain.Entities;
using Xunit;

namespace DeepPace.Tests.Domain;

public class DocumentCategoryTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldInstantiateDocumentCategory()
    {
        var id = Guid.NewGuid();
        var category = DocumentCategory.Create(
            slug: "system-design",
            nameEn: "System Design",
            nameVi: "Thiết kế hệ thống",
            sortOrder: 10,
            isActive: true,
            id: id);

        category.Id.Should().Be(id);
        category.Slug.Should().Be("system-design");
        category.NameEn.Should().Be("System Design");
        category.NameVi.Should().Be("Thiết kế hệ thống");
        category.SortOrder.Should().Be(10);
        category.IsActive.Should().BeTrue();
        category.ConcurrencyVersion.Should().Be(1);
        category.IsDeleted.Should().BeFalse();
        category.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Create_WithoutOptionalId_ShouldGenerateNewId()
    {
        var category = DocumentCategory.Create(
            slug: "frontend-web",
            nameEn: "Frontend & Web",
            nameVi: "Frontend & Web");

        category.Id.Should().NotBeEmpty();
        category.SortOrder.Should().Be(0);
        category.IsActive.Should().BeTrue();
        category.ConcurrencyVersion.Should().Be(1);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyOrNullSlug_ShouldThrowArgumentException(string? invalidSlug)
    {
        var act = () => DocumentCategory.Create(
            slug: invalidSlug!,
            nameEn: "Valid Name",
            nameVi: "Tên hợp lệ");

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("System-Design")] // Uppercase
    [InlineData("system--design")] // Double hyphen
    [InlineData("-system-design")] // Leading hyphen
    [InlineData("system-design-")] // Trailing hyphen
    [InlineData("system_design")] // Underscore not allowed
    [InlineData("system design")] // Space not allowed
    [InlineData("system.design")] // Dot not allowed
    [InlineData("hệ-thống")] // Non-ASCII not allowed
    public void Create_WithInvalidSlugPattern_ShouldThrowArgumentException(string invalidSlug)
    {
        var act = () => DocumentCategory.Create(
            slug: invalidSlug,
            nameEn: "Valid Name",
            nameVi: "Tên hợp lệ");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithSlugExceeding100Chars_ShouldThrowArgumentOutOfRangeException()
    {
        var longSlug = new string('a', 101);
        var act = () => DocumentCategory.Create(
            slug: longSlug,
            nameEn: "Valid Name",
            nameVi: "Tên hợp lệ");

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyOrNullNames_ShouldThrowArgumentException(string? invalidName)
    {
        var actEn = () => DocumentCategory.Create(
            slug: "valid-slug",
            nameEn: invalidName!,
            nameVi: "Tên hợp lệ");

        var actVi = () => DocumentCategory.Create(
            slug: "valid-slug",
            nameEn: "Valid Name",
            nameVi: invalidName!);

        actEn.Should().Throw<ArgumentException>();
        actVi.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNameExceeding100Chars_ShouldThrowArgumentOutOfRangeException()
    {
        var longName = new string('a', 101);

        var actEn = () => DocumentCategory.Create(
            slug: "valid-slug",
            nameEn: longName,
            nameVi: "Tên hợp lệ");

        var actVi = () => DocumentCategory.Create(
            slug: "valid-slug",
            nameEn: "Valid Name",
            nameVi: longName);

        actEn.Should().Throw<ArgumentOutOfRangeException>();
        actVi.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(10001)]
    public void Create_WithSortOrderOutOfBounds_ShouldThrowArgumentOutOfRangeException(int invalidOrder)
    {
        var act = () => DocumentCategory.Create(
            slug: "valid-slug",
            nameEn: "Valid Name",
            nameVi: "Tên hợp lệ",
            sortOrder: invalidOrder);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void UpdateMetadata_WithMatchingVersion_ShouldUpdatePropertiesAndIncrementVersion()
    {
        var category = DocumentCategory.Create(
            slug: "backend-runtime",
            nameEn: "Backend",
            nameVi: "Hậu đài",
            sortOrder: 1,
            isActive: true);

        category.UpdateMetadata(
            nameEn: "Backend & Runtime",
            nameVi: "Backend & Runtime",
            sortOrder: 5,
            isActive: false,
            expectedVersion: 1);

        category.NameEn.Should().Be("Backend & Runtime");
        category.NameVi.Should().Be("Backend & Runtime");
        category.SortOrder.Should().Be(5);
        category.IsActive.Should().BeFalse();
        category.ConcurrencyVersion.Should().Be(2);
        category.UpdatedAt.Should().NotBeNull();
        category.Slug.Should().Be("backend-runtime"); // Slug remains immutable
    }

    [Fact]
    public void UpdateMetadata_WithMismatchedVersion_ShouldThrowInvalidOperationException()
    {
        var category = DocumentCategory.Create(
            slug: "backend-runtime",
            nameEn: "Backend",
            nameVi: "Hậu đài",
            sortOrder: 1,
            isActive: true);

        var act = () => category.UpdateMetadata(
            nameEn: "Backend & Runtime",
            nameVi: "Backend & Runtime",
            sortOrder: 5,
            isActive: false,
            expectedVersion: 2); // Expected 2 when actual is 1

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Concurrency version mismatch*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void UpdateMetadata_WithNonPositiveVersion_ShouldThrowArgumentOutOfRangeException(int invalidVersion)
    {
        var category = DocumentCategory.Create(
            slug: "backend-runtime",
            nameEn: "Backend",
            nameVi: "Hậu đài");

        var act = () => category.UpdateMetadata(
            nameEn: "New Name",
            nameVi: "Tên mới",
            sortOrder: 1,
            isActive: true,
            expectedVersion: invalidVersion);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Delete_WithMatchingVersion_ShouldSoftDeleteAndIncrementVersion()
    {
        var category = DocumentCategory.Create(
            slug: "database-storage",
            nameEn: "Database & Storage",
            nameVi: "Cơ sở dữ liệu");

        category.Delete(expectedVersion: 1);

        category.IsDeleted.Should().BeTrue();
        category.ConcurrencyVersion.Should().Be(2);
        category.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Delete_WithMismatchedVersion_ShouldThrowInvalidOperationException()
    {
        var category = DocumentCategory.Create(
            slug: "database-storage",
            nameEn: "Database & Storage",
            nameVi: "Cơ sở dữ liệu");

        var act = () => category.Delete(expectedVersion: 99);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Concurrency version mismatch*");
    }
}
