using FluentAssertions;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;
using Xunit;

namespace DeepPace.Tests.Domain;

public class UserAccountDomainTests
{
    private readonly Guid _actorId = Guid.NewGuid();

    [Fact]
    public void InitialState_ShouldBeActiveWithVersionOne()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "learner@example.com",
            Name = "Learner"
        };

        user.Status.Should().Be(AccountStatus.Active);
        user.AuthenticationVersion.Should().Be(1);
        user.AdminControlVersion.Should().Be(1);
        user.SuspendedByUserId.Should().BeNull();
        user.SuspendedAt.Should().BeNull();
        user.SuspensionReason.Should().BeNull();
    }

    [Fact]
    public void Suspend_ValidTransition_ShouldUpdateStatusAndMonotonicallyAdvanceVersions()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "learner@example.com",
            Name = "Learner",
            TargetRole = "Staff Engineer",
            DailyGoalMinutes = 25
        };

        var now = DateTimeOffset.UtcNow;
        user.Suspend(_actorId, "Policy violation investigation", now);

        user.Status.Should().Be(AccountStatus.Suspended);
        user.SuspendedByUserId.Should().Be(_actorId);
        user.SuspendedAt.Should().Be(now);
        user.SuspensionReason.Should().Be("Policy violation investigation");
        user.AuthenticationVersion.Should().Be(2);
        user.AdminControlVersion.Should().Be(2);
        // Preserves learning preferences and metadata
        user.TargetRole.Should().Be("Staff Engineer");
        user.DailyGoalMinutes.Should().Be(25);
    }

    [Fact]
    public void Reactivate_ValidTransition_ShouldRestoreActiveAndAdvanceControlVersionWithoutRestoringAuthVersion()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "learner@example.com",
            Name = "Learner"
        };

        user.Suspend(_actorId, "Suspended temporarily");
        var authVersionAfterSuspend = user.AuthenticationVersion;
        var controlVersionAfterSuspend = user.AdminControlVersion;

        user.Reactivate(_actorId, "Investigation cleared");

        user.Status.Should().Be(AccountStatus.Active);
        user.SuspendedByUserId.Should().BeNull();
        user.SuspendedAt.Should().BeNull();
        user.SuspensionReason.Should().BeNull();
        // Reactivation does NOT decrement or advance AuthenticationVersion (earlier sessions remain revoked)
        user.AuthenticationVersion.Should().Be(authVersionAfterSuspend);
        user.AdminControlVersion.Should().Be(controlVersionAfterSuspend + 1);
    }

    [Fact]
    public void RevokeAllSessions_ValidTransition_ShouldAdvanceBothVersions()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "learner@example.com",
            Name = "Learner"
        };

        user.RevokeAllSessions(_actorId, "Lost device report");

        user.Status.Should().Be(AccountStatus.Active);
        user.AuthenticationVersion.Should().Be(2);
        user.AdminControlVersion.Should().Be(2);
    }

    [Fact]
    public void RevokeAllSessions_SelfTarget_ShouldBePermitted()
    {
        var adminId = Guid.NewGuid();
        var adminUser = new User
        {
            Id = adminId,
            Email = "admin@example.com",
            Name = "Admin User"
        };

        // Deliberate self sign-out is allowed
        var act = () => adminUser.RevokeAllSessions(adminId, "Deliberate self session sign out");
        act.Should().NotThrow();
        adminUser.AuthenticationVersion.Should().Be(2);
    }

    [Fact]
    public void Suspend_SelfTarget_ShouldThrowInvalidOperationException()
    {
        var adminId = Guid.NewGuid();
        var adminUser = new User
        {
            Id = adminId,
            Email = "admin@example.com",
            Name = "Admin User"
        };

        var act = () => adminUser.Suspend(adminId, "Accidental self suspension");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*cannot suspend their own account*");
    }

    [Fact]
    public void Suspend_AlreadySuspendedUser_ShouldThrowInvalidOperationException()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "learner@example.com",
            Name = "Learner"
        };
        user.Suspend(_actorId, "First suspension");

        var act = () => user.Suspend(_actorId, "Second suspension");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already suspended*");
    }

    [Fact]
    public void Reactivate_ActiveUser_ShouldThrowInvalidOperationException()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "learner@example.com",
            Name = "Learner"
        };

        var act = () => user.Reactivate(_actorId, "Reactivating active user");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not suspended*");
    }

    [Fact]
    public void RevokeAllSessions_SuspendedUser_ShouldThrowInvalidOperationException()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "learner@example.com",
            Name = "Learner"
        };
        user.Suspend(_actorId, "Suspended");

        var act = () => user.RevokeAllSessions(_actorId, "Revoke on suspended");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*inactive account*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Suspend_BlankReason_ShouldThrowArgumentException(string? reason)
    {
        var user = new User { Id = Guid.NewGuid(), Email = "learner@example.com", Name = "Learner" };
        var act = () => user.Suspend(_actorId, reason!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Suspend_OverlongReason_ShouldThrowArgumentException()
    {
        var user = new User { Id = Guid.NewGuid(), Email = "learner@example.com", Name = "Learner" };
        var longReason = new string('a', 501);
        var act = () => user.Suspend(_actorId, longReason);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*500 characters*");
    }

    [Fact]
    public void RefreshToken_ShouldHoldAuthenticationVersionDistinguishableFromLogout()
    {
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            FamilyId = Guid.NewGuid(),
            AuthenticationVersion = 1,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30)
        };

        token.AuthenticationVersion.Should().Be(1);
        token.RevokedAt.Should().BeNull();

        // Normal device logout marks RevokedAt without changing AuthenticationVersion
        token.RevokedAt = DateTimeOffset.UtcNow;
        token.RevokedAt.Should().NotBeNull();
        token.AuthenticationVersion.Should().Be(1);
    }

    [Fact]
    public void AssignRole_ValidTransition_ShouldAdvanceVersionsAndPreserveAccountState()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "target@example.com",
            Name = "Target User",
            PasswordHash = "hash123",
            GoogleSubjectId = "google-sub-456",
            TargetRole = "Senior Architect",
            DailyGoalMinutes = 30,
            RoleId = RoleDefaults.LearnerRoleId
        };

        var beforeAuthVersion = user.AuthenticationVersion;
        var beforeControlVersion = user.AdminControlVersion;
        var now = DateTimeOffset.UtcNow;

        user.AssignRole(_actorId, RoleDefaults.AdminRoleId, "Promotion to administrator", now);

        user.RoleId.Should().Be(RoleDefaults.AdminRoleId);
        user.AuthenticationVersion.Should().Be(beforeAuthVersion + 1);
        user.AdminControlVersion.Should().Be(beforeControlVersion + 1);
        user.UpdatedAt.Should().BeCloseTo(now, TimeSpan.FromSeconds(1));
        // Preserves account credentials, status, and learning preferences
        user.Status.Should().Be(AccountStatus.Active);
        user.PasswordHash.Should().Be("hash123");
        user.GoogleSubjectId.Should().Be("google-sub-456");
        user.TargetRole.Should().Be("Senior Architect");
        user.DailyGoalMinutes.Should().Be(30);
    }

    [Fact]
    public void AssignRole_SuspendedUser_ShouldChangeRoleAndAdvanceVersionsWhileKeepingStatusSuspended()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "suspended@example.com",
            Name = "Suspended User",
            RoleId = RoleDefaults.LearnerRoleId
        };
        var suspendTime = DateTimeOffset.UtcNow.AddDays(-1);
        user.Suspend(_actorId, "Temporary investigation", suspendTime);

        var authVersionBeforeRoleChange = user.AuthenticationVersion;
        var controlVersionBeforeRoleChange = user.AdminControlVersion;

        user.AssignRole(_actorId, RoleDefaults.AdminRoleId, "Role change while suspended");

        user.RoleId.Should().Be(RoleDefaults.AdminRoleId);
        user.Status.Should().Be(AccountStatus.Suspended);
        user.SuspendedByUserId.Should().Be(_actorId);
        user.SuspendedAt.Should().Be(suspendTime);
        user.SuspensionReason.Should().Be("Temporary investigation");
        user.AuthenticationVersion.Should().Be(authVersionBeforeRoleChange + 1);
        user.AdminControlVersion.Should().Be(controlVersionBeforeRoleChange + 1);
    }

    [Fact]
    public void AssignRole_SameRole_ShouldThrowInvalidOperationExceptionAndNotMutateVersions()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "learner@example.com",
            Name = "Learner",
            RoleId = RoleDefaults.LearnerRoleId
        };

        var act = () => user.AssignRole(_actorId, RoleDefaults.LearnerRoleId, "Assigning same role");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already has the specified role*");
        user.RoleId.Should().Be(RoleDefaults.LearnerRoleId);
        user.AuthenticationVersion.Should().Be(1);
        user.AdminControlVersion.Should().Be(1);
    }

    [Fact]
    public void AssignRole_SelfRole_ShouldThrowInvalidOperationExceptionAndNotMutateVersions()
    {
        var adminId = Guid.NewGuid();
        var user = new User
        {
            Id = adminId,
            Email = "admin@example.com",
            Name = "Admin",
            RoleId = RoleDefaults.AdminRoleId
        };

        var act = () => user.AssignRole(adminId, RoleDefaults.LearnerRoleId, "Self demotion attempt");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*cannot change their own role*");
        user.RoleId.Should().Be(RoleDefaults.AdminRoleId);
        user.AuthenticationVersion.Should().Be(1);
        user.AdminControlVersion.Should().Be(1);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void AssignRole_BlankReason_ShouldThrowArgumentException(string? reason)
    {
        var user = new User { Id = Guid.NewGuid(), Email = "user@example.com", Name = "User", RoleId = RoleDefaults.LearnerRoleId };
        var act = () => user.AssignRole(_actorId, RoleDefaults.AdminRoleId, reason!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AssignRole_OverlongReason_ShouldThrowArgumentException()
    {
        var user = new User { Id = Guid.NewGuid(), Email = "user@example.com", Name = "User", RoleId = RoleDefaults.LearnerRoleId };
        var longReason = new string('x', 501);
        var act = () => user.AssignRole(_actorId, RoleDefaults.AdminRoleId, longReason);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*500 characters*");
    }

    [Fact]
    public void AssignRole_EmptyActorOrRoleId_ShouldThrowArgumentException()
    {
        var user = new User { Id = Guid.NewGuid(), Email = "user@example.com", Name = "User", RoleId = RoleDefaults.LearnerRoleId };

        var actEmptyActor = () => user.AssignRole(Guid.Empty, RoleDefaults.AdminRoleId, "Reason");
        actEmptyActor.Should().Throw<ArgumentException>();

        var actEmptyRole = () => user.AssignRole(_actorId, Guid.Empty, "Reason");
        actEmptyRole.Should().Throw<ArgumentException>();
    }
}
