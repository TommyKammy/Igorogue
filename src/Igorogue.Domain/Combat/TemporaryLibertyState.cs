using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

using Igorogue.Domain.Board;

namespace Igorogue.Domain.Combat;

public sealed class TemporaryLibertyEffect
{
    public TemporaryLibertyEffect(
        string effectInstanceId,
        int amount,
        StoneColor ownerColor,
        string anchorStoneInstanceId,
        string sourceId,
        long createdSequence,
        int expiresAfterEnemyTurnIndex)
    {
        EffectInstanceId = StableDomainId.Validate(
            effectInstanceId,
            nameof(effectInstanceId));
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                amount,
                "Temporary liberty amount must be positive.");
        }

        if (ownerColor is not StoneColor.Black and not StoneColor.White)
        {
            throw new ArgumentOutOfRangeException(
                nameof(ownerColor),
                ownerColor,
                "Unknown temporary liberty owner color.");
        }

        AnchorStoneInstanceId = StableDomainId.Validate(
            anchorStoneInstanceId,
            nameof(anchorStoneInstanceId));
        SourceId = StableDomainId.Validate(sourceId, nameof(sourceId));
        if (createdSequence <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(createdSequence),
                createdSequence,
                "Temporary liberty created sequence must be positive.");
        }

        if (expiresAfterEnemyTurnIndex <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(expiresAfterEnemyTurnIndex),
                expiresAfterEnemyTurnIndex,
                "Temporary liberty expiry turn must be positive.");
        }

        Amount = amount;
        OwnerColor = ownerColor;
        CreatedSequence = createdSequence;
        ExpiresAfterEnemyTurnIndex = expiresAfterEnemyTurnIndex;
    }

    public string EffectInstanceId { get; }

    public int Amount { get; }

    public StoneColor OwnerColor { get; }

    public string AnchorStoneInstanceId { get; }

    public string SourceId { get; }

    public long CreatedSequence { get; }

    public int ExpiresAfterEnemyTurnIndex { get; }
}

public sealed class TemporaryLibertyState
{
    public const string EncodingVersion = "temporary-liberty-state-v1";

    private readonly ReadOnlyCollection<TemporaryLibertyEffect> effectView;
    private readonly Dictionary<string, TemporaryLibertyEffect> effectsById;

    private TemporaryLibertyState(
        StoneRuntimeState sourceStones,
        TemporaryLibertyEffect[] canonicalEffects,
        Dictionary<string, TemporaryLibertyEffect> effectsById,
        long nextCreatedSequence,
        int? expirySweepStartedForEnemyTurnIndex)
    {
        SourceStones = sourceStones;
        effectView = Array.AsReadOnly((TemporaryLibertyEffect[])canonicalEffects.Clone());
        this.effectsById = new Dictionary<string, TemporaryLibertyEffect>(
            effectsById,
            StringComparer.Ordinal);
        NextCreatedSequence = nextCreatedSequence;
        ExpirySweepStartedForEnemyTurnIndex = expirySweepStartedForEnemyTurnIndex;
    }

    public StoneRuntimeState SourceStones { get; }

    public IReadOnlyList<TemporaryLibertyEffect> Effects => effectView;

    public long NextCreatedSequence { get; }

    public int? ExpirySweepStartedForEnemyTurnIndex { get; }

    public static TemporaryLibertyState Create(
        StoneRuntimeState sourceStones,
        IEnumerable<TemporaryLibertyEffect> effects,
        long nextCreatedSequence,
        int? expirySweepStartedForEnemyTurnIndex = null)
    {
        ArgumentNullException.ThrowIfNull(sourceStones);
        ArgumentNullException.ThrowIfNull(effects);
        if (nextCreatedSequence <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(nextCreatedSequence),
                nextCreatedSequence,
                "Next temporary liberty created sequence must be positive.");
        }

