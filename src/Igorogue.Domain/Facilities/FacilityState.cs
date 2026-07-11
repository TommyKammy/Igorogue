using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

using Igorogue.Domain.Board;

namespace Igorogue.Domain.Facilities;

public sealed class FacilityState
{
    public const string EncodingVersion = "facility-state-v1";

    private readonly ReadOnlyCollection<FacilityInstance> installedFacilityView;
    private readonly FacilityInstance?[] facilitiesByCanonicalIndex;
    private readonly Dictionary<string, FacilityInstance> facilitiesById;

    private FacilityState(
        BoardState sourceBoard,
        FacilityInstance[] canonicalFacilities,
        FacilityInstance?[] facilitiesByCanonicalIndex,
        Dictionary<string, FacilityInstance> facilitiesById,
        long nextBuildSequence)
    {
        SourceBoard = sourceBoard;
        installedFacilityView = Array.AsReadOnly((FacilityInstance[])canonicalFacilities.Clone());
        this.facilitiesByCanonicalIndex =
            (FacilityInstance?[])facilitiesByCanonicalIndex.Clone();
        this.facilitiesById = new Dictionary<string, FacilityInstance>(
            facilitiesById,
            StringComparer.Ordinal);
        NextBuildSequence = nextBuildSequence;
    }

    public BoardState SourceBoard { get; }

    public IReadOnlyList<FacilityInstance> InstalledFacilities => installedFacilityView;

    public long NextBuildSequence { get; }

    public static FacilityState Create(
        BoardState sourceBoard,
        IEnumerable<FacilityInstance> installedFacilities,
        long nextBuildSequence)
    {
        ArgumentNullException.ThrowIfNull(sourceBoard);
        ArgumentNullException.ThrowIfNull(installedFacilities);
        if (nextBuildSequence <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(nextBuildSequence),
                nextBuildSequence,
                "Next facility build sequence must be positive.");
        }

        var facilities = installedFacilities.ToArray();
        var byPoint = new FacilityInstance?[sourceBoard.Geometry.PointCount];
        var byId = new Dictionary<string, FacilityInstance>(StringComparer.Ordinal);
        var buildSequences = new HashSet<long>();
        var maximumBuildSequence = 0L;

        foreach (var facility in facilities)
        {
            ArgumentNullException.ThrowIfNull(facility);
            var pointIndex = sourceBoard.Geometry.ToCanonicalIndex(facility.Point);
            if (!sourceBoard.IsEmpty(facility.Point))
            {
                throw new ArgumentException(
                    $"Facility {facility.InstanceId} cannot coexist with a stone at {facility.Point}.",
                    nameof(installedFacilities));
            }

            if (byPoint[pointIndex] is not null)
            {
                throw new ArgumentException(
                    $"Facility state contains duplicate point {facility.Point}.",
                    nameof(installedFacilities));
            }

            if (!byId.TryAdd(facility.InstanceId, facility))
            {
                throw new ArgumentException(
                    $"Facility state contains duplicate instance ID {facility.InstanceId}.",
                    nameof(installedFacilities));
            }

            if (!buildSequences.Add(facility.BuildSequence))
            {
                throw new ArgumentException(
                    $"Facility state contains duplicate build sequence {facility.BuildSequence.ToString(CultureInfo.InvariantCulture)}.",
                    nameof(installedFacilities));
            }

            byPoint[pointIndex] = facility;
            maximumBuildSequence = Math.Max(maximumBuildSequence, facility.BuildSequence);
        }

        if (nextBuildSequence <= maximumBuildSequence)
        {
            throw new ArgumentOutOfRangeException(
                nameof(nextBuildSequence),
                nextBuildSequence,
                "Next facility build sequence must be greater than every installed build sequence.");
        }

        Array.Sort(
            facilities,
            (left, right) =>
            {
                var pointComparison = sourceBoard.Geometry.ToCanonicalIndex(left.Point)
                    .CompareTo(sourceBoard.Geometry.ToCanonicalIndex(right.Point));
                return pointComparison != 0
                    ? pointComparison
                    : StringComparer.Ordinal.Compare(left.InstanceId, right.InstanceId);
            });

        return new FacilityState(sourceBoard, facilities, byPoint, byId, nextBuildSequence);
    }

    public FacilityInstance? FacilityAt(CanonicalPoint point)
    {
        var index = SourceBoard.Geometry.ToCanonicalIndex(point);
        return facilitiesByCanonicalIndex[index];
    }

    public FacilityInstance? FacilityById(string instanceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);
        return facilitiesById.GetValueOrDefault(instanceId);
    }

    public string ToCanonicalText()
    {
        var lines = new List<string>(4 + (installedFacilityView.Count * 8))
        {
            EncodingVersion,
            $"next_build_sequence={NextBuildSequence.ToString(CultureInfo.InvariantCulture)}",
            $"installed_count={installedFacilityView.Count.ToString(CultureInfo.InvariantCulture)}",
        };

        for (var index = 0; index < installedFacilityView.Count; index++)
        {
            var facility = installedFacilityView[index];
            lines.Add($"facility_index={index.ToString(CultureInfo.InvariantCulture)}");
            lines.Add($"instance_id={EncodeStableText(facility.InstanceId)}");
            lines.Add($"content_id={EncodeStableText(facility.ContentId)}");
            lines.Add($"owner={OwnerId(facility.Owner)}");
            lines.Add(
                $"point={facility.Point.X.ToString(CultureInfo.InvariantCulture)},{facility.Point.Y.ToString(CultureInfo.InvariantCulture)}");
            lines.Add($"build_sequence={facility.BuildSequence.ToString(CultureInfo.InvariantCulture)}");
            lines.Add(
                $"explicit_disable_count={facility.ExplicitDisableSources.Count.ToString(CultureInfo.InvariantCulture)}");
            foreach (var source in facility.ExplicitDisableSources)
            {
                lines.Add($"explicit_disable_source={EncodeStableText(source)}");
            }
        }

        return string.Join('\n', lines);
    }

    private static string EncodeStableText(string value) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(value));

    private static string OwnerId(StoneColor owner) => owner switch
    {
        StoneColor.Black => "black",
        StoneColor.White => "white",
        _ => throw new InvalidOperationException("Facility state contains an unknown owner color."),
    };
}
