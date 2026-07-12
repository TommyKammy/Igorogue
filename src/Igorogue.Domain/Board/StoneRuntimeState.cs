using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

namespace Igorogue.Domain.Board;

public sealed class StoneRuntimeInstance
{
    private readonly ReadOnlyCollection<string> effectMetadataView;

    public StoneRuntimeInstance(
        string instanceId,
        BoardStone stone,
        string kindId,
        long createdSequence,
        IEnumerable<string> orderedEffectMetadata)
    {
        InstanceId = StableDomainId.Validate(instanceId, nameof(instanceId));
        ArgumentNullException.ThrowIfNull(stone);
        KindId = StableDomainId.Validate(kindId, nameof(kindId));
        if (createdSequence <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(createdSequence),
                createdSequence,
                "Stone created sequence must be positive.");
        }

        ArgumentNullException.ThrowIfNull(orderedEffectMetadata);
        var metadata = orderedEffectMetadata
            .Select(value => StableDomainId.Validate(value, nameof(orderedEffectMetadata)))
            .ToArray();

        Stone = stone;
        CreatedSequence = createdSequence;
        effectMetadataView = Array.AsReadOnly(metadata);
    }

    public string InstanceId { get; }

    public BoardStone Stone { get; }

    public CanonicalPoint Point => Stone.Point;

    public StoneColor Color => Stone.Color;

    public bool IsKing => Stone.IsKing;

    public string KindId { get; }

    public long CreatedSequence { get; }

    public IReadOnlyList<string> OrderedEffectMetadata => effectMetadataView;
}

public sealed class StoneRuntimeState
{
    public const string EncodingVersion = "stone-runtime-state-v1";

    private readonly ReadOnlyCollection<StoneRuntimeInstance> instanceView;
    private readonly StoneRuntimeInstance?[] instancesByCanonicalIndex;
    private readonly Dictionary<string, StoneRuntimeInstance> instancesById;

    private StoneRuntimeState(
        BoardState sourceBoard,
        StoneRuntimeInstance[] canonicalInstances,
        StoneRuntimeInstance?[] instancesByCanonicalIndex,
        Dictionary<string, StoneRuntimeInstance> instancesById,
        long nextCreatedSequence)
    {
        SourceBoard = sourceBoard;
        instanceView = Array.AsReadOnly((StoneRuntimeInstance[])canonicalInstances.Clone());
        this.instancesByCanonicalIndex =
            (StoneRuntimeInstance?[])instancesByCanonicalIndex.Clone();
        this.instancesById = new Dictionary<string, StoneRuntimeInstance>(
            instancesById,
            StringComparer.Ordinal);
        NextCreatedSequence = nextCreatedSequence;
    }

    public BoardState SourceBoard { get; }

    public IReadOnlyList<StoneRuntimeInstance> Instances => instanceView;

    public long NextCreatedSequence { get; }

    public static StoneRuntimeState Create(
        BoardState sourceBoard,
        IEnumerable<StoneRuntimeInstance> instances,
        long nextCreatedSequence)
    {
        ArgumentNullException.ThrowIfNull(sourceBoard);
        ArgumentNullException.ThrowIfNull(instances);
        if (nextCreatedSequence <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(nextCreatedSequence),
                nextCreatedSequence,
                "Next stone created sequence must be positive.");
        }

        var canonicalInstances = instances.ToArray();
        var byPoint = new StoneRuntimeInstance?[sourceBoard.Geometry.PointCount];
        var byId = new Dictionary<string, StoneRuntimeInstance>(StringComparer.Ordinal);
        var maximumCreatedSequence = 0L;

        foreach (var instance in canonicalInstances)
        {
            ArgumentNullException.ThrowIfNull(instance);
            var pointIndex = sourceBoard.Geometry.ToCanonicalIndex(instance.Point);
            if (!ReferenceEquals(sourceBoard.StoneAt(instance.Point), instance.Stone))
            {
                throw new ArgumentException(
                    $"Stone runtime instance {instance.InstanceId} does not belong to the supplied board at {instance.Point}.",
                    nameof(instances));
            }

            if (byPoint[pointIndex] is not null)
            {
                throw new ArgumentException(
                    $"Stone runtime state contains duplicate point {instance.Point}.",
                    nameof(instances));
            }

            if (!byId.TryAdd(instance.InstanceId, instance))
            {
                throw new ArgumentException(
                    $"Stone runtime state contains duplicate instance ID {instance.InstanceId}.",
                    nameof(instances));
            }

            byPoint[pointIndex] = instance;
            maximumCreatedSequence = Math.Max(maximumCreatedSequence, instance.CreatedSequence);
        }

