using System.Collections.ObjectModel;
using System.Globalization;

using Igorogue.Domain.Board;
using Igorogue.Domain.Content;
using Igorogue.Domain.Facilities;

namespace Igorogue.Domain.Enemies;

public enum EnemyTargetReferenceKind : byte
{
    StoneGroup = 1,
}

public sealed class EnemyTargetReference : IEquatable<EnemyTargetReference>
{
    public EnemyTargetReference(StoneColor color, CanonicalPoint anchor)
    {
        if (color is not StoneColor.Black and not StoneColor.White)
        {
            throw new ArgumentOutOfRangeException(nameof(color), color, "Unknown target color.");
        }

        ArgumentNullException.ThrowIfNull(anchor);
        Kind = EnemyTargetReferenceKind.StoneGroup;
        Color = color;
        Anchor = anchor;
    }

    public EnemyTargetReferenceKind Kind { get; }

    public StoneColor Color { get; }

    public CanonicalPoint Anchor { get; }

    public string ToCanonicalText() =>
        "enemy-target-reference-v1\n" +
        "kind=stone_group\n" +
        $"color={(Color == StoneColor.Black ? "black" : "white")}\n" +
        $"anchor={Anchor.X.ToString(CultureInfo.InvariantCulture)},{Anchor.Y.ToString(CultureInfo.InvariantCulture)}";

    public bool Equals(EnemyTargetReference? other) =>
        other is not null &&
        Kind == other.Kind &&
        Color == other.Color &&
        Anchor.Equals(other.Anchor);

    public override bool Equals(object? obj) => Equals(obj as EnemyTargetReference);

    public override int GetHashCode() => HashCode.Combine(Kind, Color, Anchor);

    public override string ToString() => ToCanonicalText();
}

public sealed class BanditPlacementCandidate
{
    private readonly ReadOnlyCollection<StoneGroup> capturedGroupView;

    internal BanditPlacementCandidate(
        EnemyIntentKind intentKind,
        StoneGroup targetGroup,
        CanonicalPoint point,
        PlacementAccessMode accessMode,
        RuntimeStonePlacementEvaluation placementEvaluation,
        FacilityPlacementCommit facilityPlacementCommit,
        TerritoryAnalysis territoryAfter,
        FacilityOperatingTransition facilityTransitionAfter,
        IEnumerable<StoneGroup> capturedGroups,
        int placedGroupEffectiveLiberties,
        int? whiteKingEffectiveLibertiesAfter,
        int? blackKingEffectiveLibertiesAfter,
        int connectedOtherWhiteGroupCount,
        int targetGroupToBlackKingDistance,
        int distanceToBlackKingAdvanceTarget,
        int distanceToCenter)
    {
        if (!Enum.IsDefined(intentKind))
        {
            throw new ArgumentOutOfRangeException(nameof(intentKind), intentKind, "Unknown enemy intent.");
        }

        ArgumentNullException.ThrowIfNull(targetGroup);
        ArgumentNullException.ThrowIfNull(point);
        ArgumentNullException.ThrowIfNull(placementEvaluation);
        ArgumentNullException.ThrowIfNull(facilityPlacementCommit);
        ArgumentNullException.ThrowIfNull(territoryAfter);
        ArgumentNullException.ThrowIfNull(facilityTransitionAfter);
        ArgumentNullException.ThrowIfNull(capturedGroups);
        if (!placementEvaluation.Accepted ||
            placementEvaluation.LegalPlacementCommit is null ||
            placementEvaluation.RuntimePlacementCommit is null ||
            placementEvaluation.PostCaptureEffectiveLiberties is null)
        {
            throw new ArgumentException(
                "Bandit candidate requires an accepted exact placement projection.",
                nameof(placementEvaluation));
        }

        if (!ReferenceEquals(
                facilityPlacementCommit.Candidate,
                placementEvaluation.LegalPlacementCommit.Candidate) ||
            !ReferenceEquals(territoryAfter.SourceBoard, facilityPlacementCommit.BoardAfterCommit) ||
            !ReferenceEquals(
                facilityTransitionAfter.AnalysisAfter.TerritoryAnalysis,
                territoryAfter))
        {
            throw new ArgumentException(
                "Bandit candidate facility and territory projections must share the exact placement.",
                nameof(facilityPlacementCommit));
        }

        if (placedGroupEffectiveLiberties < 0 ||
            whiteKingEffectiveLibertiesAfter < 0 ||
            blackKingEffectiveLibertiesAfter < 0 ||
            connectedOtherWhiteGroupCount < 0 ||
            targetGroupToBlackKingDistance < 0 ||
            distanceToBlackKingAdvanceTarget < 0 ||
            distanceToCenter < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(placedGroupEffectiveLiberties),
                "Bandit candidate metrics cannot be negative.");
        }

        IntentKind = intentKind;
        TargetGroup = targetGroup;
        TargetReference = new EnemyTargetReference(targetGroup.Color, targetGroup.Anchor);
        Point = point;
        AccessMode = accessMode;
        PlacementEvaluation = placementEvaluation;
        FacilityPlacementCommit = facilityPlacementCommit;
        TerritoryAfter = territoryAfter;
        FacilityTransitionAfter = facilityTransitionAfter;
        capturedGroupView = Array.AsReadOnly(capturedGroups.ToArray());
        PlacedGroupEffectiveLiberties = placedGroupEffectiveLiberties;
        WhiteKingEffectiveLibertiesAfter = whiteKingEffectiveLibertiesAfter;
        BlackKingEffectiveLibertiesAfter = blackKingEffectiveLibertiesAfter;
        ConnectedOtherWhiteGroupCount = connectedOtherWhiteGroupCount;
        TargetGroupToBlackKingDistance = targetGroupToBlackKingDistance;
        DistanceToBlackKingAdvanceTarget = distanceToBlackKingAdvanceTarget;
        DistanceToCenter = distanceToCenter;
    }

    public EnemyIntentKind IntentKind { get; }

    public StoneGroup TargetGroup { get; }

    public EnemyTargetReference TargetReference { get; }

    public CanonicalPoint Point { get; }

    public PlacementAccessMode AccessMode { get; }

    public RuntimeStonePlacementEvaluation PlacementEvaluation { get; }

    public FacilityPlacementCommit FacilityPlacementCommit { get; }

    public TerritoryAnalysis TerritoryAfter { get; }

    public FacilityOperatingTransition FacilityTransitionAfter { get; }

    public IReadOnlyList<StoneGroup> CapturedGroups => capturedGroupView;

    public int CapturedStoneCount => capturedGroupView.Sum(group => group.Stones.Count);

    public int PlacedGroupEffectiveLiberties { get; }

    public int? WhiteKingEffectiveLibertiesAfter { get; }

    public int? BlackKingEffectiveLibertiesAfter { get; }

    public int ConnectedOtherWhiteGroupCount { get; }

    public int TargetGroupToBlackKingDistance { get; }

    public int DistanceToBlackKingAdvanceTarget { get; }

    public int DistanceToCenter { get; }
}
