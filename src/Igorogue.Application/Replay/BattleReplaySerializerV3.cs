using System.Text.Json;
using System.Text.Json.Serialization;

namespace Igorogue.Application.Replay;

public static class BattleReplaySerializerV3
{
    public const string SchemaId = BattleReplayFormatV3.SchemaId;
    public const int SchemaVersion = BattleReplayFormatV3.SchemaVersion;
    public const string StateProjection = BattleReplayFormatV3.StateProjection;
    public const int MaxDocumentBytes = 16 * 1024 * 1024;
    public const int MaxAttempts = BattleReplayFormatV3.MaxAttempts;

    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        AllowTrailingCommas = false,
        PropertyNameCaseInsensitive = false,
        ReadCommentHandling = JsonCommentHandling.Disallow,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    private static readonly JsonSerializerOptions WriteOptions = new(ReadOptions)
    {
        WriteIndented = true,
    };

    public static void Save(BattleReplayDocumentV3 document, Stream destination)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(destination);
        if (!destination.CanWrite)
        {
            throw new ArgumentException("Replay destination stream must be writable.", nameof(destination));
        }

        ValidateSerializationBudget(document);
        document.ValidateIntegrity();
        using var serialized = new MemoryStream();
        var limited = new SizeLimitedWriteStream(serialized, MaxDocumentBytes);
        JsonSerializer.Serialize(limited, ToDto(document), WriteOptions);
        limited.WriteByte((byte)'\n');

