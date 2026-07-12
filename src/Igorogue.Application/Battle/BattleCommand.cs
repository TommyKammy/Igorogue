using System.Globalization;
using Igorogue.Application.Replay;
using Igorogue.Domain.Board;
using Igorogue.Domain.Facilities;

namespace Igorogue.Application.Battle;

public interface IBattleCommand : ICanonicalCommand
{
    string ExpectedStateChecksum { get; }

    string ExpectedLogChecksum { get; }
}

public sealed class AuthorizedStonePlacementCommand : IBattleCommand
{
    public AuthorizedStonePlacementCommand(
        string expectedStateChecksum,
        string expectedLogChecksum,
        StoneColor actor,
        CanonicalPoint point,
        PlacementAccessMode accessMode)
    {
        ExpectedStateChecksum = BattleCommandValidation.CanonicalChecksum(
            expectedStateChecksum,
            nameof(expectedStateChecksum));
        ExpectedLogChecksum = BattleCommandValidation.CanonicalChecksum(
            expectedLogChecksum,
            nameof(expectedLogChecksum));
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

    public string ExpectedLogChecksum { get; }

    public StoneColor Actor { get; }

    public CanonicalPoint Point { get; }

    public PlacementAccessMode AccessMode { get; }

    public string ToCanonicalPayload() =>
        "authorized-stone-placement-v1\n" +
        $"expected_state_checksum={ExpectedStateChecksum}\n" +
        $"expected_log_checksum={ExpectedLogChecksum}\n" +
        $"actor={BattleCommandValidation.ColorId(Actor)}\n" +
        $"point={Point.X.ToString(CultureInfo.InvariantCulture)},{Point.Y.ToString(CultureInfo.InvariantCulture)}\n" +
        $"access_mode={BattleCommandValidation.AccessModeId(AccessMode)}\n";
}

public sealed class AuthorizedRuntimeStonePlacementCommand : IBattleCommand
{
    public AuthorizedRuntimeStonePlacementCommand(
        string expectedStateChecksum,
        string expectedLogChecksum,
        StoneColor actor,
        CanonicalPoint point,
        PlacementAccessMode accessMode,
        string stoneInstanceId,
        string stoneKindId,
        IEnumerable<string> orderedEffectMetadata)
    {
        ExpectedStateChecksum = BattleCommandValidation.CanonicalChecksum(
            expectedStateChecksum,
            nameof(expectedStateChecksum));
        ExpectedLogChecksum = BattleCommandValidation.CanonicalChecksum(
            expectedLogChecksum,
            nameof(expectedLogChecksum));
        if (actor != StoneColor.White)
        {
            throw new ArgumentOutOfRangeException(
                nameof(actor),
                actor,
                "Authorized runtime placement is limited to scripted white actions.");
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
        PlacementDescriptor = new StoneRuntimePlacementDescriptor(
            stoneInstanceId,
            stoneKindId,
            orderedEffectMetadata);
    }

    public string CommandType => "battle.authorized_runtime_stone_placement";

    public int CommandSchemaVersion => 1;

    public string ExpectedStateChecksum { get; }

    public string ExpectedLogChecksum { get; }

    public StoneColor Actor { get; }

    public CanonicalPoint Point { get; }

    public PlacementAccessMode AccessMode { get; }

    public StoneRuntimePlacementDescriptor PlacementDescriptor { get; }

    public string StoneInstanceId => PlacementDescriptor.InstanceId;

    public string StoneKindId => PlacementDescriptor.KindId;

    public IReadOnlyList<string> OrderedEffectMetadata =>
        PlacementDescriptor.OrderedEffectMetadata;

    public string ToCanonicalPayload()
    {
        var lines = new List<string>
        {
            "authorized-runtime-stone-placement-v1",
            $"expected_state_checksum={ExpectedStateChecksum}",
            $"expected_log_checksum={ExpectedLogChecksum}",
            $"actor={BattleCommandValidation.ColorId(Actor)}",
            $"point={Point.X.ToString(CultureInfo.InvariantCulture)},{Point.Y.ToString(CultureInfo.InvariantCulture)}",
            $"access_mode={BattleCommandValidation.AccessModeId(AccessMode)}",
            $"stone_instance_id={StoneInstanceId}",
            $"stone_kind_id={StoneKindId}",
            $"effect_metadata_count={OrderedEffectMetadata.Count.ToString(CultureInfo.InvariantCulture)}",
        };
        lines.AddRange(OrderedEffectMetadata.Select(value => $"effect_metadata={value}"));
        return string.Join('\n', lines) + "\n";
    }
}

public sealed class AuthorizedFacilityBuildCommand : IBattleCommand
{
    public AuthorizedFacilityBuildCommand(
        string expectedStateChecksum,
        string expectedLogChecksum,
        CanonicalPoint point,
        string facilityContentId,
        string instanceId)
    {
        ExpectedStateChecksum = BattleCommandValidation.CanonicalChecksum(
            expectedStateChecksum,
            nameof(expectedStateChecksum));
        ExpectedLogChecksum = BattleCommandValidation.CanonicalChecksum(
            expectedLogChecksum,
            nameof(expectedLogChecksum));
        var request = new FacilityBuildRequest(
            StoneColor.Black,
            point,
            facilityContentId,
            instanceId);

        Point = request.Point;
        FacilityContentId = request.FacilityContentId;
        InstanceId = request.InstanceId;
    }

