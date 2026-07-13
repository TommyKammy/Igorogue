using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

using Igorogue.Domain.Board;
using Igorogue.Domain.Content;
using Igorogue.Domain.Determinism;

namespace Igorogue.Domain.Cards;

public enum BasicStoneCardPlacementMode : byte
{
    Frontline = 1,
    TerminalCapture = 2,
}

/// <summary>
/// Typed, immutable projection of the one-operation basic stone-card shape.
/// The projection is based only on content shape; content IDs do not select behavior.
/// </summary>
public sealed class BasicStoneCardPlayDefinition
{
    public const string EncodingVersion = "basic-stone-card-play-definition-v1";

    private readonly ReadOnlyCollection<CardPlacementTag> placementTagView;

    private BasicStoneCardPlayDefinition(
        string contentId,
        int cost,
        StoneContentKind stoneKind,
        CardPlacementTag[] placementTags)
    {
        ContentId = contentId;
        Cost = cost;
        StoneKind = stoneKind;
        placementTagView = Array.AsReadOnly((CardPlacementTag[])placementTags.Clone());
        CanonicalText = CreateCanonicalText();
        Checksum = DeterministicChecksum.Sha256Hex(CanonicalText);
    }

    public string ContentId { get; }

    public int Cost { get; }

    public StoneContentKind StoneKind { get; }

    public IReadOnlyList<CardPlacementTag> PlacementTags => placementTagView;

    public bool AllowsTerminalCapture =>
        placementTagView.Contains(CardPlacementTag.Terminal);

    public string CanonicalText { get; }

    public string Checksum { get; }

    public static BasicStoneCardPlayDefinition Create(CardContentDefinition content)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (content.Type != CardContentType.Stone)
        {
            throw new ArgumentException(
                "A basic stone card must have type Stone.",
                nameof(content));
        }

        if (content.Target != CardTargetKind.None)
        {
            throw new ArgumentException(
                "A basic stone card must not declare a separate target kind.",
                nameof(content));
        }

        if (content.Effects.Count != 1)
        {
            throw new ArgumentException(
                "A basic stone card must have exactly one effect.",
                nameof(content));
        }

        if (content.Effects[0] is not PlaceStoneOperationDefinition placement)
        {
            throw new ArgumentException(
                "A basic stone card's only effect must place a stone.",
                nameof(content));
        }

        if (placement.StoneKind != StoneContentKind.Basic)
        {
            throw new ArgumentException(
                "A basic stone card must place a Basic stone.",
                nameof(content));
        }

        if (content.OnCaptured.Count != 0)
        {
            throw new ArgumentException(
                "A basic stone card must not define on-captured operations.",
                nameof(content));
        }

        var canonicalTags = content.PlacementTags.ToArray();
        if (!canonicalTags.Contains(CardPlacementTag.Frontline))
        {
            throw new ArgumentException(
                "A basic stone card must include the Frontline placement tag.",
                nameof(content));
        }

        if (canonicalTags.Any(
                tag => tag is not CardPlacementTag.Frontline and
                    not CardPlacementTag.Terminal))
        {
            throw new ArgumentException(
                "A basic stone card may include only Frontline and Terminal placement tags.",
                nameof(content));
        }

        Array.Sort(canonicalTags);
        return new BasicStoneCardPlayDefinition(
            content.Id,
            content.Cost,
            placement.StoneKind,
            canonicalTags);
    }

    public string ToCanonicalText() => CanonicalText;

    private string CreateCanonicalText()
    {
        var lines = new List<string>
        {
            EncodingVersion,
            $"content_id={EncodeStableText(ContentId)}",
            $"cost={Cost.ToString(CultureInfo.InvariantCulture)}",
            "type=stone",
            "target=none",
            $"stone_kind={StoneKindId(StoneKind)}",
            $"placement_tag_count={placementTagView.Count.ToString(CultureInfo.InvariantCulture)}",
        };

        for (var index = 0; index < placementTagView.Count; index++)
        {
            lines.Add(
                $"placement_tag_{index.ToString(CultureInfo.InvariantCulture)}=" +
                PlacementTagId(placementTagView[index]));
        }

        lines.Add("on_captured_count=0");
        return string.Join('\n', lines);
    }

    private static string PlacementTagId(CardPlacementTag tag) => tag switch
    {
        CardPlacementTag.Frontline => "frontline",
        CardPlacementTag.Terminal => "terminal",
        _ => throw new InvalidOperationException("Unsupported basic-stone placement tag."),
    };

    private static string StoneKindId(StoneContentKind stoneKind) => stoneKind switch
    {
        StoneContentKind.Basic => "basic",
        _ => throw new InvalidOperationException("Unsupported basic-stone kind."),
    };

    private static string EncodeStableText(string value) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
}

