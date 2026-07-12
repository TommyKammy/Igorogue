using System.Collections.ObjectModel;
using System.Globalization;

using Igorogue.Domain.Board;

namespace Igorogue.Domain.Combat;

public sealed class ContinuousLibertyModifier
{
    public ContinuousLibertyModifier(
        string modifierInstanceId,
        int amount,
        StoneColor ownerColor,
        string anchorStoneInstanceId,
        string sourceId)
    {
        ModifierInstanceId = StableDomainId.Validate(
            modifierInstanceId,
            nameof(modifierInstanceId));
        if (ownerColor is not StoneColor.Black and not StoneColor.White)
        {
            throw new ArgumentOutOfRangeException(
                nameof(ownerColor),
                ownerColor,
                "Unknown continuous liberty owner color.");
        }

        AnchorStoneInstanceId = StableDomainId.Validate(
            anchorStoneInstanceId,
            nameof(anchorStoneInstanceId));
        SourceId = StableDomainId.Validate(sourceId, nameof(sourceId));
        Amount = amount;
        OwnerColor = ownerColor;
    }

    public string ModifierInstanceId { get; }

    public int Amount { get; }

    public StoneColor OwnerColor { get; }

    public string AnchorStoneInstanceId { get; }

    public string SourceId { get; }
}

public sealed class ContinuousLibertySnapshot
{
    public const string EncodingVersion = "continuous-liberty-snapshot-v1";

    private readonly ReadOnlyCollection<ContinuousLibertyModifier> modifierView;

    private ContinuousLibertySnapshot(
        StoneRuntimeState sourceStones,
        ContinuousLibertyModifier[] canonicalModifiers)
    {
        SourceStones = sourceStones;
        modifierView = Array.AsReadOnly(
            (ContinuousLibertyModifier[])canonicalModifiers.Clone());
    }

    public StoneRuntimeState SourceStones { get; }

    public IReadOnlyList<ContinuousLibertyModifier> Modifiers => modifierView;

    public static ContinuousLibertySnapshot Empty(StoneRuntimeState sourceStones) =>
        Create(sourceStones, []);

    public static ContinuousLibertySnapshot Create(
        StoneRuntimeState sourceStones,
        IEnumerable<ContinuousLibertyModifier> modifiers)
    {
        ArgumentNullException.ThrowIfNull(sourceStones);
        ArgumentNullException.ThrowIfNull(modifiers);

        var canonicalModifiers = modifiers.ToArray();
        var modifierIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var modifier in canonicalModifiers)
        {
            ArgumentNullException.ThrowIfNull(modifier);
            if (!modifierIds.Add(modifier.ModifierInstanceId))
            {
                throw new ArgumentException(
                    $"Continuous liberty snapshot contains duplicate modifier ID {modifier.ModifierInstanceId}.",
                    nameof(modifiers));
            }

            var anchor = sourceStones.InstanceById(modifier.AnchorStoneInstanceId)
                ?? throw new ArgumentException(
                    $"Continuous modifier {modifier.ModifierInstanceId} has no live anchor stone {modifier.AnchorStoneInstanceId}.",
                    nameof(modifiers));
            if (anchor.Color != modifier.OwnerColor)
            {
                throw new ArgumentException(
                    $"Continuous modifier {modifier.ModifierInstanceId} owner does not match its anchor stone.",
                    nameof(modifiers));
            }
        }

