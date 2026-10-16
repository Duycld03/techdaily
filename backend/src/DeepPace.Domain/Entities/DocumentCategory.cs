using System.Text.RegularExpressions;
using DeepPace.Domain.Common;

namespace DeepPace.Domain.Entities;

public class DocumentCategory : BaseEntity
{
    private static readonly Regex SlugRegex = new("^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled);

    public const int MaxSlugLength = 100;
    public const int MaxNameLength = 100;
    public const int MinSortOrder = 0;
    public const int MaxSortOrder = 10000;

    public string Slug { get; private set; } = string.Empty;
    public string NameEn { get; private set; } = string.Empty;
    public string NameVi { get; private set; } = string.Empty;
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true;
    public int ConcurrencyVersion { get; private set; } = 1;

    // Parameterless constructor for EF Core
    private DocumentCategory() { }

    public static DocumentCategory Create(
        string slug,
        string nameEn,
        string nameVi,
        int sortOrder = 0,
        bool isActive = true,
        Guid? id = null)
    {
        var validatedSlug = ValidateAndNormalizeSlug(slug);
        var validatedNameEn = ValidateAndNormalizeName(nameEn, nameof(nameEn));
        var validatedNameVi = ValidateAndNormalizeName(nameVi, nameof(nameVi));
        ValidateSortOrder(sortOrder);

        return new DocumentCategory
        {
            Id = id ?? Guid.NewGuid(),
            Slug = validatedSlug,
            NameEn = validatedNameEn,
            NameVi = validatedNameVi,
            SortOrder = sortOrder,
            IsActive = isActive,
            ConcurrencyVersion = 1
        };
    }

    public void UpdateMetadata(
        string nameEn,
        string nameVi,
        int sortOrder,
        bool isActive,
        int expectedVersion)
    {
        if (expectedVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(expectedVersion), "Expected version must be a positive integer.");
        }

        if (expectedVersion != ConcurrencyVersion)
        {
            throw new InvalidOperationException($"Concurrency version mismatch: expected {expectedVersion}, actual {ConcurrencyVersion}.");
        }

        var validatedNameEn = ValidateAndNormalizeName(nameEn, nameof(nameEn));
        var validatedNameVi = ValidateAndNormalizeName(nameVi, nameof(nameVi));
        ValidateSortOrder(sortOrder);

        NameEn = validatedNameEn;
        NameVi = validatedNameVi;
        SortOrder = sortOrder;
        IsActive = isActive;
        ConcurrencyVersion++;
        MarkUpdated();
    }

    public void Delete(int expectedVersion)
    {
        if (expectedVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(expectedVersion), "Expected version must be a positive integer.");
        }

        if (expectedVersion != ConcurrencyVersion)
        {
            throw new InvalidOperationException($"Concurrency version mismatch: expected {expectedVersion}, actual {ConcurrencyVersion}.");
        }

        ConcurrencyVersion++;
        SoftDelete();
    }

    private static string ValidateAndNormalizeSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new ArgumentException("Slug cannot be empty or whitespace.", nameof(slug));
        }

        var trimmed = slug.Trim();
        if (trimmed.Length is < 1 or > MaxSlugLength)
        {
            throw new ArgumentOutOfRangeException(nameof(slug), $"Slug length must be between 1 and {MaxSlugLength} characters.");
        }

        if (!SlugRegex.IsMatch(trimmed))
        {
            throw new ArgumentException("Slug must contain only trimmed lowercase ASCII letters, digits, and single separating hyphens.", nameof(slug));
        }

        return trimmed;
    }

    private static string ValidateAndNormalizeName(string name, string paramName)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Category name cannot be empty or whitespace.", paramName);
        }

        var trimmed = name.Trim();
        if (trimmed.Length is < 1 or > MaxNameLength)
        {
            throw new ArgumentOutOfRangeException(paramName, $"Category name length must be between 1 and {MaxNameLength} characters.");
        }

        return trimmed;
    }

    private static void ValidateSortOrder(int sortOrder)
    {
        if (sortOrder is < MinSortOrder or > MaxSortOrder)
        {
            throw new ArgumentOutOfRangeException(nameof(sortOrder), $"SortOrder must be between {MinSortOrder} and {MaxSortOrder}.");
        }
    }
}