public enum BasicStoneCardPlayStatus : byte
{
    Authorized = 1,
    CardNotInHand = 2,
    CardContentMismatch = 3,
    InsufficientQi = 4,
    ActiveResolutionExists = 5,
    UnsupportedMode = 6,
    TargetOccupied = 7,
    FrontlineAdjacencyRequired = 8,
}

/// <summary>
/// Pure pre-authorization bound to exact immutable source snapshots.
/// Terminal capture still requires the existing placement-legality evaluator to prove
/// that the placement captures immediately.
/// </summary>
public sealed class BasicStoneCardPlayEvaluation
{
    internal BasicStoneCardPlayEvaluation(
        BasicStoneCardPlayStatus status,
        BasicStoneCardPlayDefinition sourceDefinition,
        BattleDeckState sourceDeck,
        int sourceQi,
        string requestedInstanceId,
        BattleCardInstance? card,
        BoardState sourceBoard,
        CanonicalPoint target,
        BasicStoneCardPlacementMode requestedMode,
        PlacementAccessMode? accessMode)
    {
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown card-play status.");
        }

        ArgumentNullException.ThrowIfNull(sourceDefinition);
        ArgumentNullException.ThrowIfNull(sourceDeck);
        ArgumentNullException.ThrowIfNull(requestedInstanceId);
        ArgumentNullException.ThrowIfNull(sourceBoard);
        ArgumentNullException.ThrowIfNull(target);

        if (status == BasicStoneCardPlayStatus.Authorized &&
            (card is null || accessMode is null))
        {
            throw new ArgumentException(
                "Authorized card play requires a card and placement access mode.",
                nameof(status));
        }

        Status = status;
        SourceDefinition = sourceDefinition;
        SourceDeck = sourceDeck;
        SourceQi = sourceQi;
        RequestedInstanceId = requestedInstanceId;
        Card = card;
        SourceBoard = sourceBoard;
        Target = target;
        RequestedMode = requestedMode;
        AccessMode = accessMode;
    }

    public BasicStoneCardPlayStatus Status { get; }

    public bool IsAuthorized => Status == BasicStoneCardPlayStatus.Authorized;

    public string ReasonId => Status switch
    {
        BasicStoneCardPlayStatus.Authorized => "authorized",
        BasicStoneCardPlayStatus.CardNotInHand => "card_not_in_hand",
        BasicStoneCardPlayStatus.CardContentMismatch => "card_content_mismatch",
        BasicStoneCardPlayStatus.InsufficientQi => "insufficient_qi",
        BasicStoneCardPlayStatus.ActiveResolutionExists => "active_resolution_exists",
        BasicStoneCardPlayStatus.UnsupportedMode => "unsupported_placement_mode",
        BasicStoneCardPlayStatus.TargetOccupied => "target_occupied",
        BasicStoneCardPlayStatus.FrontlineAdjacencyRequired =>
            "frontline_adjacency_required",
        _ => throw new InvalidOperationException("Unknown card-play status."),
    };

    public BasicStoneCardPlayDefinition SourceDefinition { get; }

    public BattleDeckState SourceDeck { get; }

    public int SourceQi { get; }

    public string RequestedInstanceId { get; }

    public BattleCardInstance? Card { get; }

    public BoardState SourceBoard { get; }

    public CanonicalPoint Target { get; }

    public BasicStoneCardPlacementMode RequestedMode { get; }

    public PlacementAccessMode? AccessMode { get; }

    public bool IsBoundTo(
        BasicStoneCardPlayDefinition definition,
        BattleDeckState deck,
        int qi,
        string instanceId,
        BoardState board,
        CanonicalPoint target,
        BasicStoneCardPlacementMode mode,
        PlacementAccessMode accessMode) =>
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
        ReferenceEquals(SourceBoard, board) &&
        Target.Equals(target) &&
        RequestedMode == mode &&
        AccessMode == accessMode;
}