        Array.Sort(
            canonicalModifiers,
            (left, right) => StringComparer.Ordinal.Compare(
                left.ModifierInstanceId,
                right.ModifierInstanceId));
        return new ContinuousLibertySnapshot(sourceStones, canonicalModifiers);
    }

    public ContinuousLibertySnapshot Rebind(StoneRuntimeState resultStones)
    {
        ArgumentNullException.ThrowIfNull(resultStones);
        if (ReferenceEquals(SourceStones, resultStones))
        {
            return this;
        }

        if (!ReferenceEquals(
                SourceStones.SourceBoard.Geometry,
                resultStones.SourceBoard.Geometry))
        {
            throw new ArgumentException(
                "Continuous liberty rebind requires the exact source geometry.",
                nameof(resultStones));
        }

        foreach (var sourceInstance in SourceStones.Instances)
        {
            var resultById = resultStones.InstanceById(sourceInstance.InstanceId);
            if (resultById is not null && !ReferenceEquals(resultById, sourceInstance))
            {
                throw new ArgumentException(
                    $"Continuous liberty rebind replaced stone runtime identity {sourceInstance.InstanceId}.",
                    nameof(resultStones));
            }

            var resultAtPoint = resultStones.InstanceAt(sourceInstance.Point);
            if (resultAtPoint is not null && !ReferenceEquals(resultAtPoint, sourceInstance))
            {
                throw new ArgumentException(
                    $"Continuous liberty rebind replaced the source stone at {sourceInstance.Point}.",
                    nameof(resultStones));
            }

            if (ReferenceEquals(
                    resultStones.SourceBoard.StoneAt(sourceInstance.Point),
                    sourceInstance.Stone) &&
                !ReferenceEquals(resultAtPoint, sourceInstance))
            {
                throw new ArgumentException(
                    $"Continuous liberty rebind recreated surviving stone runtime identity {sourceInstance.InstanceId}.",
                    nameof(resultStones));
            }
        }

        var introducedInstances = resultStones.Instances
            .Where(resultInstance =>
                SourceStones.InstanceById(resultInstance.InstanceId) is null)
            .ToArray();
        if (introducedInstances.Length > 1)
        {
            throw new ArgumentException(
                "Continuous liberty rebind permits at most one newly placed stone runtime instance.",
                nameof(resultStones));
        }

        if (introducedInstances.Length == 0)
        {
            if (resultStones.NextCreatedSequence != SourceStones.NextCreatedSequence)
            {
                throw new ArgumentException(
                    "Removal-only continuous liberty rebind must preserve the next stone sequence.",
                    nameof(resultStones));
            }
        }
        else
        {
            var introduced = introducedInstances[0];
            if (SourceStones.InstanceAt(introduced.Point) is not null ||
                introduced.CreatedSequence != SourceStones.NextCreatedSequence ||
                resultStones.NextCreatedSequence !=
                    checked(SourceStones.NextCreatedSequence + 1L))
            {
                throw new ArgumentException(
                    "Continuous liberty rebind introduced a foreign or out-of-sequence stone runtime instance.",
                    nameof(resultStones));
            }
        }

        var retainedModifiers = modifierView.Where(modifier =>
        {
            var sourceAnchor = SourceStones.InstanceById(modifier.AnchorStoneInstanceId)
                ?? throw new InvalidOperationException(
                    $"Continuous modifier {modifier.ModifierInstanceId} lost its source anchor.");
            var resultAnchor = resultStones.InstanceById(modifier.AnchorStoneInstanceId);
            return ReferenceEquals(resultAnchor, sourceAnchor);
        });
        return Create(resultStones, retainedModifiers);
    }

    public string ToCanonicalText()
    {
        var lines = new List<string>(2 + (modifierView.Count * 6))
        {
            EncodingVersion,
            $"modifier_count={modifierView.Count.ToString(CultureInfo.InvariantCulture)}",
        };
        for (var index = 0; index < modifierView.Count; index++)
        {
            var modifier = modifierView[index];
            lines.Add($"modifier_index={index.ToString(CultureInfo.InvariantCulture)}");
            lines.Add(
                $"modifier_id={TemporaryLibertyState.EncodeStableText(modifier.ModifierInstanceId)}");
            lines.Add($"amount={modifier.Amount.ToString(CultureInfo.InvariantCulture)}");
            lines.Add($"owner={TemporaryLibertyState.ColorId(modifier.OwnerColor)}");
            lines.Add(
                $"anchor_id={TemporaryLibertyState.EncodeStableText(modifier.AnchorStoneInstanceId)}");
            lines.Add($"source_id={TemporaryLibertyState.EncodeStableText(modifier.SourceId)}");
        }

        return string.Join('\n', lines);
    }
}
