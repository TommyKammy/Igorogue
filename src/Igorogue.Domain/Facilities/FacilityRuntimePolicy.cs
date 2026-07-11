using System.Collections.ObjectModel;

using Igorogue.Domain.Board;

namespace Igorogue.Domain.Facilities;

public sealed class FacilityCapacityBand
{
    public FacilityCapacityBand(int minSize, int maxSize, int slots)
    {
        if (minSize <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(minSize),
                minSize,
                "Facility capacity band minimum size must be positive.");
        }

        if (maxSize < minSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxSize),
                maxSize,
                "Facility capacity band maximum size must not be below its minimum.");
        }

        if (slots <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(slots),
                slots,
                "Facility capacity slots must be positive.");
        }

        MinSize = minSize;
        MaxSize = maxSize;
        Slots = slots;
    }

    public int MinSize { get; }

    public int MaxSize { get; }

    public int Slots { get; }
}

public sealed class FacilityRuntimePolicy
{
    private const string DefaultTypeId = "default";
    private static readonly int MaximumTerritorySize =
        BoardGeometry.AcceptedSize * BoardGeometry.AcceptedSize;

    private readonly ReadOnlyCollection<FacilityCapacityBand> capacityBandView;
    private readonly ReadOnlyDictionary<string, int> typeLimitView;

    private FacilityRuntimePolicy(
        int territoryIncomeDivisor,
        FacilityCapacityBand[] capacityBands,
        int slotCap,
        SortedDictionary<string, int> typeLimits)
    {
        TerritoryIncomeDivisor = territoryIncomeDivisor;
        capacityBandView = Array.AsReadOnly((FacilityCapacityBand[])capacityBands.Clone());
        SlotCap = slotCap;
        typeLimitView = new ReadOnlyDictionary<string, int>(
            new SortedDictionary<string, int>(typeLimits, StringComparer.Ordinal));
    }

    public int TerritoryIncomeDivisor { get; }

    public IReadOnlyList<FacilityCapacityBand> CapacityBands => capacityBandView;

    public int SlotCap { get; }

    public IReadOnlyDictionary<string, int> TypeLimits => typeLimitView;

    public static FacilityRuntimePolicy Create(
        int territoryIncomeDivisor,
        IEnumerable<FacilityCapacityBand> capacityBands,
        int slotCap,
        IEnumerable<KeyValuePair<string, int>> typeLimits)
    {
        if (territoryIncomeDivisor <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(territoryIncomeDivisor),
                territoryIncomeDivisor,
                "Territory income divisor must be positive.");
        }

        ArgumentNullException.ThrowIfNull(capacityBands);
        if (slotCap <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(slotCap),
                slotCap,
                "Facility slot cap must be positive.");
        }

        var canonicalBands = capacityBands.ToArray();
        foreach (var band in canonicalBands)
        {
            ArgumentNullException.ThrowIfNull(band);
        }

        Array.Sort(
            canonicalBands,
            (left, right) =>
            {
                var minimumComparison = left.MinSize.CompareTo(right.MinSize);
                return minimumComparison != 0
                    ? minimumComparison
                    : left.MaxSize.CompareTo(right.MaxSize);
            });
        ValidateCapacityBands(canonicalBands, slotCap);

        ArgumentNullException.ThrowIfNull(typeLimits);
        var canonicalTypeLimits = new SortedDictionary<string, int>(StringComparer.Ordinal);
        foreach (var pair in typeLimits)
        {
            var contentId = FacilityInstance.ValidateStableId(pair.Key, nameof(typeLimits));
            if (pair.Value <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(typeLimits),
                    pair.Value,
                    $"Facility type limit for {contentId} must be positive.");
            }

            if (!canonicalTypeLimits.TryAdd(contentId, pair.Value))
            {
                throw new ArgumentException(
                    $"Facility policy contains duplicate type limit {contentId}.",
                    nameof(typeLimits));
            }
        }

        if (!canonicalTypeLimits.ContainsKey(DefaultTypeId))
        {
            throw new ArgumentException(
                "Facility policy must define the default type limit.",
                nameof(typeLimits));
        }

        return new FacilityRuntimePolicy(
            territoryIncomeDivisor,
            canonicalBands,
            slotCap,
            canonicalTypeLimits);
    }

    public int TerritoryIncomeForSize(int territorySize)
    {
        ValidateTerritorySize(territorySize);
        return ((territorySize - 1) / TerritoryIncomeDivisor) + 1;
    }

    public int BaseCapacityForSize(int territorySize)
    {
        ValidateTerritorySize(territorySize);
        foreach (var band in capacityBandView)
        {
            if (territorySize >= band.MinSize && territorySize <= band.MaxSize)
            {
                return band.Slots;
            }
        }

        throw new InvalidOperationException("Facility capacity policy does not cover the territory size.");
    }

    public int EffectiveCapacityForSize(int territorySize, int nonnegativeModifier)
    {
        if (nonnegativeModifier < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(nonnegativeModifier),
                nonnegativeModifier,
                "Facility capacity modifier cannot be negative.");
        }

        var requested = (long)BaseCapacityForSize(territorySize) + nonnegativeModifier;
        return (int)Math.Min(requested, SlotCap);
    }

    public int TypeLimitFor(string contentId)
    {
        var stableContentId = FacilityInstance.ValidateStableId(contentId, nameof(contentId));
        return typeLimitView.TryGetValue(stableContentId, out var explicitLimit)
            ? explicitLimit
            : typeLimitView[DefaultTypeId];
    }

    private static void ValidateCapacityBands(
        IReadOnlyList<FacilityCapacityBand> capacityBands,
        int slotCap)
    {
        if (capacityBands.Count == 0)
        {
            throw new ArgumentException(
                "Facility capacity policy must contain at least one band.",
                nameof(capacityBands));
        }

        var expectedMinimum = 1;
        foreach (var band in capacityBands)
        {
            if (band.MinSize != expectedMinimum)
            {
                throw new ArgumentException(
                    "Facility capacity bands must be contiguous and start at territory size 1.",
                    nameof(capacityBands));
            }

            if (band.MaxSize > MaximumTerritorySize)
            {
                throw new ArgumentException(
                    $"Facility capacity bands cannot exceed territory size {MaximumTerritorySize}.",
                    nameof(capacityBands));
            }

            if (band.Slots > slotCap)
            {
                throw new ArgumentException(
                    "Base facility capacity cannot exceed the global slot cap.",
                    nameof(capacityBands));
            }

            expectedMinimum = checked(band.MaxSize + 1);
        }

        if (expectedMinimum != MaximumTerritorySize + 1)
        {
            throw new ArgumentException(
                $"Facility capacity bands must cover every territory size through {MaximumTerritorySize}.",
                nameof(capacityBands));
        }
    }

    private static void ValidateTerritorySize(int territorySize)
    {
        if (territorySize <= 0 || territorySize > MaximumTerritorySize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(territorySize),
                territorySize,
                $"Territory size must be between 1 and {MaximumTerritorySize}.");
        }
    }
}