public static class BasicStoneCardPlayEvaluator
{
    public static BasicStoneCardPlayEvaluation Evaluate(
        BasicStoneCardPlayDefinition definition,
        BattleDeckState deck,
        int qi,
        string instanceId,
        BoardState board,
        CanonicalPoint target,
        BasicStoneCardPlacementMode mode)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(deck);
        ArgumentNullException.ThrowIfNull(instanceId);
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(target);
        board.Geometry.ToCanonicalIndex(target);

        var card = deck.Hand.FirstOrDefault(
            candidate => StringComparer.Ordinal.Equals(candidate.InstanceId, instanceId));
        if (card is null)
        {
            return Evaluation(
                BasicStoneCardPlayStatus.CardNotInHand,
                definition,
                deck,
                qi,
                instanceId,
                null,
                board,
                target,
                mode,
                null);
        }

        if (!StringComparer.Ordinal.Equals(card.ContentId, definition.ContentId))
        {
            return Evaluation(
                BasicStoneCardPlayStatus.CardContentMismatch,
                definition,
                deck,
                qi,
                instanceId,
                card,
                board,
                target,
                mode,
                null);
        }

        if (qi < definition.Cost)
        {
            return Evaluation(
                BasicStoneCardPlayStatus.InsufficientQi,
                definition,
                deck,
                qi,
                instanceId,
                card,
                board,
                target,
                mode,
                null);
        }

        if (deck.Resolving.Any(candidate => candidate.IsActive))
        {
            return Evaluation(
                BasicStoneCardPlayStatus.ActiveResolutionExists,
                definition,
                deck,
                qi,
                instanceId,
                card,
                board,
                target,
                mode,
                null);
        }

        var accessMode = AccessModeFor(definition, mode);
        if (accessMode is null)
        {
            return Evaluation(
                BasicStoneCardPlayStatus.UnsupportedMode,
                definition,
                deck,
                qi,
                instanceId,
                card,
                board,
                target,
                mode,
                null);
        }

        if (!board.IsEmpty(target))
        {
            return Evaluation(
                BasicStoneCardPlayStatus.TargetOccupied,
                definition,
                deck,
                qi,
                instanceId,
                card,
                board,
                target,
                mode,
                accessMode);
        }

        if (mode == BasicStoneCardPlacementMode.Frontline &&
            !board.Geometry.GetOrthogonalNeighbours(target).Any(
                neighbour => board.StoneAt(neighbour)?.Color == StoneColor.Black))
        {
            return Evaluation(
                BasicStoneCardPlayStatus.FrontlineAdjacencyRequired,
                definition,
                deck,
                qi,
                instanceId,
                card,
                board,
                target,
                mode,
                accessMode);
        }

        return Evaluation(
            BasicStoneCardPlayStatus.Authorized,
            definition,
            deck,
            qi,
            instanceId,
            card,
            board,
            target,
            mode,
            accessMode);
    }

    private static PlacementAccessMode? AccessModeFor(
        BasicStoneCardPlayDefinition definition,
        BasicStoneCardPlacementMode mode) => mode switch
        {
            BasicStoneCardPlacementMode.Frontline => PlacementAccessMode.Normal,
            BasicStoneCardPlacementMode.TerminalCapture when definition.AllowsTerminalCapture =>
                PlacementAccessMode.TerminalCapture,
            _ => null,
        };

    private static BasicStoneCardPlayEvaluation Evaluation(
        BasicStoneCardPlayStatus status,
        BasicStoneCardPlayDefinition definition,
        BattleDeckState deck,
        int qi,
        string instanceId,
        BattleCardInstance? card,
        BoardState board,
        CanonicalPoint target,
        BasicStoneCardPlacementMode mode,
        PlacementAccessMode? accessMode) =>
        new(
            status,
            definition,
            deck,
            qi,
            instanceId,
            card,
            board,
            target,
            mode,
            accessMode);
}
