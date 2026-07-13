using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

using Igorogue.Domain.Board;
using Igorogue.Domain.Combat;
using Igorogue.Domain.Content;
using Igorogue.Domain.Determinism;

namespace Igorogue.Domain.Cards;

/// <summary>
/// Typed, immutable projection of the supported starter Reinforce operation shape.
/// The behavior is selected from target and ordered operations rather than content ID.
/// </summary>
public sealed class StarterReinforceCardPlayDefinition
{
    public const string EncodingVersion = "starter-reinforce-card-play-definition-v1";

    private readonly ReadOnlyCollection<CardOperationDefinition> effectView;

    private StarterReinforceCardPlayDefinition(CardContentDefinition content)
    {
        ContentId = content.Id;
        Cost = content.Cost;
        effectView = Array.AsReadOnly(content.Effects.ToArray());
        DrawIfTargetAtari = (DrawIfTargetAtariOperationDefinition)effectView[0];
        TemporaryLiberty = (TemporaryLibertyOperationDefinition)effectView[1];
        CanonicalText = CreateCanonicalText();
        Checksum = DeterministicChecksum.Sha256Hex(CanonicalText);
    }

    public string ContentId { get; }

    public int Cost { get; }

    public CardTargetKind Target => CardTargetKind.FriendlyGroup;

    public DrawIfTargetAtariOperationDefinition DrawIfTargetAtari { get; }

    public TemporaryLibertyOperationDefinition TemporaryLiberty { get; }

    public IReadOnlyList<CardOperationDefinition> Effects => effectView;

    public string CanonicalText { get; }

    public string Checksum { get; }

    public static StarterReinforceCardPlayDefinition Create(
        CardContentDefinition content)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (content.Rarity != CardRarity.Starter ||
            content.Type != CardContentType.Technique ||
            content.Target != CardTargetKind.FriendlyGroup ||
            content.PlacementTags.Count != 0 ||
            content.Effects.Count != 2 ||
            content.Effects[0] is not DrawIfTargetAtariOperationDefinition ||
            content.Effects[1] is not TemporaryLibertyOperationDefinition temporary ||
            temporary.DurationKind != TemporaryLibertyDurationKind.EnemyTurnEnd ||
            temporary.Timing !=
                TemporaryLibertyTiming.FirstEnemyTurnEndAtOrAfterGrant ||
            temporary.Stacking !=
                TemporaryLibertyStacking.AdditivePerEffectInstance ||
            content.OnCaptured.Count != 0)
        {
            throw new ArgumentException(
                "The card does not match the supported starter Reinforce operation shape.",
                nameof(content));
        }

        return new StarterReinforceCardPlayDefinition(content);
    }

    public static StarterReinforceCardPlayDefinition FromCoreDuelCatalog(
        CoreDuelContentCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        var candidates = catalog.StarterCards
            .Where(card =>
                card.Rarity == CardRarity.Starter &&
                card.Type == CardContentType.Technique &&
                card.Target == CardTargetKind.FriendlyGroup)
            .ToArray();
        if (candidates.Length != 1)
        {
            throw new ArgumentException(
                "Core Duel content must contain exactly one starter FriendlyGroup technique.",
                nameof(catalog));
        }

        return Create(candidates[0]);
    }

    public string ToCanonicalText() => CanonicalText;

    private string CreateCanonicalText() => string.Join(
        '\n',
        EncodingVersion,
        $"content_id={EncodeStableText(ContentId)}",
        "rarity=starter",
        $"cost={Cost.ToString(CultureInfo.InvariantCulture)}",
        "type=technique",
        "target=friendly_group",
        "placement_tag_count=0",
        "effect_count=2",
        "effect_0=draw_if_target_atari;cards=" +
            DrawIfTargetAtari.Cards.ToString(CultureInfo.InvariantCulture),
        "effect_1=temporary_liberty;amount=" +
            TemporaryLiberty.Amount.ToString(CultureInfo.InvariantCulture) +
            ";duration=enemy_turn_end;timing=first_enemy_turn_end_at_or_after_grant" +
            ";stacking=additive_per_effect_instance",
        "on_captured_count=0");

    private static string EncodeStableText(string value) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
}

public enum StarterReinforceCardPlayStatus : byte
{
    Authorized = 1,
    CardNotInHand = 2,
    CardContentMismatch = 3,
    InsufficientQi = 4,
    ActiveResolutionExists = 5,
    TargetEmpty = 6,
    ForeignTarget = 7,
}

