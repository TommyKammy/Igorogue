using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

using Igorogue.Domain.Board;

namespace Igorogue.Domain.Combat;

public enum CaptureBoundary : byte
{
    PlacementResolution = 1,
    EnemyTurnTemporaryLibertyExpirySweep = 2,
}

public enum CapturingWindow : byte
{
    PlayerActionWindow = 1,
    ClosedPlayerWindow = 2,
}

public sealed class CapturedGroup
{
    private readonly ReadOnlyCollection<StoneRuntimeInstance> stoneInstanceView;

    internal CapturedGroup(
        StoneGroup sourceGroup,
        StoneRuntimeInstance[] canonicalStoneInstances)
    {
        SourceGroup = sourceGroup;
        Color = sourceGroup.Color;
        GroupAnchor = sourceGroup.Anchor;
        stoneInstanceView = Array.AsReadOnly(
            (StoneRuntimeInstance[])canonicalStoneInstances.Clone());
        ContainsKing = canonicalStoneInstances.Any(instance => instance.IsKing);
        CapturingColor = Opposite(Color);
    }

    internal StoneGroup SourceGroup { get; }

    public StoneColor Color { get; }

    public CanonicalPoint GroupAnchor { get; }

    public IReadOnlyList<StoneRuntimeInstance> StoneInstances => stoneInstanceView;

    public bool ContainsKing { get; }

    public StoneColor CapturingColor { get; }

    private static StoneColor Opposite(StoneColor color) => color switch
    {
        StoneColor.Black => StoneColor.White,
        StoneColor.White => StoneColor.Black,
        _ => throw new InvalidOperationException("Captured group contains an unknown color."),
    };
}

public sealed class CaptureBatch
{
    public const string EncodingVersion = "capture-batch-v1";

    private readonly ReadOnlyCollection<CapturedGroup> capturedGroupView;
    private readonly ReadOnlyCollection<StoneRuntimeInstance> capturedStoneInstanceView;

    private CaptureBatch(
        string batchId,
        string reasonId,
        CaptureBoundary boundary,
        int? boundaryEnemyTurnIndex,
        CapturingWindow capturingWindow,
        CapturedGroup[] capturedGroups)
    {
        BatchId = batchId;
        ReasonId = reasonId;
        Boundary = boundary;
        BoundaryEnemyTurnIndex = boundaryEnemyTurnIndex;
        CapturingWindow = capturingWindow;
        capturedGroupView = Array.AsReadOnly((CapturedGroup[])capturedGroups.Clone());
        capturedStoneInstanceView = Array.AsReadOnly(
            capturedGroups.SelectMany(group => group.StoneInstances).ToArray());
        ContainsKing = capturedGroups.Any(group => group.ContainsKing);
        NonKingBlackStoneCount = capturedGroups
            .SelectMany(group => group.StoneInstances)
            .Count(instance => instance.Color == StoneColor.Black && !instance.IsKing);
        CanonicalText = CreateCanonicalText();
    }

    public string BatchId { get; }

    public string ReasonId { get; }

    public CaptureBoundary Boundary { get; }

    public int? BoundaryEnemyTurnIndex { get; }

    public CapturingWindow CapturingWindow { get; }

    public IReadOnlyList<CapturedGroup> CapturedGroups => capturedGroupView;

    public IReadOnlyList<StoneRuntimeInstance> CapturedStoneInstances =>
        capturedStoneInstanceView;

    public bool ContainsKing { get; }

    public int NonKingBlackStoneCount { get; }

    public string CanonicalText { get; }

