using System.Collections.ObjectModel;

using Igorogue.Domain.Board;

namespace Igorogue.Application.Battle;

internal sealed class StarterStonePlacementEffectAnalysis
{
    private readonly ReadOnlyCollection<StoneGroup> affectedEnemyAtariGroupView;

    private StarterStonePlacementEffectAnalysis(
        LegalPlacementCommit legalPlacement,
        TemporaryLibertyEffectiveLibertyAnalysis postCaptureAnalysis,
        StoneGroup placedGroup,
        StoneGroup[] affectedEnemyAtariGroups)
    {
        LegalPlacement = legalPlacement;
        PostCaptureAnalysis = postCaptureAnalysis;
        PlacedGroup = placedGroup;
        affectedEnemyAtariGroupView = Array.AsReadOnly(
            (StoneGroup[])affectedEnemyAtariGroups.Clone());
    }

    internal LegalPlacementCommit LegalPlacement { get; }

    internal TemporaryLibertyEffectiveLibertyAnalysis PostCaptureAnalysis { get; }

    internal StoneGroup PlacedGroup { get; }

    internal int PlacedGroupRealLibertyCount => PlacedGroup.RealLibertyCount;

    internal IReadOnlyList<StoneGroup> AffectedEnemyAtariGroups =>
        affectedEnemyAtariGroupView;

    internal bool EstablishedEnemyAtari => affectedEnemyAtariGroupView.Count > 0;

    internal static StarterStonePlacementEffectAnalysis Create(
        LegalPlacementCommit legalPlacement,
        TemporaryLibertyEffectiveLibertyAnalysis postCaptureAnalysis)
    {
        ArgumentNullException.ThrowIfNull(legalPlacement);
        ArgumentNullException.ThrowIfNull(postCaptureAnalysis);
        var candidate = legalPlacement.Candidate;
        if (!ReferenceEquals(
                candidate.BoardAfterCapture,
                postCaptureAnalysis.SourceStones.SourceBoard) ||
            !ReferenceEquals(
                candidate.GroupsAfterCapture,
                postCaptureAnalysis.GroupAnalysis))
        {
            throw new ArgumentException(
                "Starter-stone effects require the exact accepted post-capture analysis.",
                nameof(postCaptureAnalysis));
        }

        var placedPoint = candidate.PlacedStone.Point;
        var placedGroup = postCaptureAnalysis.GroupAnalysis.GroupAt(placedPoint)
            ?? throw new InvalidOperationException(
                "A legal starter-stone placement must retain its placed group.");
        var atariGroups = new List<StoneGroup>();
        var seenAnchors = new HashSet<CanonicalPoint>();
        var sourceGroups = StoneGroupAnalyzer.Analyze(candidate.SourceBoard);
        var affectedOpponentGroups = candidate.SourceBoard.Geometry
            .GetOrthogonalNeighbours(placedPoint)
            .Select(sourceGroups.GroupAt)
            .Where(group => group?.Color == StoneColor.White)
            .Cast<StoneGroup>()
            .DistinctBy(group => group.Anchor)
            .OrderBy(group => group.Anchor)
            .ToArray();
        foreach (var affectedBeforeCapture in affectedOpponentGroups)
        {
            var survivingPoint = affectedBeforeCapture.Stones
                .Where(stone => ReferenceEquals(
                    candidate.BoardAfterCapture.StoneAt(stone.Point),
                    stone))
                .Select(stone => stone.Point)
                .FirstOrDefault();
            if (survivingPoint is null)
            {
                continue;
            }

            var survivingGroup = postCaptureAnalysis.GroupAnalysis.GroupAt(survivingPoint)
                ?? throw new InvalidOperationException(
                    "A surviving affected enemy stone must retain a final group.");
            if (seenAnchors.Add(survivingGroup.Anchor) &&
                postCaptureAnalysis
                    .BreakdownFor(survivingGroup)
                    .EffectiveLibertyCount == 1)
            {
                atariGroups.Add(survivingGroup);
            }
        }

        atariGroups.Sort((left, right) => left.Anchor.CompareTo(right.Anchor));
        return new StarterStonePlacementEffectAnalysis(
            legalPlacement,
            postCaptureAnalysis,
            placedGroup,
            atariGroups.ToArray());
    }
}
