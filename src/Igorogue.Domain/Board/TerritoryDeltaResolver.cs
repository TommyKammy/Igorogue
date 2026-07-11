using System.Collections.ObjectModel;
using Igorogue.Domain.Combat;
using Igorogue.Domain.Facilities;

namespace Igorogue.Domain.Board;

public sealed class TerritoryEstablishedFact : IBattleFact
{
    private readonly ReadOnlyCollection<CanonicalPoint> changedPointView;

    internal TerritoryEstablishedFact(
        StoneColor sourceActor,
        CanonicalPoint[] canonicalChangedPoints)
    {
        if (sourceActor is not StoneColor.Black and not StoneColor.White)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sourceActor),
                sourceActor,
                "Unknown territory-establishment source actor.");
        }

        ArgumentNullException.ThrowIfNull(canonicalChangedPoints);
        if (canonicalChangedPoints.Length == 0)
        {
            throw new ArgumentException(
                "Territory establishment requires at least one changed point.",
                nameof(canonicalChangedPoints));
        }

        var changedPoints = (CanonicalPoint[])canonicalChangedPoints.Clone();
        for (var index = 0; index < changedPoints.Length; index++)
        {
            ArgumentNullException.ThrowIfNull(changedPoints[index]);
            if (index > 0 && changedPoints[index - 1].CompareTo(changedPoints[index]) >= 0)
            {
                throw new ArgumentException(
                    "Territory establishment points must be unique and in canonical order.",
                    nameof(canonicalChangedPoints));
            }
        }

        SourceActor = sourceActor;
        changedPointView = Array.AsReadOnly(changedPoints);
    }

    public StoneColor SourceActor { get; }

    public IReadOnlyList<CanonicalPoint> ChangedPoints => changedPointView;
}

public static class TerritoryDeltaResolver
{
    public static TerritoryEstablishedFact? Resolve(
        TerritoryAnalysis before,
        TerritoryAnalysis after,
        FacilityPlacementCommit placementCommit,
        StoneColor sourceActor)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);
        ArgumentNullException.ThrowIfNull(placementCommit);
        if (!ReferenceEquals(
                before.SourceBoard,
                placementCommit.Candidate.SourceBoard))
        {
            throw new ArgumentException(
                "Before territory analysis must belong to the placement commit's exact source board.",
                nameof(before));
        }

        if (!ReferenceEquals(
                after.SourceBoard,
                placementCommit.BoardAfterCommit))
        {
            throw new ArgumentException(
                "After territory analysis must belong to the placement commit's exact result board.",
                nameof(after));
        }

        return ResolveCore(before, after, sourceActor);
    }

    internal static TerritoryEstablishedFact? ResolveCore(
        TerritoryAnalysis before,
        TerritoryAnalysis after,
        StoneColor sourceActor)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);
        if (sourceActor is not StoneColor.Black and not StoneColor.White)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sourceActor),
                sourceActor,
                "Unknown territory-delta source actor.");
        }

        if (!ReferenceEquals(before.SourceBoard.Geometry, after.SourceBoard.Geometry))
        {
            throw new ArgumentException(
                "Territory analyses must use the exact same board geometry.",
                nameof(after));
        }

        var changedPoints = new List<CanonicalPoint>();
        foreach (var point in after.SourceBoard.Geometry.CanonicalPoints)
        {
            var ownerAfter = after.RegionAt(point)?.Owner;
            if (ownerAfter != TerritoryOwner.Black)
            {
                continue;
            }

            var ownerBefore = before.RegionAt(point)?.Owner;
            if (ownerBefore != TerritoryOwner.Black)
            {
                changedPoints.Add(point);
            }
        }

        return changedPoints.Count == 0
            ? null
            : new TerritoryEstablishedFact(sourceActor, changedPoints.ToArray());
    }
}
