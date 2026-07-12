using System.Collections.ObjectModel;
using Igorogue.Domain.Combat;
using Igorogue.Domain.Facilities;

namespace Igorogue.Domain.Board;

public enum TerritoryEstablishmentSourceKind : byte
{
    Placement = 1,
    TemporaryLibertyExpiry = 2,
}

public sealed class TerritoryEstablishedFact : IBattleFact
{
    private readonly ReadOnlyCollection<CanonicalPoint> changedPointView;

    internal TerritoryEstablishedFact(
        StoneColor sourceActor,
        CanonicalPoint[] canonicalChangedPoints)
        : this(
            sourceActor,
            TerritoryEstablishmentSourceKind.Placement,
            TerritoryDeltaResolver.StonePlacementSourceReasonId,
            sourceActor == StoneColor.Black,
            canonicalChangedPoints)
    {
    }

    internal TerritoryEstablishedFact(
        StoneColor sourceActor,
        TerritoryEstablishmentSourceKind sourceKind,
        string sourceReasonId,
        bool implicitMomentumEligible,
        CanonicalPoint[] canonicalChangedPoints)
    {
        if (sourceActor is not StoneColor.Black and not StoneColor.White)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sourceActor),
                sourceActor,
                "Unknown territory-establishment source actor.");
        }

        if (sourceKind is < TerritoryEstablishmentSourceKind.Placement or
            > TerritoryEstablishmentSourceKind.TemporaryLibertyExpiry)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sourceKind),
                sourceKind,
                "Unknown territory-establishment source kind.");
        }

        SourceReasonId = StableDomainId.Validate(
            sourceReasonId,
            nameof(sourceReasonId));
        if (sourceKind == TerritoryEstablishmentSourceKind.TemporaryLibertyExpiry &&
            implicitMomentumEligible)
        {
            throw new ArgumentException(
                "Mandatory temporary-liberty expiry cannot be eligible for implicit Momentum.",
                nameof(implicitMomentumEligible));
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
        SourceKind = sourceKind;
        ImplicitMomentumEligible = implicitMomentumEligible;
        changedPointView = Array.AsReadOnly(changedPoints);
    }

    public StoneColor SourceActor { get; }

    public TerritoryEstablishmentSourceKind SourceKind { get; }

    public string SourceReasonId { get; }

    public bool ImplicitMomentumEligible { get; }

    public IReadOnlyList<CanonicalPoint> ChangedPoints => changedPointView;
}

public static class TerritoryDeltaResolver
{
    public const string StonePlacementSourceReasonId = "stone_placement";

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

    public static TerritoryEstablishedFact? ResolveAfterExpiry(
        TerritoryAnalysis before,
        TemporaryLibertyExpiryResolution expiry)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(expiry);
        if (!ReferenceEquals(before.SourceBoard, expiry.SourceStones.SourceBoard))
        {
            throw new ArgumentException(
                "Before territory analysis must belong to the expiry resolution's exact source board.",
                nameof(before));
        }

        if (!ReferenceEquals(
                expiry.TerritoryAfterResolution.SourceBoard,
                expiry.BoardAfterResolution))
        {
            throw new ArgumentException(
                "Expiry territory analysis must belong to the expiry resolution's exact result board.",
                nameof(expiry));
        }

        return ResolveCore(
            before,
            expiry.TerritoryAfterResolution,
            StoneColor.White,
            TerritoryEstablishmentSourceKind.TemporaryLibertyExpiry,
            TemporaryLibertyExpiryResolver.TopologySourceReasonId,
            implicitMomentumEligible: false);
    }

    internal static TerritoryEstablishedFact? ResolveCore(
        TerritoryAnalysis before,
        TerritoryAnalysis after,
        StoneColor sourceActor) => ResolveCore(
            before,
            after,
            sourceActor,
            TerritoryEstablishmentSourceKind.Placement,
            StonePlacementSourceReasonId,
            sourceActor == StoneColor.Black);

    private static TerritoryEstablishedFact? ResolveCore(
        TerritoryAnalysis before,
        TerritoryAnalysis after,
        StoneColor sourceActor,
        TerritoryEstablishmentSourceKind sourceKind,
        string sourceReasonId,
        bool implicitMomentumEligible)
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
            : new TerritoryEstablishedFact(
                sourceActor,
                sourceKind,
                sourceReasonId,
                implicitMomentumEligible,
                changedPoints.ToArray());
    }
}
