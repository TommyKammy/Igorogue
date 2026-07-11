using System.Diagnostics.CodeAnalysis;

namespace Igorogue.Domain.Board;

public static class HypotheticalPlacementResolver
{
    public static bool TryCreate(
        BoardState source,
        BoardStone proposedStone,
        [NotNullWhen(true)] out HypotheticalPlacement? placement)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(proposedStone);
        placement = null;

        if (!source.IsEmpty(proposedStone.Point))
        {
            return false;
        }

        var boardAfterPlacement = BoardState.Create(
            source.Geometry,
            source.OccupiedStones.Append(proposedStone));
        var groupsAfterPlacement = StoneGroupAnalyzer.Analyze(boardAfterPlacement);
        var adjacentOpponentGroups = FindAdjacentOpponentGroups(
            proposedStone,
            groupsAfterPlacement);
        placement = new HypotheticalPlacement(
            source,
            proposedStone,
            boardAfterPlacement,
            groupsAfterPlacement,
            adjacentOpponentGroups);
        return true;
    }

    public static HypotheticalPlacementResolution ResolveCaptures(
        HypotheticalPlacement placement,
        EffectiveLibertySnapshot effectiveLiberties)
    {
        ArgumentNullException.ThrowIfNull(placement);
        ArgumentNullException.ThrowIfNull(effectiveLiberties);
        if (!ReferenceEquals(
                placement.GroupsAfterPlacement,
                effectiveLiberties.GroupAnalysis))
        {
            throw new ArgumentException(
                "Effective liberties must belong to the placement's exact post-placement snapshot.",
                nameof(effectiveLiberties));
        }

        var capturedGroups = placement.AdjacentOpponentGroups
            .Where(group => effectiveLiberties.EffectiveLibertiesFor(group) == 0)
            .ToArray();
        var capturedPointMask = new bool[placement.BoardAfterPlacement.Geometry.PointCount];
        foreach (var group in capturedGroups)
        {
            foreach (var stone in group.Stones)
            {
                capturedPointMask[placement.BoardAfterPlacement.Geometry
                    .ToCanonicalIndex(stone.Point)] = true;
            }
        }

        var boardAfterCapture = BoardState.Create(
            placement.BoardAfterPlacement.Geometry,
            placement.BoardAfterPlacement.OccupiedStones.Where(stone =>
                !capturedPointMask[placement.BoardAfterPlacement.Geometry
                    .ToCanonicalIndex(stone.Point)]));
        var groupsAfterCapture = StoneGroupAnalyzer.Analyze(boardAfterCapture);
        var placedGroupAfterCapture = groupsAfterCapture.GroupAt(placement.PlacedStone.Point)
            ?? throw new InvalidOperationException("Placed stone must remain after opponent capture.");
        var orderedFacts = CreateOrderedFacts(placement.PlacedStone, capturedGroups);

        return new HypotheticalPlacementResolution(
            placement.SourceBoard,
            placement.PlacedStone,
            boardAfterCapture,
            capturedGroups,
            groupsAfterCapture,
            placedGroupAfterCapture,
            orderedFacts);
    }

    private static StoneGroup[] FindAdjacentOpponentGroups(
        BoardStone proposedStone,
        StoneGroupAnalysis groupsAfterPlacement)
    {
        var geometry = groupsAfterPlacement.Geometry;
        var candidateAnchorMask = new bool[geometry.PointCount];
        foreach (var neighbour in geometry.GetOrthogonalNeighbours(proposedStone.Point))
        {
            var group = groupsAfterPlacement.GroupAt(neighbour);
            if (group is not null && group.Color != proposedStone.Color)
            {
                candidateAnchorMask[geometry.ToCanonicalIndex(group.Anchor)] = true;
            }
        }

        return groupsAfterPlacement.Groups
            .Where(group => candidateAnchorMask[geometry.ToCanonicalIndex(group.Anchor)])
            .ToArray();
    }

    private static PlacementCaptureFact[] CreateOrderedFacts(
        BoardStone placedStone,
        IReadOnlyList<StoneGroup> capturedGroups)
    {
        var facts = new PlacementCaptureFact[capturedGroups.Count + 1];
        facts[0] = new StonePlacedFact(placedStone);
        for (var index = 0; index < capturedGroups.Count; index++)
        {
            facts[index + 1] = new GroupCapturedFact(capturedGroups[index], placedStone.Color);
        }

        return facts;
    }
}