        if (expirySweepStartedForEnemyTurnIndex <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(expirySweepStartedForEnemyTurnIndex),
                expirySweepStartedForEnemyTurnIndex,
                "Expiry sweep marker must be a positive enemy-turn index.");
        }

        var canonicalEffects = effects.ToArray();
        var byId = new Dictionary<string, TemporaryLibertyEffect>(StringComparer.Ordinal);
        var maximumCreatedSequence = 0L;
        foreach (var effect in canonicalEffects)
        {
            ArgumentNullException.ThrowIfNull(effect);
            var anchor = sourceStones.InstanceById(effect.AnchorStoneInstanceId)
                ?? throw new ArgumentException(
                    $"Temporary liberty effect {effect.EffectInstanceId} has no live anchor stone {effect.AnchorStoneInstanceId}.",
                    nameof(effects));
            if (anchor.Color != effect.OwnerColor)
            {
                throw new ArgumentException(
                    $"Temporary liberty effect {effect.EffectInstanceId} owner does not match its anchor stone.",
                    nameof(effects));
            }

            if (!byId.TryAdd(effect.EffectInstanceId, effect))
            {
                throw new ArgumentException(
                    $"Temporary liberty state contains duplicate effect ID {effect.EffectInstanceId}.",
                    nameof(effects));
            }

            maximumCreatedSequence = Math.Max(maximumCreatedSequence, effect.CreatedSequence);
        }

        if (nextCreatedSequence <= maximumCreatedSequence)
        {
            throw new ArgumentOutOfRangeException(
                nameof(nextCreatedSequence),
                nextCreatedSequence,
                "Next temporary liberty created sequence must exceed every live effect sequence.");
        }

        if (expirySweepStartedForEnemyTurnIndex is int sweepMarker &&
            canonicalEffects.Any(effect =>
                effect.ExpiresAfterEnemyTurnIndex <= sweepMarker))
        {
            throw new ArgumentException(
                "A sweep-marked temporary liberty state cannot retain effects due at or before the completed boundary.",
                nameof(effects));
        }

        Array.Sort(
            canonicalEffects,
            (left, right) =>
            {
                var sequenceComparison = left.CreatedSequence.CompareTo(right.CreatedSequence);
                return sequenceComparison != 0
                    ? sequenceComparison
                    : StringComparer.Ordinal.Compare(
                        left.EffectInstanceId,
                        right.EffectInstanceId);
            });

        return new TemporaryLibertyState(
            sourceStones,
            canonicalEffects,
            byId,
            nextCreatedSequence,
            expirySweepStartedForEnemyTurnIndex);
    }

    public TemporaryLibertyEffect? EffectById(string effectInstanceId)
    {
        StableDomainId.Validate(effectInstanceId, nameof(effectInstanceId));
        return effectsById.GetValueOrDefault(effectInstanceId);
    }

    internal TemporaryLibertyState ReplaceEffects(
        StoneRuntimeState sourceStones,
        IEnumerable<TemporaryLibertyEffect> effects,
        int? expirySweepStartedForEnemyTurnIndex) =>
        Create(
            sourceStones,
            effects,
            NextCreatedSequence,
            expirySweepStartedForEnemyTurnIndex);

    internal TemporaryLibertyState AppendEffect(TemporaryLibertyEffect effect) =>
        Create(
            SourceStones,
            effectView.Append(effect),
            checked(NextCreatedSequence + 1L),
            ExpirySweepStartedForEnemyTurnIndex);

    public string ToCanonicalText()
    {
        var lines = new List<string>(4 + (effectView.Count * 8))
        {
            EncodingVersion,
            $"next_created_sequence={NextCreatedSequence.ToString(CultureInfo.InvariantCulture)}",
            $"sweep_started_for={ExpirySweepStartedForEnemyTurnIndex?.ToString(CultureInfo.InvariantCulture) ?? "none"}",
            $"effect_count={effectView.Count.ToString(CultureInfo.InvariantCulture)}",
        };

        for (var index = 0; index < effectView.Count; index++)
        {
            var effect = effectView[index];
            lines.Add($"effect_index={index.ToString(CultureInfo.InvariantCulture)}");
            lines.Add($"effect_id={EncodeStableText(effect.EffectInstanceId)}");
            lines.Add($"amount={effect.Amount.ToString(CultureInfo.InvariantCulture)}");
            lines.Add($"owner={ColorId(effect.OwnerColor)}");
            lines.Add($"anchor_id={EncodeStableText(effect.AnchorStoneInstanceId)}");
            lines.Add($"source_id={EncodeStableText(effect.SourceId)}");
            lines.Add($"created_sequence={effect.CreatedSequence.ToString(CultureInfo.InvariantCulture)}");
            lines.Add(
                $"expires_after_enemy_turn={effect.ExpiresAfterEnemyTurnIndex.ToString(CultureInfo.InvariantCulture)}");
        }

        return string.Join('\n', lines);
    }

    internal static string EncodeStableText(string value) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(value));

    internal static string ColorId(StoneColor color) => color switch
    {
        StoneColor.Black => "black",
        StoneColor.White => "white",
        _ => throw new InvalidOperationException("Unknown temporary liberty color."),
    };
}

public sealed class TemporaryLibertyGrantResolution
{
    internal TemporaryLibertyGrantResolution(
        TemporaryLibertyState sourceState,
        TemporaryLibertyState stateAfterGrant,
        TemporaryLibertyEffect grantedEffect,
        TemporaryLibertyGrantedFact grantedFact,
        TemporaryLibertyExpirySweepWindow? continuedSweepWindow = null)
    {
        SourceState = sourceState;
        StateAfterGrant = stateAfterGrant;
        GrantedEffect = grantedEffect;
        GrantedFact = grantedFact;
        ContinuedSweepWindow = continuedSweepWindow;
    }

