using TraderPro.Domain.Common;
using TraderPro.Domain.Platform;

namespace TraderPro.UnitTests;

public sealed class PlatformFoundationTests
{
    private static readonly DateTimeOffset UtcNow =
        new(2026, 7, 29, 4, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Platform_records_are_created_with_uuid_version_7_ids()
    {
        var workspaceId = Uuid7.NewGuid();
        var companyId = Uuid7.NewGuid();
        var aggregateId = Uuid7.NewGuid();
        var records = new Guid[]
        {
            Workspace.Create("ws", "Workspace", UtcNow).Id,
            Company.Create(
                workspaceId,
                "co",
                "Company",
                null,
                null,
                UtcNow).Id,
            Branch.Create(
                workspaceId,
                companyId,
                "br",
                "Branch",
                true,
                UtcNow).Id,
            PlatformUser.Create(
                workspaceId,
                "owner",
                "Owner",
                UtcNow).Id,
            Device.Create(
                workspaceId,
                "installation",
                "Phone",
                "android",
                UtcNow).Id,
            IdempotencyRecord.Create(
                workspaceId,
                "key",
                "Command",
                "request-hash",
                UtcNow).Id,
            OutboxMessage.Create(
                workspaceId,
                "Event",
                1,
                "Aggregate",
                aggregateId,
                1,
                "{}",
                "correlation",
                UtcNow).Id,
            AuditEvent.Create(
                workspaceId,
                null,
                null,
                null,
                null,
                "Created",
                "Aggregate",
                aggregateId,
                null,
                null,
                null,
                "{}",
                "correlation",
                UtcNow).Id,
        };

        Assert.All(records, id => Assert.Equal(7, id.Version));
    }

    [Fact]
    public void Entity_construction_rejects_non_utc_timestamps()
    {
        var localTime = new DateTimeOffset(
            2026,
            7,
            29,
            9,
            30,
            0,
            TimeSpan.FromHours(5.5));

        var exception = Assert.Throws<ArgumentException>(
            () => Workspace.Create("ws", "Workspace", localTime));

        Assert.Contains("zero UTC offset", exception.Message);
    }

    [Fact]
    public void Entity_construction_enforces_required_values()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => Company.Create(
                Uuid7.NewGuid(),
                " ",
                "Company",
                null,
                null,
                UtcNow));

        Assert.Equal("code", exception.ParamName);
    }

    [Fact]
    public void Idempotency_record_has_a_controlled_terminal_transition()
    {
        var record = IdempotencyRecord.Create(
            Uuid7.NewGuid(),
            "key",
            "Command",
            "hash",
            UtcNow);
        var completedAt = UtcNow.AddMinutes(1);

        record.Complete("""{"result":"ok"}""", 201, completedAt);

        Assert.Equal(IdempotencyRecordStatus.Completed, record.Status);
        Assert.Equal(201, record.ResultStatusCode);
        Assert.Equal(completedAt, record.CompletedAtUtc);
        Assert.Throws<InvalidOperationException>(
            () => record.Fail(null, completedAt.AddMinutes(1)));
    }

    [Fact]
    public void Invalid_json_is_rejected_before_persistence()
    {
        Assert.Throws<ArgumentException>(
            () => OutboxMessage.Create(
                Uuid7.NewGuid(),
                "Event",
                1,
                "Aggregate",
                Uuid7.NewGuid(),
                1,
                "{not-json}",
                "correlation",
                UtcNow));
    }

    [Fact]
    public void Outbox_lifecycle_tracks_attempts_without_becoming_append_only()
    {
        var message = OutboxMessage.Create(
            Uuid7.NewGuid(),
            "Event",
            1,
            "Aggregate",
            Uuid7.NewGuid(),
            1,
            "{}",
            "correlation",
            UtcNow);

        message.StartProcessing();
        message.RecordFailure("temporary", UtcNow.AddMinutes(5));
        message.StartProcessing();
        message.MarkProcessed(UtcNow.AddMinutes(6));

        Assert.Equal(OutboxMessageStatus.Processed, message.Status);
        Assert.Equal(2, message.AttemptCount);
        Assert.Equal(UtcNow.AddMinutes(6), message.ProcessedAtUtc);
    }

    [Theory]
    [InlineData(BranchStatus.Inactive)]
    [InlineData(BranchStatus.Archived)]
    public void Default_branch_cannot_leave_active_status(
        BranchStatus requestedStatus)
    {
        var branch = Branch.Create(
            Uuid7.NewGuid(),
            Uuid7.NewGuid(),
            "default",
            "Default Branch",
            true,
            UtcNow);

        Assert.Throws<InvalidOperationException>(
            () => branch.SetStatus(requestedStatus));
        Assert.True(branch.IsDefault);
        Assert.Equal(BranchStatus.Active, branch.Status);
    }

    [Fact]
    public void Inactive_branch_cannot_become_default()
    {
        var branch = Branch.Create(
            Uuid7.NewGuid(),
            Uuid7.NewGuid(),
            "inactive",
            "Inactive Branch",
            false,
            UtcNow);
        branch.SetStatus(BranchStatus.Inactive);

        Assert.Throws<InvalidOperationException>(
            () => branch.SetDefault(true));
        Assert.False(branch.IsDefault);
    }

    [Fact]
    public void Outbox_cannot_start_processing_twice()
    {
        var message = NewOutboxMessage();
        message.StartProcessing();

        Assert.Throws<InvalidOperationException>(message.StartProcessing);
        Assert.Equal(OutboxMessageStatus.Processing, message.Status);
    }

    [Fact]
    public void Processed_outbox_cannot_start_processing()
    {
        var message = NewOutboxMessage();
        message.StartProcessing();
        message.MarkProcessed(UtcNow.AddMinutes(1));

        Assert.Throws<InvalidOperationException>(message.StartProcessing);
        Assert.Equal(OutboxMessageStatus.Processed, message.Status);
    }

    [Fact]
    public void Failed_outbox_cannot_start_processing_without_a_retry_schedule()
    {
        var message = NewOutboxMessage();
        message.StartProcessing();
        message.RecordFailure("permanent", null);

        Assert.Throws<InvalidOperationException>(message.StartProcessing);
        Assert.Equal(OutboxMessageStatus.Failed, message.Status);
    }

    [Fact]
    public void Audit_event_requires_company_when_branch_is_present()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => AuditEvent.Create(
                Uuid7.NewGuid(),
                null,
                Uuid7.NewGuid(),
                null,
                null,
                "Created",
                "Company",
                Uuid7.NewGuid(),
                null,
                null,
                null,
                "{}",
                "correlation",
                UtcNow));

        Assert.Equal("companyId", exception.ParamName);
    }

    private static OutboxMessage NewOutboxMessage()
    {
        return OutboxMessage.Create(
            Uuid7.NewGuid(),
            "Event",
            1,
            "Aggregate",
            Uuid7.NewGuid(),
            1,
            "{}",
            "correlation",
            UtcNow);
    }
}
