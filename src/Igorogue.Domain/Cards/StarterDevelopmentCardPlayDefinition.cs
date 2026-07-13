using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

using Igorogue.Domain.Board;
using Igorogue.Domain.Content;
using Igorogue.Domain.Determinism;
using Igorogue.Domain.Facilities;

namespace Igorogue.Domain.Cards;

/// <summary>
/// Typed, immutable projection of the supported starter Development operation
/// shape. The facility content ID and cost remain content-authored values.
/// </summary>
public sealed class StarterDevelopmentCardPlayDefinition
{
    public const string EncodingVersion = "starter-development-card-play-definition-v1";

    private readonly ReadOnlyCollection<CardOperationDefinition> effectView;

    private StarterDevelopmentCardPlayDefinition(CardContentDefinition content)
    {
        ContentId = content.Id;
        Cost = content.Cost;
        effectView = Array.AsReadOnly(content.Effects.ToArray());
        BuildFacility = (BuildFacilityOperationDefinition)effectView[0];
        CanonicalText = CreateCanonicalText();
        Checksum = DeterministicChecksum.Sha256Hex(CanonicalText);
    }

    public string ContentId { get; }

    public int Cost { get; }

    public CardTargetKind Target => CardTargetKind.BlackTerritoryEmpty;

    public BuildFacilityOperationDefinition BuildFacility { get; }

    public string FacilityContentId => BuildFacility.FacilityContentId;

    public IReadOnlyList<CardOperationDefinition> Effects => effectView;

    public string CanonicalText { get; }

    public string Checksum { get; }

    public static StarterDevelopmentCardPlayDefinition Create(
        CardContentDefinition content)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (content.Rarity != CardRarity.Starter ||
            content.Type != CardContentType.Territory ||
            content.Target != CardTargetKind.BlackTerritoryEmpty ||
            content.PlacementTags.Count != 0 ||
            content.Effects.Count != 1 ||
            content.Effects[0] is not BuildFacilityOperationDefinition ||
            content.OnCaptured.Count != 0)
        {
            throw new ArgumentException(
                "The card does not match the supported starter Development operation shape.",
                nameof(content));
        }

