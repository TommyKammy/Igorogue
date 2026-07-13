using System.Globalization;

using Igorogue.Application.Battle;
using Igorogue.Domain.Board;
using Igorogue.Domain.Cards;

namespace Igorogue.Application.Replay;

internal static class BattleReplayCommandCodecV3
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
                $"Command schema {attempt.CommandSchemaVersion.ToString(CultureInfo.InvariantCulture)} is not supported by replay schema 3.");
        }

        if (attempt.CommandType is not "battle.play_card" and
            not "battle.end_player_turn" and
            not "battle.resolve_bandit_enemy_action" and
            not "battle.restart")
        {
            throw Failure(
                "unsupported_command_type",
                attempt,
                $"Command type '{attempt.CommandType}' is not supported by replay schema 3.");
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
                "battle.play_card" => DecodePlayCard(attempt, geometry),
                "battle.end_player_turn" => DecodeEndPlayerTurn(attempt),
                "battle.resolve_bandit_enemy_action" => DecodeBanditEnemyAction(attempt),
                "battle.restart" => DecodeRestartBattle(attempt),
                _ => throw Failure(
                    "unsupported_command_type",
                    attempt,
                    $"Command type '{attempt.CommandType}' is not supported by replay schema 3."),
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

    private static PlayCardCommand DecodePlayCard(
        BattleReplayAttempt attempt,
        BoardGeometry geometry)
    {
        var fields = ParsePayload(
            attempt,
            "play-card-v1",
            "expected_state_checksum",
            "expected_log_checksum",
            "card_instance_id",
            "target",
            "placement_mode");
        var target = ParsePoint(fields[3], geometry);
        return fields[4] switch
        {
            "none" => new PlayCardCommand(fields[0], fields[1], fields[2], target),
            "frontline" => new PlayCardCommand(
                fields[0],
                fields[1],
                fields[2],
                target,
                StoneCardPlacementMode.Frontline),
            "contact" => new PlayCardCommand(
                fields[0],
                fields[1],
                fields[2],
                target,
                StoneCardPlacementMode.Contact),
            "terminal_capture" => new PlayCardCommand(
                fields[0],
                fields[1],
                fields[2],
                target,
                StoneCardPlacementMode.TerminalCapture),
            _ => throw new FormatException("Unknown starter-card placement mode."),
        };
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

    private static ResolveBanditEnemyActionCommand DecodeBanditEnemyAction(
        BattleReplayAttempt attempt)
    {
        var fields = ParsePayload(
            attempt,
            "resolve-bandit-enemy-action-v1",
            "expected_state_checksum",
            "expected_log_checksum");
        return new ResolveBanditEnemyActionCommand(fields[0], fields[1]);
    }

    private static RestartBattleCommand DecodeRestartBattle(BattleReplayAttempt attempt)
    {
        var fields = ParsePayload(
            attempt,
            "restart-battle-v1",
            "expected_state_checksum",
            "expected_log_checksum");
        return new RestartBattleCommand(fields[0], fields[1]);
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
                "Command payload has an invalid field count or terminator.");
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

    private static ReplayValidationException Failure(
        string reasonId,
        BattleReplayAttempt attempt,
        string message,
        Exception? innerException = null) =>
        new(reasonId, message, attempt.AttemptSequence, innerException);
}