    public string CommandType => "battle.authorized_facility_build";

    public int CommandSchemaVersion => 1;

    public string ExpectedStateChecksum { get; }

    public string ExpectedLogChecksum { get; }

    public CanonicalPoint Point { get; }

    public string FacilityContentId { get; }

    public string InstanceId { get; }

    public string ToCanonicalPayload() =>
        "authorized-facility-build-v1\n" +
        $"expected_state_checksum={ExpectedStateChecksum}\n" +
        $"expected_log_checksum={ExpectedLogChecksum}\n" +
        "actor=black\n" +
        $"point={Point.X.ToString(CultureInfo.InvariantCulture)},{Point.Y.ToString(CultureInfo.InvariantCulture)}\n" +
        $"facility_content_id={FacilityContentId}\n" +
        $"instance_id={InstanceId}\n";
}

public sealed class EndPlayerTurnCommand : IBattleCommand
{
    public EndPlayerTurnCommand(
        string expectedStateChecksum,
        string expectedLogChecksum)
    {
        ExpectedStateChecksum = BattleCommandValidation.CanonicalChecksum(
            expectedStateChecksum,
            nameof(expectedStateChecksum));
        ExpectedLogChecksum = BattleCommandValidation.CanonicalChecksum(
            expectedLogChecksum,
            nameof(expectedLogChecksum));
    }

    public string CommandType => "battle.end_player_turn";

    public int CommandSchemaVersion => 1;

    public string ExpectedStateChecksum { get; }

    public string ExpectedLogChecksum { get; }

    public string ToCanonicalPayload() =>
        "end-player-turn-v1\n" +
        $"expected_state_checksum={ExpectedStateChecksum}\n" +
        $"expected_log_checksum={ExpectedLogChecksum}\n";
}

public sealed class ResolveEnemyPassCommand : IBattleCommand
{
    public ResolveEnemyPassCommand(
        string expectedStateChecksum,
        string expectedLogChecksum)
    {
        ExpectedStateChecksum = BattleCommandValidation.CanonicalChecksum(
            expectedStateChecksum,
            nameof(expectedStateChecksum));
        ExpectedLogChecksum = BattleCommandValidation.CanonicalChecksum(
            expectedLogChecksum,
            nameof(expectedLogChecksum));
    }

    public string CommandType => "battle.resolve_enemy_pass";

    public int CommandSchemaVersion => 1;

    public string ExpectedStateChecksum { get; }

    public string ExpectedLogChecksum { get; }

    public string ToCanonicalPayload() =>
        "resolve-enemy-pass-v1\n" +
        $"expected_state_checksum={ExpectedStateChecksum}\n" +
        $"expected_log_checksum={ExpectedLogChecksum}\n";
}

internal static class BattleCommandValidation
{
    public static string CanonicalChecksum(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        if (value.Length != 64 || value.Any(character => !Uri.IsHexDigit(character)))
        {
            throw new ArgumentException(
                "Expected checksum must contain exactly 64 hex digits.",
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