    public static CaptureBatch Create(
        string batchId,
        string reasonId,
        CaptureBoundary boundary,
        int? boundaryEnemyTurnIndex,
        CapturingWindow capturingWindow,
        StoneRuntimeState sourceStones,
        IEnumerable<StoneGroup> capturedGroups)
    {
        var canonicalBatchId = StableDomainId.Validate(batchId, nameof(batchId));
        var canonicalReasonId = StableDomainId.Validate(reasonId, nameof(reasonId));
        ArgumentNullException.ThrowIfNull(sourceStones);
        ArgumentNullException.ThrowIfNull(capturedGroups);
        ValidateBoundary(boundary, boundaryEnemyTurnIndex);
        ValidateCapturingWindow(capturingWindow);

        var sourceGroupAnalysis = StoneGroupAnalyzer.Analyze(sourceStones.SourceBoard);
        var suppliedGroups = capturedGroups.ToArray();
        if (suppliedGroups.Length == 0)
        {
            throw new ArgumentException(
                "A capture batch must contain at least one captured group.",
                nameof(capturedGroups));
        }

        foreach (var group in suppliedGroups)
        {
            ArgumentNullException.ThrowIfNull(group);
        }

        Array.Sort(suppliedGroups, (left, right) => left.Anchor.CompareTo(right.Anchor));
        var seenAnchors = new HashSet<CanonicalPoint>();
        var seenInstanceIds = new HashSet<string>(StringComparer.Ordinal);
        var canonicalGroups = new CapturedGroup[suppliedGroups.Length];
        for (var groupIndex = 0; groupIndex < suppliedGroups.Length; groupIndex++)
        {
            var supplied = suppliedGroups[groupIndex];
            if (!seenAnchors.Add(supplied.Anchor))
            {
                throw new ArgumentException(
                    $"Capture batch contains duplicate group anchor {supplied.Anchor}.",
                    nameof(capturedGroups));
            }

            var canonicalSourceGroup = sourceGroupAnalysis.GroupAt(supplied.Anchor);
            if (canonicalSourceGroup is null ||
                canonicalSourceGroup.Color != supplied.Color ||
                !canonicalSourceGroup.StonePoints.SequenceEqual(supplied.StonePoints) ||
                supplied.Stones.Any(stone =>
                    !ReferenceEquals(
                        sourceStones.SourceBoard.StoneAt(stone.Point),
                        stone)))
            {
                throw new ArgumentException(
                    $"Captured group at {supplied.Anchor} does not belong to the source stone runtime.",
                    nameof(capturedGroups));
            }

            var instances = new StoneRuntimeInstance[canonicalSourceGroup.Stones.Count];
            for (var stoneIndex = 0; stoneIndex < canonicalSourceGroup.Stones.Count; stoneIndex++)
            {
                var point = canonicalSourceGroup.StonePoints[stoneIndex];
                var instance = sourceStones.InstanceAt(point)
                    ?? throw new ArgumentException(
                        $"Captured point {point} has no source stone runtime instance.",
                        nameof(sourceStones));
                if (!seenInstanceIds.Add(instance.InstanceId))
                {
                    throw new ArgumentException(
                        $"Capture batch contains duplicate stone instance {instance.InstanceId}.",
                        nameof(capturedGroups));
                }

                instances[stoneIndex] = instance;
            }

            canonicalGroups[groupIndex] = new CapturedGroup(canonicalSourceGroup, instances);
        }

        return new CaptureBatch(
            canonicalBatchId,
            canonicalReasonId,
            boundary,
            boundaryEnemyTurnIndex,
            capturingWindow,
            canonicalGroups);
    }