        return new StarterDevelopmentCardPlayDefinition(content);
    }

    public static StarterDevelopmentCardPlayDefinition FromCoreDuelCatalog(
        CoreDuelContentCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        var candidates = catalog.StarterCards
            .Where(card =>
                card.Rarity == CardRarity.Starter &&
                card.Type == CardContentType.Territory &&
                card.Target == CardTargetKind.BlackTerritoryEmpty)
            .ToArray();
        if (candidates.Length != 1)
        {
            throw new ArgumentException(
                "Core Duel content must contain exactly one starter BlackTerritoryEmpty territory card.",
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
        "type=territory",
        "target=black_territory_empty",
        "placement_tag_count=0",
        "effect_count=1",
        $"effect_0=build_facility;facility={EncodeStableText(FacilityContentId)}",
        "on_captured_count=0");

    private static string EncodeStableText(string value) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
}

public enum StarterDevelopmentCardPlayStatus : byte
{
    Authorized = 1,
    CardNotInHand = 2,
    CardContentMismatch = 3,
    InsufficientQi = 4,
    ActiveResolutionExists = 5,
    TargetHasStone = 6,
    TargetOccupied = 7,
    TargetNotOwnedTerritory = 8,
    CapacityFull = 9,
    TypeLimitReached = 10,
}

/// <summary>
/// Pure Development pre-authorization bound to the exact card, facility-state,
/// territory, and runtime-policy snapshots used by the shared facility kernel.
/// </summary>
public sealed class StarterDevelopmentCardPlayEvaluation
{
    internal StarterDevelopmentCardPlayEvaluation(
        StarterDevelopmentCardPlayStatus status,
        StarterDevelopmentCardPlayDefinition sourceDefinition,
        BattleDeckState sourceDeck,
        int sourceQi,
        string requestedCardInstanceId,
        BattleCardInstance? card,
        FacilityRuntimeAnalysis sourceFacilityAnalysis,
        FacilityBuildRequest buildRequest,
        FacilityBuildEvaluation? buildEvaluation)
    {
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(
                nameof(status),
                status,
                "Unknown Development card-play status.");
        }

        ArgumentNullException.ThrowIfNull(sourceDefinition);
        ArgumentNullException.ThrowIfNull(sourceDeck);
        ArgumentNullException.ThrowIfNull(requestedCardInstanceId);
        ArgumentNullException.ThrowIfNull(sourceFacilityAnalysis);
        ArgumentNullException.ThrowIfNull(buildRequest);
        if (!StringComparer.Ordinal.Equals(
                sourceDefinition.FacilityContentId,
                buildRequest.FacilityContentId))
        {
            throw new ArgumentException(
                "Development build request must use the exact content-authored facility ID.",
                nameof(buildRequest));
        }

        if (buildEvaluation is not null &&
            (!ReferenceEquals(buildEvaluation.Analysis, sourceFacilityAnalysis) ||
             !ReferenceEquals(buildEvaluation.Request, buildRequest)))
        {
            throw new ArgumentException(
                "Development facility evaluation must retain its exact source analysis and request.",
                nameof(buildEvaluation));
        }

        var mappedBuildStatus = buildEvaluation is null
            ? (StarterDevelopmentCardPlayStatus?)null
            : StatusFor(buildEvaluation.Status);
        if (status == StarterDevelopmentCardPlayStatus.Authorized &&
            (card is null ||
             buildEvaluation is null ||
             !buildEvaluation.IsLegal ||
             mappedBuildStatus != status))
        {
            throw new ArgumentException(
                "Authorized Development requires a card and legal shared facility evaluation.",
                nameof(status));
        }

        if (IsFacilityStatus(status) && mappedBuildStatus != status)
        {
            throw new ArgumentException(
                "Development facility rejection must match the shared facility evaluation.",
                nameof(status));
        }

        Status = status;
        SourceDefinition = sourceDefinition;
        SourceDeck = sourceDeck;
        SourceQi = sourceQi;
        RequestedCardInstanceId = requestedCardInstanceId;
        Card = card;
        SourceFacilityAnalysis = sourceFacilityAnalysis;
        BuildRequest = buildRequest;
        BuildEvaluation = buildEvaluation;
    }

    public StarterDevelopmentCardPlayStatus Status { get; }

    public bool IsAuthorized => Status == StarterDevelopmentCardPlayStatus.Authorized;

    public string ReasonId => Status switch
    {
        StarterDevelopmentCardPlayStatus.Authorized => "authorized",
        StarterDevelopmentCardPlayStatus.CardNotInHand => "card_not_in_hand",
        StarterDevelopmentCardPlayStatus.CardContentMismatch => "card_content_mismatch",
        StarterDevelopmentCardPlayStatus.InsufficientQi => "insufficient_qi",
        StarterDevelopmentCardPlayStatus.ActiveResolutionExists =>
            "active_resolution_exists",
        StarterDevelopmentCardPlayStatus.TargetHasStone or
        StarterDevelopmentCardPlayStatus.TargetOccupied or
        StarterDevelopmentCardPlayStatus.TargetNotOwnedTerritory or
        StarterDevelopmentCardPlayStatus.CapacityFull or
        StarterDevelopmentCardPlayStatus.TypeLimitReached =>
            BuildEvaluation?.ReasonId ?? throw new InvalidOperationException(
                "A Development facility status is missing its shared facility evaluation."),
        _ => throw new InvalidOperationException("Unknown Development card-play status."),
    };

    public StarterDevelopmentCardPlayDefinition SourceDefinition { get; }

    public BattleDeckState SourceDeck { get; }

    public int SourceQi { get; }

    public string RequestedCardInstanceId { get; }

    public BattleCardInstance? Card { get; }

    public FacilityRuntimeAnalysis SourceFacilityAnalysis { get; }

    public FacilityState SourceFacilityState => SourceFacilityAnalysis.FacilityState;

    public FacilityRuntimePolicy SourceFacilityPolicy => SourceFacilityAnalysis.Policy;

    public FacilityBuildRequest BuildRequest { get; }

    public FacilityBuildEvaluation? BuildEvaluation { get; }

    public CanonicalPoint Target => BuildRequest.Point;

    public string FacilityInstanceId => BuildRequest.InstanceId;

    public FacilityBuildEvaluation LegalFacilityBuildEvaluation =>
        IsAuthorized && BuildEvaluation is { IsLegal: true }
            ? BuildEvaluation
            : throw new InvalidOperationException(
                "Only an authorized Development result exposes a legal facility build evaluation.");

    public bool IsBoundTo(
        StarterDevelopmentCardPlayDefinition definition,
        BattleDeckState deck,
        int qi,
        string cardInstanceId,
        FacilityRuntimeAnalysis facilityAnalysis,
        CanonicalPoint target,
        string facilityInstanceId) =>
        IsAuthorized &&
        ReferenceEquals(SourceDefinition, definition) &&
        ReferenceEquals(SourceDeck, deck) &&
        SourceQi == qi &&
        StringComparer.Ordinal.Equals(RequestedCardInstanceId, cardInstanceId) &&
        Card is not null &&
        StringComparer.Ordinal.Equals(Card.InstanceId, cardInstanceId) &&
        ReferenceEquals(
            Card,
            deck.Hand.FirstOrDefault(candidate =>
                StringComparer.Ordinal.Equals(candidate.InstanceId, cardInstanceId))) &&
        ReferenceEquals(SourceFacilityAnalysis, facilityAnalysis) &&
        ReferenceEquals(SourceFacilityState, facilityAnalysis.FacilityState) &&
        ReferenceEquals(SourceFacilityPolicy, facilityAnalysis.Policy) &&
        Target.Equals(target) &&
        StringComparer.Ordinal.Equals(FacilityInstanceId, facilityInstanceId) &&
        StringComparer.Ordinal.Equals(
            BuildRequest.FacilityContentId,
            definition.FacilityContentId) &&
        BuildRequest.ActorColor == StoneColor.Black &&
        BuildEvaluation is { IsLegal: true } &&
        ReferenceEquals(BuildEvaluation.Analysis, facilityAnalysis) &&
        ReferenceEquals(BuildEvaluation.Request, BuildRequest);

    private static bool IsFacilityStatus(StarterDevelopmentCardPlayStatus status) =>
        status is StarterDevelopmentCardPlayStatus.Authorized or
            StarterDevelopmentCardPlayStatus.TargetHasStone or
            StarterDevelopmentCardPlayStatus.TargetOccupied or
            StarterDevelopmentCardPlayStatus.TargetNotOwnedTerritory or
            StarterDevelopmentCardPlayStatus.CapacityFull or
            StarterDevelopmentCardPlayStatus.TypeLimitReached;

    internal static StarterDevelopmentCardPlayStatus StatusFor(
        FacilityBuildStatus status) => status switch
        {
            FacilityBuildStatus.Legal => StarterDevelopmentCardPlayStatus.Authorized,
            FacilityBuildStatus.TargetHasStone =>
                StarterDevelopmentCardPlayStatus.TargetHasStone,
            FacilityBuildStatus.TargetOccupied =>
                StarterDevelopmentCardPlayStatus.TargetOccupied,
            FacilityBuildStatus.TargetNotOwnedTerritory =>
                StarterDevelopmentCardPlayStatus.TargetNotOwnedTerritory,
            FacilityBuildStatus.CapacityFull =>
                StarterDevelopmentCardPlayStatus.CapacityFull,
            FacilityBuildStatus.TypeLimitReached =>
                StarterDevelopmentCardPlayStatus.TypeLimitReached,
            _ => throw new InvalidOperationException("Unknown shared facility build status."),
        };
}