        if (canonicalInstances.Length != sourceBoard.OccupiedStones.Count)
        {
            throw new ArgumentException(
                "Stone runtime state must contain exactly one instance for every occupied board point.",
                nameof(instances));
        }

        if (nextCreatedSequence <= maximumCreatedSequence)
        {
            throw new ArgumentOutOfRangeException(
                nameof(nextCreatedSequence),
                nextCreatedSequence,
                "Next stone created sequence must be greater than every live stone sequence.");
        }

        Array.Sort(
            canonicalInstances,
            (left, right) => sourceBoard.Geometry.ToCanonicalIndex(left.Point)
                .CompareTo(sourceBoard.Geometry.ToCanonicalIndex(right.Point)));

        return new StoneRuntimeState(
            sourceBoard,
            canonicalInstances,
            byPoint,
            byId,
            nextCreatedSequence);
    }

    public StoneRuntimeInstance? InstanceAt(CanonicalPoint point)
    {
        var index = SourceBoard.Geometry.ToCanonicalIndex(point);
        return instancesByCanonicalIndex[index];
    }

    public StoneRuntimeInstance? InstanceById(string instanceId)
    {
        StableDomainId.Validate(instanceId, nameof(instanceId));
        return instancesById.GetValueOrDefault(instanceId);
    }

    internal StoneRuntimeState RebindAfterRemoval(BoardState resultBoard)
    {
        ArgumentNullException.ThrowIfNull(resultBoard);
        if (!ReferenceEquals(SourceBoard.Geometry, resultBoard.Geometry))
        {
            throw new ArgumentException(
                "Result board must use the exact source geometry.",
                nameof(resultBoard));
        }

        var retained = instanceView
            .Where(instance => ReferenceEquals(resultBoard.StoneAt(instance.Point), instance.Stone))
            .ToArray();
        if (retained.Length != resultBoard.OccupiedStones.Count)
        {
            throw new ArgumentException(
                "Removal rebind cannot introduce or replace board stones.",
                nameof(resultBoard));
        }

        return Create(resultBoard, retained, NextCreatedSequence);
    }

    public string ToCanonicalText()
    {
        var lines = new List<string>(3 + (instanceView.Count * 9))
        {
            EncodingVersion,
            $"next_created_sequence={NextCreatedSequence.ToString(CultureInfo.InvariantCulture)}",
            $"instance_count={instanceView.Count.ToString(CultureInfo.InvariantCulture)}",
        };

        for (var index = 0; index < instanceView.Count; index++)
        {
            var instance = instanceView[index];
            lines.Add($"instance_index={index.ToString(CultureInfo.InvariantCulture)}");
            lines.Add($"instance_id={EncodeStableText(instance.InstanceId)}");
            lines.Add($"kind_id={EncodeStableText(instance.KindId)}");
            lines.Add($"color={ColorId(instance.Color)}");
            lines.Add($"king={(instance.IsKing ? 1 : 0).ToString(CultureInfo.InvariantCulture)}");
            lines.Add(
                $"point={instance.Point.X.ToString(CultureInfo.InvariantCulture)},{instance.Point.Y.ToString(CultureInfo.InvariantCulture)}");
            lines.Add($"created_sequence={instance.CreatedSequence.ToString(CultureInfo.InvariantCulture)}");
            lines.Add(
                $"effect_metadata_count={instance.OrderedEffectMetadata.Count.ToString(CultureInfo.InvariantCulture)}");
            foreach (var metadata in instance.OrderedEffectMetadata)
            {
                lines.Add($"effect_metadata={EncodeStableText(metadata)}");
            }
        }

        return string.Join('\n', lines);
    }

    private static string EncodeStableText(string value) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(value));

    private static string ColorId(StoneColor color) => color switch
    {
        StoneColor.Black => "black",
        StoneColor.White => "white",
        _ => throw new InvalidOperationException("Stone runtime state contains an unknown color."),
    };
}
