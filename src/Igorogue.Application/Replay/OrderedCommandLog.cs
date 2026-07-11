using System.Collections.ObjectModel;
using System.Globalization;
using Igorogue.Domain.Determinism;

namespace Igorogue.Application.Replay;

public sealed class OrderedCommandLog
{
    private const string HeaderVersion = "igorogue-command-log-v1";
    private const string EntryVersion = "igorogue-command-log-entry-v1";
    private const int SupportedCommandSchemaVersion = 1;

    private readonly CommandLogEntry[] entries;
    private readonly ReadOnlyCollection<CommandLogEntry> entryView;

    private OrderedCommandLog(
        ReplayMetadata metadata,
        CommandLogEntry[] entries,
        string currentChecksum)
    {
        Metadata = metadata;
        this.entries = entries;
        entryView = Array.AsReadOnly(entries);
        CurrentChecksum = currentChecksum;
    }

    public ReplayMetadata Metadata { get; }

    public IReadOnlyList<CommandLogEntry> Entries => entryView;

    public string CurrentChecksum { get; }

    public static OrderedCommandLog Create(ReplayMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        var checksum = DeterministicChecksum.Combine(HeaderVersion, metadata.ToCanonicalText());
        return new OrderedCommandLog(metadata, [], checksum);
    }

    public OrderedCommandLog Append(ICanonicalCommand command, string resultChecksum)
    {
        ArgumentNullException.ThrowIfNull(command);
        var commandType = ValidateCommandType(command.CommandType);
        var commandSchemaVersion = ValidateCommandSchemaVersion(command.CommandSchemaVersion);
        var payload = command.ToCanonicalPayload()
            ?? throw new InvalidOperationException("Canonical command payload cannot be null.");
        var canonicalResultChecksum = ValidateResultChecksum(resultChecksum);
        var sequence = entries.LongLength;
        var sequenceText = sequence.ToString(CultureInfo.InvariantCulture);
        var logChecksum = DeterministicChecksum.Combine(
            EntryVersion,
            CurrentChecksum,
            sequenceText,
            commandType,
            commandSchemaVersion.ToString(CultureInfo.InvariantCulture),
            payload,
            canonicalResultChecksum);

        var entry = new CommandLogEntry(
            sequence,
            commandType,
            commandSchemaVersion,
            payload,
            canonicalResultChecksum,
            logChecksum);
        var nextEntries = new CommandLogEntry[entries.Length + 1];
        Array.Copy(entries, nextEntries, entries.Length);
        nextEntries[^1] = entry;
        return new OrderedCommandLog(Metadata, nextEntries, logChecksum);
    }

    private static string ValidateCommandType(string commandType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandType);
        if (commandType.Any(character =>
                !char.IsAsciiLetterOrDigit(character) && character is not '.' and not '_' and not '-'))
        {
            throw new ArgumentException(
                "Command type must contain only ASCII letters, digits, '.', '_', or '-'.",
                nameof(commandType));
        }

        return commandType;
    }

    private static int ValidateCommandSchemaVersion(int commandSchemaVersion)
    {
        if (commandSchemaVersion != SupportedCommandSchemaVersion)
        {
            throw new NotSupportedException(
                $"Command schema version {commandSchemaVersion.ToString(CultureInfo.InvariantCulture)} is not supported.");
        }

        return commandSchemaVersion;
    }

    private static string ValidateResultChecksum(string resultChecksum)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resultChecksum);
        if (resultChecksum.Length != 64 || resultChecksum.Any(character => !Uri.IsHexDigit(character)))
        {
            throw new ArgumentException(
                "Result checksum must contain exactly 64 hex digits.",
                nameof(resultChecksum));
        }

        return resultChecksum.ToLowerInvariant();
    }
}

public sealed record CommandLogEntry(
    long Sequence,
    string CommandType,
    int CommandSchemaVersion,
    string CanonicalPayload,
    string ResultChecksum,
    string LogChecksum);