        serialized.Position = 0;
        serialized.CopyTo(destination);
    }

    public static BattleReplayDocumentV3 Load(Stream source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (!source.CanRead)
        {
            throw new ArgumentException("Replay source stream must be readable.", nameof(source));
        }

        try
        {
            using var buffer = new MemoryStream();
            var chunk = new byte[81920];
            while (true)
            {
                var remaining = (MaxDocumentBytes + 1) - checked((int)buffer.Length);
                var read = source.Read(chunk, 0, Math.Min(chunk.Length, remaining));
                if (read == 0)
                {
                    break;
                }

                buffer.Write(chunk, 0, read);
                if (buffer.Length > MaxDocumentBytes)
                {
                    throw new ReplayValidationException(
                        "replay_too_large",
                        $"Replay document exceeds the {MaxDocumentBytes} byte limit.");
                }
            }

            if (buffer.Length == 0)
            {
                throw new ReplayValidationException(
                    "malformed_replay",
                    "Replay document is empty.");
            }

            var utf8 = buffer.ToArray();
            using (var parsed = JsonDocument.Parse(
                       utf8,
                       new JsonDocumentOptions
                       {
                           AllowTrailingCommas = false,
                           CommentHandling = JsonCommentHandling.Disallow,
                           MaxDepth = 64,
                       }))
            {
                RejectAttemptCountBeforeMaterialization(parsed.RootElement);
                RejectDuplicateProperties(parsed.RootElement, depth: 0);
            }

            var dto = JsonSerializer.Deserialize<ReplayDto>(utf8, ReadOptions)
                ?? throw new ReplayValidationException(
                    "malformed_replay",
                    "Replay document is null.");
            return FromDto(dto);
        }
        catch (ReplayValidationException)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is JsonException or ArgumentException or InvalidOperationException or
                NotSupportedException or OverflowException)
        {
            throw new ReplayValidationException(
                "malformed_replay",
                "Replay JSON is malformed or unsupported.",
                innerException: exception);
        }
    }

    private static BattleReplayDocumentV3 FromDto(ReplayDto dto)
    {
        if (!string.Equals(dto.SchemaId, SchemaId, StringComparison.Ordinal) ||
            dto.SchemaVersion != SchemaVersion)
        {
            throw new ReplayValidationException(
                "unsupported_replay_schema",
                "Replay schema ID or version is not supported.");
        }

        var stateProjection = Required(dto.StateProjection, "state_projection");
        if (!string.Equals(stateProjection, StateProjection, StringComparison.Ordinal))
        {
            throw new ReplayValidationException(
                "unsupported_state_projection",
                "Replay state projection is not supported by replay schema 3.");
        }

        if (!string.Equals(
                dto.IntegrityScheme,
                BattleReplayFormatV3.IntegrityScheme,
                StringComparison.Ordinal))
        {
            throw new ReplayValidationException(
                "unsupported_integrity_scheme",
                "Replay integrity scheme is not supported.");
        }

        var metadataDto = dto.Metadata
            ?? throw Missing("metadata");
        var metadata = ReplayMetadata.Create(
            Required(metadataDto.GameVersion, "metadata.game_version"),
            Required(metadataDto.ContentHash, "metadata.content_hash"),
            Required(metadataDto.InitialSeed, "metadata.initial_seed"));
        if (!string.Equals(
                metadataDto.RngAlgorithm,
                metadata.RngAlgorithmVersion,
                StringComparison.Ordinal) ||
            metadataDto.CommandLogSchema != metadata.CommandLogSchemaVersion ||
            !string.Equals(
                metadataDto.ChecksumScheme,
                metadata.ChecksumScheme,
                StringComparison.Ordinal))
        {
            throw new ReplayValidationException(
                "unsupported_metadata",
                "Replay metadata uses an unsupported deterministic contract.");
        }

        var attemptDtos = dto.Attempts
            ?? throw Missing("attempts");
        if (attemptDtos.Count > MaxAttempts)
        {
            throw new ReplayValidationException(
                "replay_too_many_attempts",
                $"Replay document exceeds the {MaxAttempts} attempt limit.");
        }

        var attempts = new BattleReplayAttempt[attemptDtos.Count];
        for (var index = 0; index < attemptDtos.Count; index++)
        {
            var attempt = attemptDtos[index]
                ?? throw Missing($"attempts[{index.ToString(System.Globalization.CultureInfo.InvariantCulture)}]");
            attempts[index] = new BattleReplayAttempt(
                Required(attempt.AttemptSequence, "attempt_sequence"),
                Required(attempt.CommandType, "command_type"),
                Required(attempt.CommandSchemaVersion, "command_schema_version"),
                Required(attempt.CanonicalPayload, "canonical_payload"),
                Required(attempt.BeforeStateChecksum, "before_state_checksum"),
                Required(attempt.BeforeLogChecksum, "before_log_checksum"),
                Required(attempt.Accepted, "accepted"),
                Required(attempt.ReasonId, "reason_id"),
                Required(attempt.StateChecksum, "state_checksum"),
                Required(attempt.LogChecksum, "log_checksum"),
                Required(attempt.AttemptChecksum, "attempt_checksum"));
        }

        var terminalDto = dto.Terminal
            ?? throw Missing("terminal");
        var terminal = new BattleReplayTerminal(
            Required(terminalDto.IsTerminal, "terminal.is_terminal"),
            Required(terminalDto.OutcomeId, "terminal.outcome"),
            Required(terminalDto.EndReasonId, "terminal.end_reason"));
        return BattleReplayDocumentV3.CreateValidated(
            metadata,
            Required(dto.InitialStateChecksum, "initial_state_checksum"),
            Required(dto.InitialLogChecksum, "initial_log_checksum"),
            attempts,
            Required(dto.FinalStateChecksum, "final_state_checksum"),
            Required(dto.FinalLogChecksum, "final_log_checksum"),
            terminal,
            stateProjection,
            Required(dto.AttemptsChecksum, "attempts_checksum"),
            Required(dto.DocumentChecksum, "document_checksum"));
    }

    private static ReplayDto ToDto(BattleReplayDocumentV3 document) => new()
    {
        SchemaId = SchemaId,
        SchemaVersion = SchemaVersion,
        StateProjection = document.StateProjection,
        IntegrityScheme = document.IntegrityScheme,
        Metadata = new MetadataDto
        {
            GameVersion = document.Metadata.GameVersion,
            ContentHash = document.Metadata.ContentHash,
            InitialSeed = document.Metadata.InitialSeed,
            RngAlgorithm = document.Metadata.RngAlgorithmVersion,
            CommandLogSchema = document.Metadata.CommandLogSchemaVersion,
            ChecksumScheme = document.Metadata.ChecksumScheme,
        },
        InitialStateChecksum = document.InitialStateChecksum,
        InitialLogChecksum = document.InitialLogChecksum,
        Attempts = document.Attempts.Select(attempt => new AttemptDto
        {
            AttemptSequence = attempt.AttemptSequence,
            CommandType = attempt.CommandType,
            CommandSchemaVersion = attempt.CommandSchemaVersion,
            CanonicalPayload = attempt.CanonicalPayload,
            BeforeStateChecksum = attempt.BeforeStateChecksum,
            BeforeLogChecksum = attempt.BeforeLogChecksum,
            Accepted = attempt.Accepted,
            ReasonId = attempt.ReasonId,
            StateChecksum = attempt.StateChecksum,
            LogChecksum = attempt.LogChecksum,
            AttemptChecksum = attempt.AttemptChecksum,
        }).ToArray(),
        AttemptsChecksum = document.AttemptsChecksum,
        FinalStateChecksum = document.FinalStateChecksum,
        FinalLogChecksum = document.FinalLogChecksum,
        Terminal = new TerminalDto
        {
            IsTerminal = document.Terminal.IsTerminal,
            OutcomeId = document.Terminal.OutcomeId,
            EndReasonId = document.Terminal.EndReasonId,
        },
        DocumentChecksum = document.DocumentChecksum,
    };

    private static void ValidateSerializationBudget(BattleReplayDocumentV3 document)
    {
        long variableCharacterCount = 0;
        Add(document.StateProjection);
        Add(document.Metadata.GameVersion);
        Add(document.Metadata.ContentHash);
        Add(document.Metadata.RngAlgorithmVersion);
        Add(document.Metadata.ChecksumScheme);
        Add(document.InitialStateChecksum);
        Add(document.InitialLogChecksum);
        Add(document.AttemptsChecksum);
        Add(document.FinalStateChecksum);
        Add(document.FinalLogChecksum);
        Add(document.Terminal.OutcomeId);
        Add(document.Terminal.EndReasonId);
        Add(document.DocumentChecksum);
        foreach (var attempt in document.Attempts)
        {
            Add(attempt.CommandType);
            Add(attempt.CanonicalPayload);
            Add(attempt.BeforeStateChecksum);
            Add(attempt.BeforeLogChecksum);
            Add(attempt.ReasonId);
            Add(attempt.StateChecksum);
            Add(attempt.LogChecksum);
            Add(attempt.AttemptChecksum);
        }

        void Add(string value)
        {
            variableCharacterCount = checked(variableCharacterCount + value.Length);
            if (variableCharacterCount > MaxDocumentBytes)
            {
                throw new ReplayValidationException(
                    "replay_too_large",
                    $"Replay variable fields exceed the {MaxDocumentBytes} byte budget.");
            }
        }
    }

    private static void RejectDuplicateProperties(JsonElement element, int depth)
    {
        if (depth > 64)
        {
            throw new ReplayValidationException(
                "malformed_replay",
                "Replay JSON exceeds the supported nesting depth.");
        }

        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            var propertyIndex = 0;
            foreach (var property in element.EnumerateObject())
            {
                if (!names.Add(property.Name))
                {
                    throw new ReplayValidationException(
                        "duplicate_json_property",
                        $"Replay JSON contains a duplicate object property at depth {depth}, index {propertyIndex}.");
                }

                RejectDuplicateProperties(property.Value, depth + 1);
                propertyIndex++;
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in element.EnumerateArray())
            {
                RejectDuplicateProperties(child, depth + 1);
            }
        }
    }

    private static void RejectAttemptCountBeforeMaterialization(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (var property in root.EnumerateObject())
        {
            if (property.NameEquals("attempts") &&
                property.Value.ValueKind == JsonValueKind.Array &&
                property.Value.GetArrayLength() > MaxAttempts)
            {
                throw new ReplayValidationException(
                    "replay_too_many_attempts",
                    $"Replay document exceeds the {MaxAttempts} attempt limit.");
            }
        }
    }

    private static ReplayValidationException Missing(string fieldName) =>
        new("malformed_replay", $"Replay field '{fieldName}' is required.");

    private static string Required(string? value, string fieldName) =>
        value ?? throw Missing(fieldName);

    private static int Required(int? value, string fieldName) =>
        value ?? throw Missing(fieldName);

    private static long Required(long? value, string fieldName) =>
        value ?? throw Missing(fieldName);

    private static bool Required(bool? value, string fieldName) =>
        value ?? throw Missing(fieldName);

    private sealed record ReplayDto
    {
        [JsonPropertyName("schema_id"), JsonPropertyOrder(0)]
        public string? SchemaId { get; init; }

        [JsonPropertyName("schema_version"), JsonPropertyOrder(1)]
        public int? SchemaVersion { get; init; }

        [JsonPropertyName("state_projection"), JsonPropertyOrder(2)]
        public string? StateProjection { get; init; }

        [JsonPropertyName("integrity_scheme"), JsonPropertyOrder(3)]
        public string? IntegrityScheme { get; init; }

        [JsonPropertyName("metadata"), JsonPropertyOrder(4)]
        public MetadataDto? Metadata { get; init; }

        [JsonPropertyName("initial_state_checksum"), JsonPropertyOrder(5)]
        public string? InitialStateChecksum { get; init; }

        [JsonPropertyName("initial_log_checksum"), JsonPropertyOrder(6)]
        public string? InitialLogChecksum { get; init; }

        [JsonPropertyName("attempts"), JsonPropertyOrder(7)]
        public IReadOnlyList<AttemptDto?>? Attempts { get; init; }

        [JsonPropertyName("attempts_checksum"), JsonPropertyOrder(8)]
        public string? AttemptsChecksum { get; init; }

        [JsonPropertyName("final_state_checksum"), JsonPropertyOrder(9)]
        public string? FinalStateChecksum { get; init; }

        [JsonPropertyName("final_log_checksum"), JsonPropertyOrder(10)]
        public string? FinalLogChecksum { get; init; }

        [JsonPropertyName("terminal"), JsonPropertyOrder(11)]
        public TerminalDto? Terminal { get; init; }

        [JsonPropertyName("document_checksum"), JsonPropertyOrder(12)]
        public string? DocumentChecksum { get; init; }
    }

    private sealed record MetadataDto
    {
        [JsonPropertyName("game_version"), JsonPropertyOrder(0)]
        public string? GameVersion { get; init; }

        [JsonPropertyName("content_hash"), JsonPropertyOrder(1)]
        public string? ContentHash { get; init; }

        [JsonPropertyName("initial_seed"), JsonPropertyOrder(2)]
        public long? InitialSeed { get; init; }

        [JsonPropertyName("rng_algorithm"), JsonPropertyOrder(3)]
        public string? RngAlgorithm { get; init; }

        [JsonPropertyName("command_log_schema"), JsonPropertyOrder(4)]
        public int? CommandLogSchema { get; init; }

        [JsonPropertyName("checksum_scheme"), JsonPropertyOrder(5)]
        public string? ChecksumScheme { get; init; }
    }

    private sealed record AttemptDto
    {
        [JsonPropertyName("attempt_sequence"), JsonPropertyOrder(0)]
        public int? AttemptSequence { get; init; }

        [JsonPropertyName("command_type"), JsonPropertyOrder(1)]
        public string? CommandType { get; init; }

        [JsonPropertyName("command_schema_version"), JsonPropertyOrder(2)]
        public int? CommandSchemaVersion { get; init; }

        [JsonPropertyName("canonical_payload"), JsonPropertyOrder(3)]
        public string? CanonicalPayload { get; init; }

        [JsonPropertyName("before_state_checksum"), JsonPropertyOrder(4)]
        public string? BeforeStateChecksum { get; init; }

        [JsonPropertyName("before_log_checksum"), JsonPropertyOrder(5)]
        public string? BeforeLogChecksum { get; init; }

        [JsonPropertyName("accepted"), JsonPropertyOrder(6)]
        public bool? Accepted { get; init; }

        [JsonPropertyName("reason_id"), JsonPropertyOrder(7)]
        public string? ReasonId { get; init; }

        [JsonPropertyName("state_checksum"), JsonPropertyOrder(8)]
        public string? StateChecksum { get; init; }

        [JsonPropertyName("log_checksum"), JsonPropertyOrder(9)]
        public string? LogChecksum { get; init; }

        [JsonPropertyName("attempt_checksum"), JsonPropertyOrder(10)]
        public string? AttemptChecksum { get; init; }
    }

    private sealed record TerminalDto
    {
        [JsonPropertyName("is_terminal"), JsonPropertyOrder(0)]
        public bool? IsTerminal { get; init; }

        [JsonPropertyName("outcome"), JsonPropertyOrder(1)]
        public string? OutcomeId { get; init; }

        [JsonPropertyName("end_reason"), JsonPropertyOrder(2)]
        public string? EndReasonId { get; init; }
    }

    private sealed class SizeLimitedWriteStream : Stream
    {
        private readonly Stream destination;
        private readonly long maxBytes;
        private long bytesWritten;

        internal SizeLimitedWriteStream(Stream destination, long maxBytes)
        {
            this.destination = destination;
            this.maxBytes = maxBytes;
        }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => bytesWritten;

        public override long Position
        {
            get => bytesWritten;
            set => throw new NotSupportedException();
        }

        public override void Flush() => destination.Flush();

        public override int Read(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) =>
            throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
        {
            EnsureCapacity(count);
            destination.Write(buffer, offset, count);
            bytesWritten += count;
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            EnsureCapacity(buffer.Length);
            destination.Write(buffer);
            bytesWritten += buffer.Length;
        }

        public override void WriteByte(byte value)
        {
            EnsureCapacity(1);
            destination.WriteByte(value);
            bytesWritten++;
        }

        private void EnsureCapacity(int count)
        {
            if (count < 0 || bytesWritten > maxBytes - count)
            {
                throw new ReplayValidationException(
                    "replay_too_large",
                    $"Replay document exceeds the {maxBytes} byte limit.");
            }
        }
    }
}
