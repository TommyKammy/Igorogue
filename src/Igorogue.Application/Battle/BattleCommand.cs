using System.Globalization;
using Igorogue.Application.Replay;
using Igorogue.Domain.Board;

namespace Igorogue.Application.Battle;

public interface IBattleCommand : ICanonicalCommand
{
    string ExpectedStateChecksum { get; }
}

public sealed class AuthorizedStonePlacementCommand : IBattleCommand
{
    public AuthorizedStonePlacementCommand(
        string expectedStateChecksum,
        StoneColor actor,
        CanonicalPoint point,
        PlacementAccessMode accessMode)
    {
        ExpectedStateChecksum = BattleCommandValidation.CanonicalChecksum(
            expectedStateChecksum,
            nameof(expectedStateChecksum));
        if (actor is not StoneColor.Black and not StoneColor.White)
        {
            throw new ArgumentOutOfRangeException(nameof(actor), actor, "Unknown actor color.");
        }

        ArgumentNullException.ThrowIfNull(point);
        if (accessMode is not PlacementAccessMode.Normal and
            not PlacementAccessMode.TerminalCapture)
        {
            throw new ArgumentOutOfRangeException(
                nameof(accessMode),
                accessMode,
                "Unknown placement access mode.");
        }

        Actor = actor;
        Point = point;
        AccessMode = accessMode;
    }

    public string CommandType => "battle.authorized_stone_placement";

    public int CommandSchemaVersion => 1;

    public string ExpectedStateChecksum { get; }

    public StoneColor Actor { get; }

    public CanonicalPoint Point { get; }

    public PlacementAccessMode AccessMode { get; }

    public string ToCanonicalPayload() =>
        "authorized-stone-placement-v1\n" +
        $"expected_state_checksum={ExpectedStateChecksum}\n" +
        $"actor={BattleCommandValidation.ColorId(Actor)}\n" +
        $"point={Point.X.ToString(CultureInfo.InvariantCulture)},{Point.Y.ToString(CultureInfo.InvariantCulture)}\n" +
        $"access_mode={BattleCommandValidation.AccessModeId(AccessMode)}\n";
}

public sealed class EndPlayerTurnCommand : IBattleCommand
{
    public EndPlayerTurnCommand(string expectedStateChecksum)
    {
        ExpectedStateChecksum = BattleCommandValidation.CanonicalChecksum(
            expectedStateChecksum,
            nameof(expectedStateChecksum));
    }

    public string CommandType => "battle.end_player_turn";

    public int CommandSchemaVersion => 1;

    public string ExpectedStateChecksum { get; }

    public string ToCanonicalPayload() =>
        "end-player-turn-v1\n" +
        $"expected_state_checksum={ExpectedStateChecksum}\n";
}

public sealed class ResolveEnemyPassCommand : IBattleCommand
{
    public ResolveEnemyPassCommand(string expectedStateChecksum)
    {
        ExpectedStateChecksum = BattleCommandValidation.CanonicalChecksum(
            expectedStateChecksum,
            nameof(expectedStateChecksum));
    }

    public string CommandType => "battle.resolve_enemy_pass";

    public int CommandSchemaVersion => 1;

    public string ExpectedStateChecksum { get; }

    public string ToCanonicalPayload() =>
        "resolve-enemy-pass-v1\n" +
        $"expected_state_checksum={ExpectedStateChecksum}\n";
}

internal static class BattleCommandValidation
{
    public static string CanonicalChecksum(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        if (value.Length != 64 || value.Any(character => !Uri.IsHexDigit(character)))
        {
            throw new ArgumentException(
                "Expected state checksum must contain exactly 64 hex digits.",
                parameterName);
        }

        return value.ToLowerInvariant();
    }

    public static string ColorId(StoneColor color) => color switch
    {
        StoneColor.Black => "black",
        StoneColor.White => "white",
        _ => throw new InvalidOperationException("Command contains an unknown actor color."),
    };

    public static string AccessModeId(PlacementAccessMode mode) => mode switch
    {
        PlacementAccessMode.Normal => "normal",
        PlacementAccessMode.TerminalCapture => "terminal_capture",
        _ => throw new InvalidOperationException("Command contains an unknown access mode."),
    };
}