/// <summary>
/// Pure Reinforce pre-authorization bound to exact immutable card and liberty
/// snapshots plus the command-time canonical group anchor stone.
/// </summary>
public sealed class StarterReinforceCardPlayEvaluation
{
    internal StarterReinforceCardPlayEvaluation(
        StarterReinforceCardPlayStatus status,
        StarterReinforceCardPlayDefinition sourceDefinition,
        BattleDeckState sourceDeck,
        int sourceQi,
        string requestedInstanceId,
        BattleCardInstance? card,
        StoneRuntimeState sourceStones,
        TemporaryLibertyState sourceTemporaryLiberties,
        ContinuousLibertySnapshot sourceContinuousLiberties,
        CanonicalPoint target,
        CanonicalPoint? targetGroupAnchor,
        string? targetAnchorStoneInstanceId,
        int? targetEffectiveLibertyCount,
        TemporaryLibertyEffectiveLibertyAnalysis? effectiveLibertyAnalysis)
    {
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(
                nameof(status),
                status,
                "Unknown Reinforce card-play status.");
        }

        ArgumentNullException.ThrowIfNull(sourceDefinition);
        ArgumentNullException.ThrowIfNull(sourceDeck);
        ArgumentNullException.ThrowIfNull(requestedInstanceId);
        ArgumentNullException.ThrowIfNull(sourceStones);
        ArgumentNullException.ThrowIfNull(sourceTemporaryLiberties);
        ArgumentNullException.ThrowIfNull(sourceContinuousLiberties);
        ArgumentNullException.ThrowIfNull(target);
        if (status == StarterReinforceCardPlayStatus.Authorized &&
            (card is null ||
             targetGroupAnchor is null ||
             targetAnchorStoneInstanceId is null ||
             targetEffectiveLibertyCount is null ||
             effectiveLibertyAnalysis is null))
        {
            throw new ArgumentException(
                "Authorized Reinforce requires a card and exact target-group binding.",
                nameof(status));
        }

        Status = status;
        SourceDefinition = sourceDefinition;
        SourceDeck = sourceDeck;
        SourceQi = sourceQi;
        RequestedInstanceId = requestedInstanceId;
        Card = card;
        SourceStones = sourceStones;
        SourceTemporaryLiberties = sourceTemporaryLiberties;
        SourceContinuousLiberties = sourceContinuousLiberties;
        Target = target;
        TargetGroupAnchor = targetGroupAnchor;
        TargetAnchorStoneInstanceId = targetAnchorStoneInstanceId;
        TargetEffectiveLibertyCount = targetEffectiveLibertyCount;
        EffectiveLibertyAnalysis = effectiveLibertyAnalysis;
    }

    public StarterReinforceCardPlayStatus Status { get; }

    public bool IsAuthorized => Status == StarterReinforceCardPlayStatus.Authorized;

    public string ReasonId => Status switch
    {
        StarterReinforceCardPlayStatus.Authorized => "authorized",
        StarterReinforceCardPlayStatus.CardNotInHand => "card_not_in_hand",
        StarterReinforceCardPlayStatus.CardContentMismatch => "card_content_mismatch",
        StarterReinforceCardPlayStatus.InsufficientQi => "insufficient_qi",
        StarterReinforceCardPlayStatus.ActiveResolutionExists =>
            "active_resolution_exists",
        StarterReinforceCardPlayStatus.TargetEmpty => "reinforce_target_empty",
        StarterReinforceCardPlayStatus.ForeignTarget => "reinforce_target_foreign",
        _ => throw new InvalidOperationException("Unknown Reinforce card-play status."),
    };

    public StarterReinforceCardPlayDefinition SourceDefinition { get; }

    public BattleDeckState SourceDeck { get; }

    public int SourceQi { get; }

    public string RequestedInstanceId { get; }

    public BattleCardInstance? Card { get; }

    public StoneRuntimeState SourceStones { get; }

    public TemporaryLibertyState SourceTemporaryLiberties { get; }

    public ContinuousLibertySnapshot SourceContinuousLiberties { get; }

    public CanonicalPoint Target { get; }

    public CanonicalPoint? TargetGroupAnchor { get; }

    public string? TargetAnchorStoneInstanceId { get; }

    public int? TargetEffectiveLibertyCount { get; }

    public TemporaryLibertyEffectiveLibertyAnalysis? EffectiveLibertyAnalysis { get; }

    public bool IsBoundTo(
        StarterReinforceCardPlayDefinition definition,
        BattleDeckState deck,
        int qi,
        string instanceId,
        StoneRuntimeState stones,
        TemporaryLibertyState temporaryLiberties,
        ContinuousLibertySnapshot continuousLiberties,
        CanonicalPoint target) =>
        IsAuthorized &&
        ReferenceEquals(SourceDefinition, definition) &&
        ReferenceEquals(SourceDeck, deck) &&
        SourceQi == qi &&
        StringComparer.Ordinal.Equals(RequestedInstanceId, instanceId) &&
        Card is not null &&
        StringComparer.Ordinal.Equals(Card.InstanceId, instanceId) &&
        ReferenceEquals(
            Card,
            deck.Hand.FirstOrDefault(candidate =>
                StringComparer.Ordinal.Equals(candidate.InstanceId, instanceId))) &&
        ReferenceEquals(SourceStones, stones) &&
        ReferenceEquals(SourceTemporaryLiberties, temporaryLiberties) &&
        ReferenceEquals(SourceContinuousLiberties, continuousLiberties) &&
        Target.Equals(target);
}

