using FluentAssertions;
using DeepPace.Domain.Entities;
using Xunit;

namespace DeepPace.Tests.Domain;

public class RoleDomainTests
{
    [Fact]
    public void FixedSeedIdentities_ShouldMatchSpecification()
    {
        RoleDefaults.LearnerRoleId.Should().Be(Guid.Parse("20000000-0000-4000-8000-000000000001"));
        RoleDefaults.AdminRoleId.Should().Be(Guid.Parse("20000000-0000-4000-8000-000000000002"));
        RoleDefaults.LearnerKey.Should().Be("learner");
        RoleDefaults.AdminKey.Should().Be("admin");
    }

    [Fact]
    public void Create_ValidParameters_ShouldConstructRole()
    {
        var id = Guid.NewGuid();
        var role = Role.Create("tech-lead", "Technical Lead", "Trưởng nhóm kỹ thuật", isProtected: false, id: id);

        role.Id.Should().Be(id);
        role.Key.Should().Be("tech-lead");
        role.NameEn.Should().Be("Technical Lead");
        role.NameVi.Should().Be("Trưởng nhóm kỹ thuật");
        role.IsProtected.Should().BeFalse();
        role.IsDeleted.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("Admin")] // uppercase rejected
    [InlineData("admin role")] // space rejected
    [InlineData("admin@role")] // invalid symbol
    public void Create_InvalidKey_ShouldThrowArgumentException(string? key)
    {
        var act = () => Role.Create(key!, "Admin", "Quản trị viên");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_KeyExceeding64Characters_ShouldThrowArgumentException()
    {
        var longKey = new string('a', 65);
        var act = () => Role.Create(longKey, "Admin", "Quản trị viên");
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("", "Quản trị viên")]
    [InlineData("   ", "Quản trị viên")]
    [InlineData(null, "Quản trị viên")]
    [InlineData("Administrator", "")]
    [InlineData("Administrator", "   ")]
    [InlineData("Administrator", null)]
    public void Create_InvalidBilingualNames_ShouldThrowArgumentException(string? nameEn, string? nameVi)
    {
        var act = () => Role.Create("admin", nameEn!, nameVi!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_NamesExceeding100Characters_ShouldThrowArgumentException()
    {
        var longName = new string('x', 101);
        var actEn = () => Role.Create("admin", longName, "Quản trị viên");
        actEn.Should().Throw<ArgumentException>();

        var actVi = () => Role.Create("admin", "Admin", longName);
        actVi.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RolePermission_Create_ValidKey_ShouldSucceed()
    {
        var roleId = RoleDefaults.AdminRoleId;
        var perm = RolePermission.Create(roleId, PermissionKeys.ManageAccounts);

        perm.RoleId.Should().Be(roleId);
        perm.PermissionKey.Should().Be(PermissionKeys.ManageAccounts);
    }

    [Theory]
    [InlineData("manageaccounts")] // lowercase variant rejected
    [InlineData("MANAGE_ACCOUNTS")]
    [InlineData("*")] // wildcard rejected
    [InlineData("ManageEverything")] // unknown key rejected
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void RolePermission_Create_InvalidKey_ShouldThrowArgumentException(string? key)
    {
        var act = () => RolePermission.Create(RoleDefaults.AdminRoleId, key!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void User_InitialRoleId_ShouldDefaultToLearnerRole()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "learner@example.com",
            Name = "Learner"
        };

        user.RoleId.Should().Be(RoleDefaults.LearnerRoleId);
        // Preserves learning-goal TargetRole and defaults
        user.TargetRole.Should().Be("Deep Work & Focus Practitioner");
        user.DailyGoalMinutes.Should().Be(10);
    }
}
