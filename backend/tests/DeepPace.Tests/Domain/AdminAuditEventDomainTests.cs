using FluentAssertions;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;
using Xunit;

namespace DeepPace.Tests.Domain;

public class AdminAuditEventDomainTests
{
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly Guid _targetId = Guid.NewGuid();

    [Fact]
    public void Create_ValidParameters_ShouldConstructImmutableRecord()
    {
        var now = DateTimeOffset.UtcNow;
        var evt = AdminAuditEvent.Create(
            actorUserId: _actorId,
            action: AdminAuditActions.UserSuspend,
            targetType: "User",
            targetId: _targetId.ToString(),
            outcome: AdminAuditOutcome.Succeeded,
            reason: "Account policy violation",
            errorCode: null,
            correlationId: "corr-12345",
            changedFields: new[] { "Status", "AuthenticationVersion", "AdminControlVersion" },
            statePayload: "{\"Status\":\"Suspended\",\"AuthenticationVersion\":2}",
            occurredAt: now);

        evt.Id.Should().NotBeEmpty();
        evt.OccurredAt.Should().Be(now);
        evt.ActorUserId.Should().Be(_actorId);
        evt.Action.Should().Be(AdminAuditActions.UserSuspend);
        evt.TargetType.Should().Be("User");
        evt.TargetId.Should().Be(_targetId.ToString());
        evt.Outcome.Should().Be(AdminAuditOutcome.Succeeded);
        evt.Reason.Should().Be("Account policy violation");
        evt.ErrorCode.Should().BeNull();
        evt.CorrelationId.Should().Be("corr-12345");
        evt.ChangedFields.Should().Contain("Status");
        evt.StatePayload.Should().Contain("Suspended");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_InvalidAction_ShouldThrowArgumentException(string? action)
    {
        var act = () => AdminAuditEvent.Create(
            _actorId,
            action!,
            "User",
            _targetId.ToString(),
            AdminAuditOutcome.Succeeded);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_EmptyActorId_ShouldThrowArgumentException()
    {
        var act = () => AdminAuditEvent.Create(
            Guid.Empty,
            AdminAuditActions.UserSuspend,
            "User",
            _targetId.ToString(),
            AdminAuditOutcome.Succeeded);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_OverlongReason_ShouldThrowArgumentException()
    {
        var longReason = new string('x', 501);
        var act = () => AdminAuditEvent.Create(
            _actorId,
            AdminAuditActions.UserSuspend,
            "User",
            _targetId.ToString(),
            AdminAuditOutcome.Succeeded,
            reason: longReason);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*500 characters*");
    }

    [Theory]
    [InlineData("User provided Bearer token eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...")]
    [InlineData("password: secret_password_123")]
    public void Create_SensitiveReason_ShouldThrowArgumentException(string sensitiveReason)
    {
        var act = () => AdminAuditEvent.Create(
            _actorId,
            AdminAuditActions.UserSuspend,
            "User",
            _targetId.ToString(),
            AdminAuditOutcome.Succeeded,
            reason: sensitiveReason);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*sensitive*");
    }

    [Fact]
    public void Create_UserAssignRole_ShouldConstructValidAuditEventWithSafePayload()
    {
        var prevRoleId = RoleDefaults.LearnerRoleId;
        var newRoleId = RoleDefaults.AdminRoleId;
        var statePayload = System.Text.Json.JsonSerializer.Serialize(new
        {
            PreviousRoleId = prevRoleId,
            NewRoleId = newRoleId,
            ResultingAuthenticationVersion = 2,
            ResultingAdminControlVersion = 2
        });

        var evt = AdminAuditEvent.Create(
            actorUserId: _actorId,
            action: AdminAuditActions.UserAssignRole,
            targetType: "User",
            targetId: _targetId.ToString(),
            outcome: AdminAuditOutcome.Succeeded,
            reason: "Promoting team member to administrator",
            correlationId: "corr-role-assign-1",
            changedFields: new[] { "RoleId", "AuthenticationVersion", "AdminControlVersion" },
            statePayload: statePayload);

        evt.Action.Should().Be(AdminAuditActions.UserAssignRole);
        evt.TargetType.Should().Be("User");
        evt.TargetId.Should().Be(_targetId.ToString());
        evt.ActorUserId.Should().Be(_actorId);
        evt.ChangedFields.Should().Equal("RoleId", "AuthenticationVersion", "AdminControlVersion");
        evt.StatePayload.Should().Contain(prevRoleId.ToString());
        evt.StatePayload.Should().Contain(newRoleId.ToString());
        evt.StatePayload.Should().NotContain("password");
        evt.StatePayload.Should().NotContain("hash");
    }

    [Fact]
    public void Create_UserAssignRole_RejectsEmptyActorId_PreventingFakeActorFabrication()
    {
        var act = () => AdminAuditEvent.Create(
            actorUserId: Guid.Empty,
            action: AdminAuditActions.UserAssignRole,
            targetType: "User",
            targetId: _targetId.ToString(),
            outcome: AdminAuditOutcome.Succeeded,
            reason: "Bootstrap operation");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Actor user ID cannot be empty*");
    }
}