    public TemporaryLibertyState SourceState { get; }

    public TemporaryLibertyState StateAfterGrant { get; }

    public TemporaryLibertyEffect GrantedEffect { get; }

    public TemporaryLibertyGrantedFact GrantedFact { get; }

    public TemporaryLibertyExpirySweepWindow? ContinuedSweepWindow { get; }
}

public static class TemporaryLibertyGrantResolver
{
    public static TemporaryLibertyGrantResolution Grant(
        TemporaryLibertyState sourceState,
        CanonicalPoint targetPoint,
        string effectInstanceId,
        int amount,
        string sourceId,
        int currentEnemyTurnIndex)
    {
        ArgumentNullException.ThrowIfNull(sourceState);
        ArgumentNullException.ThrowIfNull(targetPoint);
        if (currentEnemyTurnIndex <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(currentEnemyTurnIndex),
                currentEnemyTurnIndex,
                "Enemy-turn index must be positive.");
        }

        if (sourceState.ExpirySweepStartedForEnemyTurnIndex is int marker &&
            marker > currentEnemyTurnIndex)
        {
            throw new ArgumentException(
                "Current enemy-turn index cannot precede the stored sweep marker.",
                nameof(currentEnemyTurnIndex));
        }

        if (sourceState.ExpirySweepStartedForEnemyTurnIndex == currentEnemyTurnIndex)
        {
            throw new ArgumentException(
                "A completed current-boundary sweep cannot accept a before-sweep grant.",
                nameof(currentEnemyTurnIndex));
        }

        return GrantCore(
            sourceState,
            targetPoint,
            effectInstanceId,
            amount,
            sourceId,
            currentEnemyTurnIndex);
    }

    public static TemporaryLibertyGrantResolution GrantAfterExpirySweepStarted(
        TemporaryLibertyState sourceState,
        CanonicalPoint targetPoint,
        string effectInstanceId,
        int amount,
        string sourceId,
        TemporaryLibertyExpirySweepWindow sweepWindow)
    {
        ArgumentNullException.ThrowIfNull(sourceState);
        ArgumentNullException.ThrowIfNull(sweepWindow);
        if (sweepWindow.Terminal)
        {
            throw new InvalidOperationException(
                "A terminal expiry sweep cannot grant later temporary liberties.");
        }

        if (!ReferenceEquals(
                sourceState,
                sweepWindow.TemporaryLibertiesAfterSweep))
        {
            throw new ArgumentException(
                "Post-sweep grants require the exact temporary-liberty state produced by the sweep or prior chained grant.",
                nameof(sourceState));
        }

        var grant = GrantCore(
            sourceState,
            targetPoint,
            effectInstanceId,
            amount,
            sourceId,
            checked(sweepWindow.EnemyTurnIndex + 1));
        var stateAfterGrant =
            grant.StateAfterGrant.ExpirySweepStartedForEnemyTurnIndex ==
                sweepWindow.EnemyTurnIndex
                ? grant.StateAfterGrant
                : TemporaryLibertyState.Create(
                    grant.StateAfterGrant.SourceStones,
                    grant.StateAfterGrant.Effects,
                    grant.StateAfterGrant.NextCreatedSequence,
                    sweepWindow.EnemyTurnIndex);
        return new TemporaryLibertyGrantResolution(
            grant.SourceState,
            stateAfterGrant,
            grant.GrantedEffect,
            grant.GrantedFact,
            sweepWindow.ContinueWith(stateAfterGrant));
    }

    private static TemporaryLibertyGrantResolution GrantCore(
        TemporaryLibertyState sourceState,
        CanonicalPoint targetPoint,
        string effectInstanceId,
        int amount,
        string sourceId,
        int expiryTurn)
    {
        ArgumentNullException.ThrowIfNull(sourceState);
        ArgumentNullException.ThrowIfNull(targetPoint);

        var group = StoneGroupAnalyzer
            .Analyze(sourceState.SourceStones.SourceBoard)
            .GroupAt(targetPoint)
            ?? throw new ArgumentException(
                "Temporary liberty target must be an occupied stone group.",
                nameof(targetPoint));
        var anchorStone = group.Stones[0];
        var anchorRuntime = sourceState.SourceStones.InstanceAt(anchorStone.Point)
            ?? throw new InvalidOperationException(
                "Every occupied group stone must have a runtime instance.");
        var effect = new TemporaryLibertyEffect(
            effectInstanceId,
            amount,
            group.Color,
            anchorRuntime.InstanceId,
            sourceId,
            sourceState.NextCreatedSequence,
            expiryTurn);
        var stateAfterGrant = sourceState.AppendEffect(effect);
        var fact = new TemporaryLibertyGrantedFact(effect, group.Anchor);

        return new TemporaryLibertyGrantResolution(
            sourceState,
            stateAfterGrant,
            effect,
            fact);
    }
}
