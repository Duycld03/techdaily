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
}
