using System.Globalization;
using Igorogue.Application.Replay;
using Igorogue.Domain.Determinism;

namespace Igorogue.Application.Tests;

public sealed class OrderedCommandLogTests
{
    private const string ContentHash =
        "sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    [Fact]
    public void SameSeedAndOrderedCommandsProduceSameOutputsAndChecksum()
    {
        var commands = new[]
        {
            new ProbeCommand("probe.choose", "choice=alpha\n"),
            new ProbeCommand("probe.choose", "choice=beta\n"),
            new ProbeCommand("probe.end", string.Empty),
        };

        var first = RunProbe(123456789, commands);
        var second = RunProbe(123456789, commands);

        Assert.Equal(first.Outputs, second.Outputs);
        Assert.Equal(first.RngCanonicalState, second.RngCanonicalState);
        Assert.Equal(first.StateChecksum, second.StateChecksum);
        Assert.Equal(first.LogChecksum, second.LogChecksum);
    }

    [Fact]
    public void SeedAndCommandOrderAffectDeterministicResult()
    {
        var firstCommand = new ProbeCommand("probe.choose", "choice=alpha\n");
        var secondCommand = new ProbeCommand("probe.choose", "choice=beta\n");

        var baseline = RunProbe(17, [firstCommand, secondCommand]);
        var reordered = RunProbe(17, [secondCommand, firstCommand]);
        var differentSeed = RunProbe(18, [firstCommand, secondCommand]);

        Assert.NotEqual(baseline.LogChecksum, reordered.LogChecksum);
        Assert.NotEqual(baseline.Outputs, differentSeed.Outputs);
        Assert.NotEqual(baseline.LogChecksum, differentSeed.LogChecksum);
    }

    [Fact]
    public void AppendIsImmutableAndAssignsStableSequenceNumbers()
    {
        var metadata = ReplayMetadata.Create("v0.2.10", ContentHash, 99);
        var empty = OrderedCommandLog.Create(metadata);
        var firstStateChecksum = DeterministicChecksum.Sha256Hex("state-1");
        var secondStateChecksum = DeterministicChecksum.Sha256Hex("state-2");

        var oneEntry = empty.Append(
            new ProbeCommand("probe.choose", "choice=alpha\n"),
            firstStateChecksum);
        var twoEntries = oneEntry.Append(
            new ProbeCommand("probe.end", string.Empty),
            secondStateChecksum);

        Assert.Empty(empty.Entries);
        Assert.Single(oneEntry.Entries);
        Assert.Equal(2, twoEntries.Entries.Count);
        Assert.Equal(0, twoEntries.Entries[0].Sequence);
        Assert.Equal(1, twoEntries.Entries[1].Sequence);
        Assert.Equal(1, twoEntries.Entries[0].CommandSchemaVersion);
        Assert.Equal(firstStateChecksum, twoEntries.Entries[0].ResultChecksum);
        Assert.Equal(twoEntries.CurrentChecksum, twoEntries.Entries[1].LogChecksum);
    }

    [Fact]
    public void InvalidAppendLeavesLogUnchanged()
    {
        var metadata = ReplayMetadata.Create("v0.2.10", ContentHash, 31);
        var log = OrderedCommandLog.Create(metadata);
        var checksumBefore = log.CurrentChecksum;

        Assert.Throws<ArgumentException>(
            () => log.Append(new ProbeCommand("probe.choose", "choice=alpha\n"), "invalid"));

        Assert.Empty(log.Entries);
        Assert.Equal(checksumBefore, log.CurrentChecksum);
    }

    [Fact]
    public void MetadataIsCanonicalAndRejectsMalformedIdentity()
    {
        var metadata = ReplayMetadata.Create(
            "v0.2.10",
            "sha256:ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789",
            -1);

        Assert.Equal(
            "game_version=v0.2.10\n" +
            "content_hash=sha256:abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789\n" +
            "initial_seed=-1\n" +
            "rng_algorithm=splitmix64-v1\n" +
            "command_log_schema=1\n" +
            "checksum_scheme=sha256-length-prefixed-v1\n",
            metadata.ToCanonicalText());
        Assert.Throws<ArgumentException>(
            () => ReplayMetadata.Create("version with spaces", ContentHash, 1));
        Assert.Throws<ArgumentException>(
            () => ReplayMetadata.Create("v0.2.10", "not-a-content-hash", 1));
    }

