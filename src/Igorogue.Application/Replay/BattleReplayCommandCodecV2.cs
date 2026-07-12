using System.Globalization;
using Igorogue.Application.Battle;
using Igorogue.Domain.Board;
using Igorogue.Domain.Combat;

namespace Igorogue.Application.Replay;

internal static class BattleReplayCommandCodecV2
{
    private const int SupportedCommandSchemaVersion = 1;

    internal static void ValidateSupported(BattleReplayAttempt attempt)
    {
        ArgumentNullException.ThrowIfNull(attempt);
        if (attempt.CommandSchemaVersion != SupportedCommandSchemaVersion)
        {
            throw Failure(
                "unsupported_command_schema",
                attempt,
                $"Command schema {attempt.CommandSchemaVersion.ToString(CultureInfo.InvariantCulture)} is not supported by replay schema 2.");
        }

        if (attempt.CommandType is not "battle.authorized_stone_placement" and
            not "battle.authorized_facility_build" and
            not "battle.end_player_turn" and
            not "battle.resolve_enemy_pass" and
            not "battle.authorized_runtime_stone_placement")
        {
            throw Failure(
                "unsupported_command_type",
                attempt,
                $"Command type '{attempt.CommandType}' is not supported by replay schema 2.");
        }
    }

    internal static IBattleCommand Decode(
        BattleReplayAttempt attempt,
        BoardGeometry geometry)
    {
        ArgumentNullException.ThrowIfNull(attempt);
        ArgumentNullException.ThrowIfNull(geometry);

        try
        {
            ValidateSupported(attempt);
            IBattleCommand command = attempt.CommandType switch
            {
                "battle.authorized_stone_placement" or
                "battle.authorized_facility_build" or
                "battle.end_player_turn" or
                "battle.resolve_enemy_pass" =>
                    BattleReplayCommandCodec.Decode(attempt, geometry),
                "battle.authorized_runtime_stone_placement" =>
                    DecodeRuntimeStonePlacement(attempt, geometry),
                _ => throw Failure(
                    "unsupported_command_type",
                    attempt,
                    $"Command type '{attempt.CommandType}' is not supported by replay schema 2."),
            };

            if (!string.Equals(command.CommandType, attempt.CommandType, StringComparison.Ordinal) ||
                command.CommandSchemaVersion != attempt.CommandSchemaVersion ||
                !string.Equals(
                    command.ToCanonicalPayload(),
                    attempt.CanonicalPayload,
                    StringComparison.Ordinal))
            {
                throw Failure(
                    "noncanonical_command_payload",
                    attempt,
                    "Decoded command does not reproduce the exact canonical payload.");
            }

            return command;
        }
        catch (ReplayValidationException)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is ArgumentException or FormatException or InvalidOperationException or OverflowException)
        {
            throw Failure(
                "malformed_command_payload",
                attempt,
                "Command payload is malformed.",
                exception);
        }
    }

    private static AuthorizedRuntimeStonePlacementCommand DecodeRuntimeStonePlacement(
        BattleReplayAttempt attempt,
        BoardGeometry geometry)
    {
        var remaining = attempt.CanonicalPayload.AsSpan();
        RequireHeader(ref remaining, attempt, "authorized-runtime-stone-placement-v1");
        var expectedStateChecksum = ReadField(
            ref remaining,
            attempt,
            "expected_state_checksum");
        var expectedLogChecksum = ReadField(
            ref remaining,
            attempt,
            "expected_log_checksum");
        var actor = ParseColor(ReadField(ref remaining, attempt, "actor"));
        var point = ParsePoint(ReadField(ref remaining, attempt, "point"), geometry);
        var accessMode = ParseAccessMode(
            ReadField(ref remaining, attempt, "access_mode"));
        var stoneInstanceId = ReadField(
            ref remaining,
            attempt,
            "stone_instance_id");
        var stoneKindId = ReadField(
            ref remaining,
            attempt,
            "stone_kind_id");
        var effectMetadataCountText = ReadField(
            ref remaining,
            attempt,
            "effect_metadata_count");
        if (!int.TryParse(
                effectMetadataCountText,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var effectMetadataCount))
        {
            throw Failure(
                "malformed_command_payload",
                attempt,
                "Command payload field 'effect_metadata_count' is malformed.");
        }

        var effectMetadata = new List<string>();
        for (var index = 0; index < effectMetadataCount; index++)
        {
            effectMetadata.Add(ReadField(ref remaining, attempt, "effect_metadata"));
        }

        if (!remaining.IsEmpty)
        {
            throw Failure(
                "malformed_command_payload",
                attempt,
                "Command payload has an invalid field count or terminator.");
        }

        return new AuthorizedRuntimeStonePlacementCommand(
            expectedStateChecksum,
            expectedLogChecksum,
            actor,
            point,
            accessMode,
            stoneInstanceId,
            stoneKindId,
            effectMetadata);
    }

    private static void RequireHeader(
        ref ReadOnlySpan<char> remaining,
        BattleReplayAttempt attempt,
        string expectedHeader)
    {
        if (!TryReadLine(ref remaining, out var line) ||
            !line.Equals(expectedHeader.AsSpan(), StringComparison.Ordinal))
        {
            throw Failure(
                "malformed_command_payload",
                attempt,
                "Command payload has an invalid header, field count, or terminator.");
        }
    }

    private static string ReadField(
        ref ReadOnlySpan<char> remaining,
        BattleReplayAttempt attempt,
        string fieldName)
    {
        var prefix = fieldName + "=";
        if (!TryReadLine(ref remaining, out var line) ||
            !line.StartsWith(prefix.AsSpan(), StringComparison.Ordinal) ||
            line[prefix.Length..].Contains('\r'))
        {
            throw Failure(
                "malformed_command_payload",
                attempt,
                $"Command payload field '{fieldName}' is malformed or out of order.");
        }

        return line[prefix.Length..].ToString();
    }

    private static bool TryReadLine(
        ref ReadOnlySpan<char> remaining,
        out ReadOnlySpan<char> line)
    {
        var newlineIndex = remaining.IndexOf('\n');
        if (newlineIndex < 0)
        {
            line = default;
            return false;
        }

        line = remaining[..newlineIndex];
        remaining = remaining[(newlineIndex + 1)..];
        return true;
    }

    private static CanonicalPoint ParsePoint(string value, BoardGeometry geometry)
    {
        var coordinates = value.AsSpan();
        var commaIndex = coordinates.IndexOf(',');
        if (commaIndex <= 0 ||
            commaIndex == coordinates.Length - 1 ||
            coordinates[(commaIndex + 1)..].Contains(',') ||
            !int.TryParse(
                coordinates[..commaIndex],
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var x) ||
            !int.TryParse(
                coordinates[(commaIndex + 1)..],
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var y))
        {
            throw new FormatException("Canonical point must be encoded as x,y.");
        }

        return geometry.CreateCanonicalPoint(x, y);
    }

    private static StoneColor ParseColor(string value) => value switch
    {
        "black" => StoneColor.Black,
        "white" => StoneColor.White,
        _ => throw new FormatException("Unknown command actor color."),
    };

    private static PlacementAccessMode ParseAccessMode(string value) => value switch
    {
        "normal" => PlacementAccessMode.Normal,
        "terminal_capture" => PlacementAccessMode.TerminalCapture,
        _ => throw new FormatException("Unknown placement access mode."),
    };

    private static ReplayValidationException Failure(
        string reasonId,
        BattleReplayAttempt attempt,
        string message,
        Exception? innerException = null) =>
        new(reasonId, message, attempt.AttemptSequence, innerException);
}
