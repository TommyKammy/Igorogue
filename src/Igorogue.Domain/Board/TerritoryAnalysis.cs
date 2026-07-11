using System.Collections.ObjectModel;

namespace Igorogue.Domain.Board;

public sealed class TerritoryAnalysis
{
    private readonly ReadOnlyCollection<TerritoryRegion> regionView;
    private readonly TerritoryRegion?[] regionsByCanonicalIndex;

    internal TerritoryAnalysis(
        BoardState sourceBoard,
        TerritoryRegion[] orderedRegions,
        TerritoryRegion?[] regionsByCanonicalIndex)
    {
        ArgumentNullException.ThrowIfNull(sourceBoard);
        ArgumentNullException.ThrowIfNull(orderedRegions);
        ArgumentNullException.ThrowIfNull(regionsByCanonicalIndex);
        if (regionsByCanonicalIndex.Length != sourceBoard.Geometry.PointCount)
        {
            throw new ArgumentException(
                "Territory lookup must contain one entry per board point.",
                nameof(regionsByCanonicalIndex));
        }

        var regions = (TerritoryRegion[])orderedRegions.Clone();
        foreach (var region in regions)
        {
            ArgumentNullException.ThrowIfNull(region);
        }

        SourceBoard = sourceBoard;
        regionView = Array.AsReadOnly(regions);
        this.regionsByCanonicalIndex = (TerritoryRegion?[])regionsByCanonicalIndex.Clone();
    }

    public BoardState SourceBoard { get; }

    public IReadOnlyList<TerritoryRegion> Regions => regionView;

    public TerritoryRegion? RegionAt(CanonicalPoint point)
    {
        var index = SourceBoard.Geometry.ToCanonicalIndex(point);
        return regionsByCanonicalIndex[index];
    }
}