    private static void ValidateBoundary(
        CaptureBoundary boundary,
        int? boundaryEnemyTurnIndex)
    {
        switch (boundary)
        {
            case CaptureBoundary.PlacementResolution when boundaryEnemyTurnIndex is null:
                return;
            case CaptureBoundary.EnemyTurnTemporaryLibertyExpirySweep
                when boundaryEnemyTurnIndex is > 0:
                return;
            case CaptureBoundary.PlacementResolution:
                throw new ArgumentException(
                    "Placement capture batches cannot carry an enemy-turn index.",
                    nameof(boundaryEnemyTurnIndex));
            case CaptureBoundary.EnemyTurnTemporaryLibertyExpirySweep:
                throw new ArgumentOutOfRangeException(
                    nameof(boundaryEnemyTurnIndex),
                    boundaryEnemyTurnIndex,
                    "Temporary-liberty expiry capture batches require a positive enemy-turn index.");
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(boundary),
                    boundary,
                    "Unknown capture boundary.");
        }
    }

    private static void ValidateCapturingWindow(CapturingWindow capturingWindow)
    {
        if (capturingWindow is not CapturingWindow.PlayerActionWindow and
            not CapturingWindow.ClosedPlayerWindow)
        {
            throw new ArgumentOutOfRangeException(
                nameof(capturingWindow),
                capturingWindow,
                "Unknown capturing window.");
        }
    }

    private string CreateCanonicalText()
    {
        var lines = new List<string>
        {
            EncodingVersion,
            $"batch_id={EncodeStableText(BatchId)}",
            $"reason_id={EncodeStableText(ReasonId)}",
            $"boundary={BoundaryId(Boundary)}",
            $"boundary_enemy_turn_index={BoundaryEnemyTurnIndex?.ToString(CultureInfo.InvariantCulture) ?? "none"}",
            $"capturing_window={CapturingWindowId(CapturingWindow)}",
            $"contains_king={(ContainsKing ? "1" : "0")}",
            $"nonking_black_stone_count={NonKingBlackStoneCount.ToString(CultureInfo.InvariantCulture)}",
            $"captured_group_count={capturedGroupView.Count.ToString(CultureInfo.InvariantCulture)}",
        };

        for (var groupIndex = 0; groupIndex < capturedGroupView.Count; groupIndex++)
        {
            var group = capturedGroupView[groupIndex];
            lines.Add($"captured_group_index={groupIndex.ToString(CultureInfo.InvariantCulture)}");
            lines.Add($"captured_color={ColorId(group.Color)}");
            lines.Add($"capturing_color={ColorId(group.CapturingColor)}");
            lines.Add(
                $"group_anchor={group.GroupAnchor.X.ToString(CultureInfo.InvariantCulture)},{group.GroupAnchor.Y.ToString(CultureInfo.InvariantCulture)}");
            lines.Add($"group_contains_king={(group.ContainsKing ? "1" : "0")}");
            lines.Add(
                $"group_stone_count={group.StoneInstances.Count.ToString(CultureInfo.InvariantCulture)}");
            foreach (var instance in group.StoneInstances)
            {
                lines.Add(
                    $"captured_stone={EncodeStableText(instance.InstanceId)}:" +
                    $"{EncodeStableText(instance.KindId)}:" +
                    $"{instance.Point.X.ToString(CultureInfo.InvariantCulture)}," +
                    $"{instance.Point.Y.ToString(CultureInfo.InvariantCulture)}:" +
                    $"king={(instance.IsKing ? "1" : "0")}:" +
                    $"created_sequence={instance.CreatedSequence.ToString(CultureInfo.InvariantCulture)}");
                lines.Add(
                    $"captured_stone_effect_metadata_count={instance.OrderedEffectMetadata.Count.ToString(CultureInfo.InvariantCulture)}");
                foreach (var metadata in instance.OrderedEffectMetadata)
                {
                    lines.Add(
                        $"captured_stone_effect_metadata={EncodeStableText(metadata)}");
                }
            }
        }

        return string.Join('\n', lines);
    }

    private static string BoundaryId(CaptureBoundary boundary) => boundary switch
    {
        CaptureBoundary.PlacementResolution => "placement_resolution",
        CaptureBoundary.EnemyTurnTemporaryLibertyExpirySweep =>
            "enemy_turn_temporary_liberty_expiry_sweep",
        _ => throw new InvalidOperationException("Capture batch contains an unknown boundary."),
    };

    private static string CapturingWindowId(CapturingWindow capturingWindow) =>
        capturingWindow switch
        {
            CapturingWindow.PlayerActionWindow => "player_action_window",
            CapturingWindow.ClosedPlayerWindow => "closed_player_window",
            _ => throw new InvalidOperationException(
                "Capture batch contains an unknown capturing window."),
        };

    private static string ColorId(StoneColor color) => color switch
    {
        StoneColor.Black => "black",
        StoneColor.White => "white",
        _ => throw new InvalidOperationException("Capture batch contains an unknown color."),
    };

    private static string EncodeStableText(string value) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
}
