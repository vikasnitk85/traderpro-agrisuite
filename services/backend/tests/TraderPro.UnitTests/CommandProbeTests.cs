using System.Text.Json;
using TraderPro.Application.Platform.CommandProbes;
using TraderPro.Domain.Common;
using TraderPro.Domain.Platform;

namespace TraderPro.UnitTests;

public sealed class CommandProbeTests
{
    private static readonly DateTimeOffset UtcNow =
        new(2026, 7, 29, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Command_probe_starts_at_zero_and_version_one_with_uuid_v7()
    {
        var probe = CommandProbe.Create(
            Uuid7.NewGuid(),
            "Probe A",
            UtcNow);

        Assert.Equal(7, probe.Id.Version);
        Assert.Equal("Probe A", probe.Name);
        Assert.Equal(0, probe.Counter);
        Assert.Equal(1, probe.Version);
    }

    [Fact]
    public void Command_probe_allows_only_positive_whole_number_increments()
    {
        var probe = CommandProbe.Create(
            Uuid7.NewGuid(),
            "Probe A",
            UtcNow);

        probe.Increment(2);

        Assert.Equal(2, probe.Counter);
        Assert.Throws<ArgumentOutOfRangeException>(() => probe.Increment(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => probe.Increment(-1));
    }

    [Fact]
    public void Completed_idempotency_result_requires_valid_http_status()
    {
        var record = IdempotencyRecord.Create(
            Uuid7.NewGuid(),
            "key",
            CommandProbeCommandTypes.Create,
            "hash",
            UtcNow);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => record.Complete("{}", 99, UtcNow));
        Assert.Equal(IdempotencyRecordStatus.Pending, record.Status);
        Assert.Null(record.ResultStatusCode);
    }

    [Fact]
    public void Equivalent_typed_create_commands_have_the_same_hash()
    {
        const string firstJson =
            """ { "ignoredTransportValue": 1, "name": "Probe A" } """;
        const string secondJson =
            """
            {
              "name":"Probe A",
              "ignoredTransportValue": 2
            }
            """;
        var first = JsonSerializer.Deserialize<CreateBody>(
            firstJson,
            JsonOptions)!;
        var second = JsonSerializer.Deserialize<CreateBody>(
            secondJson,
            JsonOptions)!;

        Assert.Equal(
            CommandProbeRequestHash.ForCreate(first.Name),
            CommandProbeRequestHash.ForCreate(second.Name));
        Assert.Equal(
            CommandProbeRequestHash.ForCreate(" Probe A "),
            CommandProbeRequestHash.ForCreate("Probe A"));
    }

    [Fact]
    public void Business_relevant_command_values_change_the_hash()
    {
        var id = Uuid7.NewGuid();
        var baselineCreate = CommandProbeRequestHash.ForCreate("Probe A");
        var baselineIncrement = CommandProbeRequestHash.ForIncrement(id, 1, 1);

        Assert.NotEqual(
            baselineCreate,
            CommandProbeRequestHash.ForCreate("Probe B"));
        Assert.NotEqual(
            baselineIncrement,
            CommandProbeRequestHash.ForIncrement(Uuid7.NewGuid(), 1, 1));
        Assert.NotEqual(
            baselineIncrement,
            CommandProbeRequestHash.ForIncrement(id, 2, 1));
        Assert.NotEqual(
            baselineIncrement,
            CommandProbeRequestHash.ForIncrement(id, 1, 2));
    }

    [Fact]
    public void Canonical_hash_is_lowercase_sha256_hex()
    {
        var hash = CommandProbeRequestHash.ForCreate("Probe A");

        Assert.Equal(64, hash.Length);
        Assert.Matches("^[0-9a-f]{64}$", hash);
    }

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private sealed record CreateBody(string Name);
}
