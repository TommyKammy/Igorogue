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
