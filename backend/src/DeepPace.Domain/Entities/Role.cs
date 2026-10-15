using System.Text.RegularExpressions;
using DeepPace.Domain.Common;

namespace DeepPace.Domain.Entities;

public static class RoleDefaults
{
    public static readonly Guid LearnerRoleId = Guid.Parse("20000000-0000-4000-8000-000000000001");
    public static readonly Guid AdminRoleId = Guid.Parse("20000000-0000-4000-8000-000000000002");

    public const string LearnerKey = "learner";
    public const string AdminKey = "admin";
}

public class Role : BaseEntity
{
    private static readonly Regex KeyRegex = new("^[a-z0-9_-]{1,64}$", RegexOptions.Compiled);

    public string Key { get; private set; } = string.Empty;
    public string NameEn { get; private set; } = string.Empty;
    public string NameVi { get; private set; } = string.Empty;
    public bool IsProtected { get; private set; }

    // Navigation properties
    public ICollection<RolePermission> Permissions { get; set; } = new List<RolePermission>();
    public ICollection<User> Users { get; set; } = new List<User>();

    // Parameterless constructor for EF Core
    private Role() { }

    public static Role Create(
        string key,
        string nameEn,
        string nameVi,
        bool isProtected = false,
        Guid? id = null)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Role key cannot be empty.", nameof(key));
        }

        var trimmedKey = key.Trim();
        if (!KeyRegex.IsMatch(trimmedKey))
        {
            throw new ArgumentException("Role key must be lowercase ASCII letters, digits, underscores, or hyphens (1-64 characters).", nameof(key));
        }

        if (string.IsNullOrWhiteSpace(nameEn))
        {
            throw new ArgumentException("English name cannot be empty.", nameof(nameEn));
        }
        var trimmedNameEn = nameEn.Trim();
        if (trimmedNameEn.Length > 100)
        {
            throw new ArgumentException("English name cannot exceed 100 characters.", nameof(nameEn));
        }

        if (string.IsNullOrWhiteSpace(nameVi))
        {
            throw new ArgumentException("Vietnamese name cannot be empty.", nameof(nameVi));
        }
        var trimmedNameVi = nameVi.Trim();
        if (trimmedNameVi.Length > 100)
        {
            throw new ArgumentException("Vietnamese name cannot exceed 100 characters.", nameof(nameVi));
        }

        return new Role
        {
            Id = id ?? Guid.NewGuid(),
            Key = trimmedKey,
            NameEn = trimmedNameEn,
            NameVi = trimmedNameVi,
            IsProtected = isProtected
        };
    }
}