    [Fact]
    public void UnsupportedCommandSchemaIsRejectedWithoutChangingLog()
    {
        var log = OrderedCommandLog.Create(ReplayMetadata.Create("v0.2.10", ContentHash, 1));
        var checksumBefore = log.CurrentChecksum;

        Assert.Throws<NotSupportedException>(
            () => log.Append(
                new ProbeCommand("probe.choose", "choice=alpha\n", CommandSchemaVersion: 2),
                DeterministicChecksum.Sha256Hex("state")));

        Assert.Empty(log.Entries);
        Assert.Equal(checksumBefore, log.CurrentChecksum);
    }

    [Fact]
    public void HeaderAndEntryChecksumsMatchVersionedGoldenValues()
    {
        var metadata = ReplayMetadata.Create("v0.2.10", ContentHash, 99);
        var empty = OrderedCommandLog.Create(metadata);
        var resultChecksum = DeterministicChecksum.Sha256Hex("state-1");

        var appended = empty.Append(
            new ProbeCommand("probe.choose", "choice=alpha\n"),
            resultChecksum.ToUpperInvariant());

        Assert.Equal(
            "c0a290eb2ba7b0ae92c7140b073a3f667667e5b47aa72e46b4cf0cec75f6a62b",
            empty.CurrentChecksum);
        Assert.Equal(resultChecksum, appended.Entries[0].ResultChecksum);
        Assert.Equal(
            "aaf2170b88e577d39f4345f1fce8676ee0830036426ec691e706c3c455ed67fd",
            appended.CurrentChecksum);
    }

    [Fact]
    public void NullCanonicalPayloadIsRejectedWithoutChangingLog()
    {
        var log = OrderedCommandLog.Create(ReplayMetadata.Create("v0.2.10", ContentHash, 1));
        var checksumBefore = log.CurrentChecksum;

        Assert.Throws<InvalidOperationException>(
            () => log.Append(
                new NullPayloadCommand(),
                DeterministicChecksum.Sha256Hex("state")));

        Assert.Empty(log.Entries);
        Assert.Equal(checksumBefore, log.CurrentChecksum);
    }

    private static ProbeRunResult RunProbe(long seed, IReadOnlyList<ProbeCommand> commands)
    {
        var rng = AuthoritativeRngState.Create(seed);
        var log = OrderedCommandLog.Create(ReplayMetadata.Create("v0.2.10", ContentHash, seed));
        var outputs = new List<ulong>();

        foreach (var command in commands)
        {
            var draw = rng.NextGameplay();
            rng = draw.NextState;
            outputs.Add(draw.Value);
            var resultChecksum = DeterministicChecksum.Combine(
                "task-0002-probe-state-v1",
                rng.ToCanonicalText(),
                command.CommandType,
                command.ToCanonicalPayload(),
                draw.Value.ToString(CultureInfo.InvariantCulture));
            log = log.Append(command, resultChecksum);
        }

        var canonicalState = rng.ToCanonicalText();
        return new ProbeRunResult(
            outputs,
            canonicalState,
            DeterministicChecksum.Sha256Hex(canonicalState),
            log.CurrentChecksum);
    }

    private sealed record ProbeCommand(
        string CommandType,
        string Payload,
        int CommandSchemaVersion = 1) : ICanonicalCommand
    {
        public string ToCanonicalPayload() => Payload;
    }

    private sealed record NullPayloadCommand : ICanonicalCommand
    {
        public string CommandType => "probe.null";

        public int CommandSchemaVersion => 1;

        public string ToCanonicalPayload() => null!;
    }

    private sealed record ProbeRunResult(
        IReadOnlyList<ulong> Outputs,
        string RngCanonicalState,
        string StateChecksum,
        string LogChecksum);
}