public static class StarterDevelopmentCardPlayEvaluator
{
    public static StarterDevelopmentCardPlayEvaluation Evaluate(
        StarterDevelopmentCardPlayDefinition definition,
        BattleDeckState deck,
        int qi,
        string cardInstanceId,
        FacilityRuntimeAnalysis facilityAnalysis,
        CanonicalPoint target,
        string facilityInstanceId)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(deck);
        ArgumentNullException.ThrowIfNull(cardInstanceId);
        ArgumentNullException.ThrowIfNull(facilityAnalysis);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(facilityInstanceId);
        facilityAnalysis.SourceBoard.Geometry.ToCanonicalIndex(target);

        var request = new FacilityBuildRequest(
            StoneColor.Black,
            target,
            definition.FacilityContentId,
            facilityInstanceId);
        var card = deck.Hand.FirstOrDefault(candidate =>
            StringComparer.Ordinal.Equals(candidate.InstanceId, cardInstanceId));
        if (card is null)
        {
            return Evaluation(
                StarterDevelopmentCardPlayStatus.CardNotInHand,
                definition,
                deck,
                qi,
                cardInstanceId,
                null,
                facilityAnalysis,
                request,
                null);
        }

        if (!StringComparer.Ordinal.Equals(card.ContentId, definition.ContentId))
        {
            return Evaluation(
                StarterDevelopmentCardPlayStatus.CardContentMismatch,
                definition,
                deck,
                qi,
                cardInstanceId,
                card,
                facilityAnalysis,
                request,
                null);
        }

        if (qi < definition.Cost)
        {
            return Evaluation(
                StarterDevelopmentCardPlayStatus.InsufficientQi,
                definition,
                deck,
                qi,
                cardInstanceId,
                card,
                facilityAnalysis,
                request,
                null);
        }

        if (deck.Resolving.Any(candidate => candidate.IsActive))
        {
            return Evaluation(
                StarterDevelopmentCardPlayStatus.ActiveResolutionExists,
                definition,
                deck,
                qi,
                cardInstanceId,
                card,
                facilityAnalysis,
                request,
                null);
        }

        var buildEvaluation = FacilityBuildEvaluator.Evaluate(
            facilityAnalysis,
            request);
        return Evaluation(
            StarterDevelopmentCardPlayEvaluation.StatusFor(buildEvaluation.Status),
            definition,
            deck,
            qi,
            cardInstanceId,
            card,
            facilityAnalysis,
            request,
            buildEvaluation);
    }

    private static StarterDevelopmentCardPlayEvaluation Evaluation(
        StarterDevelopmentCardPlayStatus status,
        StarterDevelopmentCardPlayDefinition definition,
        BattleDeckState deck,
        int qi,
        string cardInstanceId,
        BattleCardInstance? card,
        FacilityRuntimeAnalysis facilityAnalysis,
        FacilityBuildRequest request,
        FacilityBuildEvaluation? buildEvaluation) =>
        new(
            status,
            definition,
            deck,
            qi,
            cardInstanceId,
            card,
            facilityAnalysis,
            request,
            buildEvaluation);
}
