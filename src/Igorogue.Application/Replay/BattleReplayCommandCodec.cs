using System.Globalization;
using Igorogue.Application.Battle;
using Igorogue.Domain.Board;
using Igorogue.Domain.Combat;

namespace Igorogue.Application.Replay;

internal static class BattleReplayCommandCodec
{
    internal static IBattleCommand Decode(
        BattleReplayAttempt attempt,
        BoardGeometry geometry)
    {
        ArgumentNullException.ThrowIfNull(attempt);
        ArgumentNullException.ThrowIfNull(geometry);

        try
        {
            if (attempt.CommandSchemaVersion != 1)
            {
                throw Failure(
                    "unsupported_command_schema",
                    attempt,
                    $"Command schema {attempt.CommandSchemaVersion.ToString(CultureInfo.InvariantCulture)} is not supported.");
            }

            IBattleCommand command = attempt.CommandType switch
            {
                "battle.authorized_stone_placement" => DecodeStonePlacement(attempt, geometry),
                "battle.authorized_facility_build" => DecodeFacilityBuild(attempt, geometry),
                "battle.end_player_turn" => DecodeEndPlayerTurn(attempt),
                "battle.resolve_enemy_pass" => DecodeEnemyPass(attempt),
                _ => throw Failure(
                    "unsupported_command_type",
                    attempt,
                    $"Command type '{attempt.CommandType}' is not supported by replay schema 1."),
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

    private static AuthorizedStonePlacementCommand DecodeStonePlacement(
        BattleReplayAttempt attempt,
        BoardGeometry geometry)
    {
        var fields = ParsePayload(
            attempt,
            "authorized-stone-placement-v1",
            "expected_state_checksum",
            "expected_log_checksum",
            "actor",
            "point",
            "access_mode");
        return new AuthorizedStonePlacementCommand(
            fields[0],
            fields[1],
            ParseColor(fields[2]),
            ParsePoint(fields[3], geometry),
            ParseAccessMode(fields[4]));
    }

    private static AuthorizedFacilityBuildCommand DecodeFacilityBuild(
        BattleReplayAttempt attempt,
        BoardGeometry geometry)
    {
        var fields = ParsePayload(
            attempt,
            "authorized-facility-build-v1",
            "expected_state_checksum",
            "expected_log_checksum",
            "actor",
            "point",
            "facility_content_id",
            "instance_id");
        if (!string.Equals(fields[2], "black", StringComparison.Ordinal))
        {
            throw Failure(
                "malformed_command_payload",
                attempt,
                "Authorized facility-build actor must be black.");
        }

        return new AuthorizedFacilityBuildCommand(
            fields[0],
            fields[1],
            ParsePoint(fields[3], geometry),
            fields[4],
            fields[5]);
    }

    private static EndPlayerTurnCommand DecodeEndPlayerTurn(BattleReplayAttempt attempt)
    {
        var fields = ParsePayload(
            attempt,
            "end-player-turn-v1",
            "expected_state_checksum",
            "expected_log_checksum");
        return new EndPlayerTurnCommand(fields[0], fields[1]);
    }

    private static ResolveEnemyPassCommand DecodeEnemyPass(BattleReplayAttempt attempt)
    {
        var fields = ParsePayload(
            attempt,
            "resolve-enemy-pass-v1",
            "expected_state_checksum",
            "expected_log_checksum");
        return new ResolveEnemyPassCommand(fields[0], fields[1]);
    }

    private static string[] ParsePayload(
        BattleReplayAttempt attempt,
        string header,
        params string[] fieldNames)
    {
        var remaining = attempt.CanonicalPayload.AsSpan();
        if (!TryReadLine(ref remaining, out var headerLine) ||
            !headerLine.Equals(header.AsSpan(), StringComparison.Ordinal))
        {
            throw Failure(
                "malformed_command_payload",
                attempt,
                "Command payload has an invalid header, field count, or terminator.");
        }

        var values = new string[fieldNames.Length];
        for (var index = 0; index < fieldNames.Length; index++)
        {
            var prefix = fieldNames[index] + "=";
            if (!TryReadLine(ref remaining, out var line) ||
                !line.StartsWith(prefix.AsSpan(), StringComparison.Ordinal) ||
                line[prefix.Length..].Contains('\r'))
            {
                throw Failure(
                    "malformed_command_payload",
                    attempt,
                    $"Command payload field '{fieldNames[index]}' is malformed or out of order.");
            }

            values[index] = line[prefix.Length..].ToString();
        }

        if (!remaining.IsEmpty)
        {
            throw Failure(
                "malformed_command_payload",
                attempt,
                "Command payload has an invalid header, field count, or terminator.");
        }

        return values;
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