public static class StarterReinforceCardPlayEvaluator
{
    public static StarterReinforceCardPlayEvaluation Evaluate(
        StarterReinforceCardPlayDefinition definition,
        BattleDeckState deck,
        int qi,
        string instanceId,
        StoneRuntimeState stones,
        TemporaryLibertyState temporaryLiberties,
        ContinuousLibertySnapshot continuousLiberties,
        CanonicalPoint target)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(deck);
        ArgumentNullException.ThrowIfNull(instanceId);
        ArgumentNullException.ThrowIfNull(stones);
        ArgumentNullException.ThrowIfNull(temporaryLiberties);
        ArgumentNullException.ThrowIfNull(continuousLiberties);
        ArgumentNullException.ThrowIfNull(target);
        stones.SourceBoard.Geometry.ToCanonicalIndex(target);
        if (!ReferenceEquals(stones, temporaryLiberties.SourceStones) ||
            !ReferenceEquals(stones, continuousLiberties.SourceStones))
        {
            throw new ArgumentException(
                "Reinforce requires exact-bound stone and liberty snapshots.",
                nameof(stones));
        }

        var card = deck.Hand.FirstOrDefault(candidate =>
            StringComparer.Ordinal.Equals(candidate.InstanceId, instanceId));
        if (card is null)
        {
            return Evaluation(
                StarterReinforceCardPlayStatus.CardNotInHand,
                definition,
                deck,
                qi,
                instanceId,
                null,
                stones,
                temporaryLiberties,
                continuousLiberties,
                target);
        }

        if (!StringComparer.Ordinal.Equals(card.ContentId, definition.ContentId))
        {
            return Evaluation(
                StarterReinforceCardPlayStatus.CardContentMismatch,
                definition,
                deck,
                qi,
                instanceId,
                card,
                stones,
                temporaryLiberties,
                continuousLiberties,
                target);
        }

        if (qi < definition.Cost)
        {
            return Evaluation(
                StarterReinforceCardPlayStatus.InsufficientQi,
                definition,
                deck,
                qi,
                instanceId,
                card,
                stones,
                temporaryLiberties,
                continuousLiberties,
                target);
        }

        if (deck.Resolving.Any(candidate => candidate.IsActive))
        {
            return Evaluation(
                StarterReinforceCardPlayStatus.ActiveResolutionExists,
                definition,
                deck,
                qi,
                instanceId,
                card,
                stones,
                temporaryLiberties,
                continuousLiberties,
                target);
        }

        var targetStone = stones.InstanceAt(target);
        if (targetStone is null)
        {
            return Evaluation(
                StarterReinforceCardPlayStatus.TargetEmpty,
                definition,
                deck,
                qi,
                instanceId,
                card,
                stones,
                temporaryLiberties,
                continuousLiberties,
                target);
        }

        if (targetStone.Color != StoneColor.Black)
        {
            return Evaluation(
                StarterReinforceCardPlayStatus.ForeignTarget,
                definition,
                deck,
                qi,
                instanceId,
                card,
                stones,
                temporaryLiberties,
                continuousLiberties,
                target);
        }

        var analysis = TemporaryLibertyEffectiveLibertyAnalyzer.Analyze(
            stones,
            temporaryLiberties,
            continuousLiberties);
        var group = analysis.GroupAnalysis.GroupAt(target)
            ?? throw new InvalidOperationException(
                "A live Reinforce target stone must belong to a group.");
        var anchor = stones.InstanceAt(group.Anchor)
            ?? throw new InvalidOperationException(
                "A Reinforce target group must have a runtime anchor stone.");
        var effective = analysis.BreakdownFor(group).EffectiveLibertyCount;
        return new StarterReinforceCardPlayEvaluation(
            StarterReinforceCardPlayStatus.Authorized,
            definition,
            deck,
            qi,
            instanceId,
            card,
            stones,
            temporaryLiberties,
            continuousLiberties,
            target,
            group.Anchor,
            anchor.InstanceId,
            effective,
            analysis);
    }

    private static StarterReinforceCardPlayEvaluation Evaluation(
        StarterReinforceCardPlayStatus status,
        StarterReinforceCardPlayDefinition definition,
        BattleDeckState deck,
        int qi,
        string instanceId,
        BattleCardInstance? card,
        StoneRuntimeState stones,
        TemporaryLibertyState temporaryLiberties,
        ContinuousLibertySnapshot continuousLiberties,
        CanonicalPoint target) =>
        new(
            status,
            definition,
            deck,
            qi,
            instanceId,
            card,
            stones,
            temporaryLiberties,
            continuousLiberties,
            target,
            null,
            null,
            null,
            null);
}
