using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

using Igorogue.Domain.Board;
using Igorogue.Domain.Content;
using Igorogue.Domain.Determinism;

namespace Igorogue.Domain.Cards;

public enum StarterStoneCardProfile : byte
{
    BasicPlacement = 1,
    Extend = 2,
    Contact = 3,
    Lure = 4,
}

public enum StoneCardPlacementMode : byte
{
    Frontline = 1,
    Contact = 2,
    TerminalCapture = 3,
}

/// <summary>
/// Typed, immutable projection of one supported starter stone-card shape.
/// Behavior is selected by printed operation shape rather than content ID.
/// </summary>
public sealed class StarterStoneCardPlayDefinition
{
    public const string EncodingVersion = "starter-stone-card-play-definition-v1";

    private readonly ReadOnlyCollection<CardPlacementTag> placementTagView;
    private readonly ReadOnlyCollection<CardOperationDefinition> effectView;
    private readonly ReadOnlyCollection<CardOperationDefinition> onCapturedView;

    private StarterStoneCardPlayDefinition(
        CardContentDefinition content,
        StarterStoneCardProfile profile)
    {
        ContentId = content.Id;
        Cost = content.Cost;
        Profile = profile;

        var placementTags = content.PlacementTags.ToArray();
        Array.Sort(placementTags);
        placementTagView = Array.AsReadOnly(placementTags);

        var effects = content.Effects.ToArray();
        effectView = Array.AsReadOnly(effects);
        onCapturedView = Array.AsReadOnly(content.OnCaptured.ToArray());
        Placement = (PlaceStoneOperationDefinition)effects[0];

        CanonicalText = CreateCanonicalText();
        Checksum = DeterministicChecksum.Sha256Hex(CanonicalText);
    }

    public string ContentId { get; }

    public int Cost { get; }

    public StarterStoneCardProfile Profile { get; }

    public StoneContentKind StoneKind => Placement.StoneKind;

    public PlaceStoneOperationDefinition Placement { get; }

    public IReadOnlyList<CardPlacementTag> PlacementTags => placementTagView;

    public IReadOnlyList<CardOperationDefinition> Effects => effectView;

    public IReadOnlyList<CardOperationDefinition> OnCaptured => onCapturedView;

    public bool AllowsTerminalCapture =>
        placementTagView.Contains(CardPlacementTag.Terminal);

    public string CanonicalText { get; }

    public string Checksum { get; }

    public static StarterStoneCardPlayDefinition Create(CardContentDefinition content)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (content.Rarity != CardRarity.Starter)
        {
            throw new ArgumentException(
                "A starter stone card must have Starter rarity.",
                nameof(content));
        }

        if (content.Type != CardContentType.Stone)
        {
            throw new ArgumentException(
                "A starter stone card must have type Stone.",
                nameof(content));
        }

        if (content.Target != CardTargetKind.None)
        {
            throw new ArgumentException(
                "A starter stone card must not declare a separate target kind.",
                nameof(content));
        }

        var profile = ProjectProfile(content);
        return new StarterStoneCardPlayDefinition(content, profile);
    }

    public string ToCanonicalText() => CanonicalText;

    private static StarterStoneCardProfile ProjectProfile(CardContentDefinition content)
    {
        if (TagsAre(content, CardPlacementTag.Frontline, CardPlacementTag.Terminal) &&
            content.Effects.Count == 1 &&
            IsPlacement(content.Effects[0], StoneContentKind.Basic) &&
            content.OnCaptured.Count == 0)
        {
            return StarterStoneCardProfile.BasicPlacement;
        }

        if (TagsAre(content, CardPlacementTag.Frontline) &&
            content.Effects.Count == 2 &&
            IsPlacement(content.Effects[0], StoneContentKind.Basic) &&
            content.Effects[1] is DrawIfRealLibertiesAtLeastOperationDefinition &&
            content.OnCaptured.Count == 0)
        {
            return StarterStoneCardProfile.Extend;
        }

        if (TagsAre(content, CardPlacementTag.Contact, CardPlacementTag.Terminal) &&
            content.Effects.Count == 2 &&
            IsPlacement(content.Effects[0], StoneContentKind.Basic) &&
            content.Effects[1] is GainQiIfEnemyAtariOperationDefinition &&
            content.OnCaptured.Count == 0)
        {
            return StarterStoneCardProfile.Contact;
        }

        if (TagsAre(content, CardPlacementTag.Contact) &&
            content.Effects.Count == 2 &&
            IsPlacement(content.Effects[0], StoneContentKind.Lure) &&
            content.Effects[1] is ReserveDrawOperationDefinition &&
            content.OnCaptured.Count == 1 &&
            content.OnCaptured[0] is ReserveDrawOperationDefinition)
        {
            return StarterStoneCardProfile.Lure;
        }

        throw new ArgumentException(
            "The card does not match a supported starter stone-card operation shape.",
            nameof(content));
    }

    private static bool TagsAre(
        CardContentDefinition content,
        params CardPlacementTag[] expected)
    {
        if (content.PlacementTags.Count != expected.Length)
        {
            return false;
        }

        var canonicalExpected = (CardPlacementTag[])expected.Clone();
        Array.Sort(canonicalExpected);
        return content.PlacementTags.SequenceEqual(canonicalExpected);
    }

    private static bool IsPlacement(
        CardOperationDefinition operation,
        StoneContentKind expectedKind) =>
        operation is PlaceStoneOperationDefinition placement &&
        placement.StoneKind == expectedKind;

    private string CreateCanonicalText()
    {
        var lines = new List<string>
        {
            EncodingVersion,
            $"content_id={EncodeStableText(ContentId)}",
            "rarity=starter",
            $"cost={Cost.ToString(CultureInfo.InvariantCulture)}",
            "type=stone",
            "target=none",
            $"profile={ProfileId(Profile)}",
            $"stone_kind={StoneKindId(StoneKind)}",
            $"placement_tag_count={placementTagView.Count.ToString(CultureInfo.InvariantCulture)}",
        };

        for (var index = 0; index < placementTagView.Count; index++)
        {
            lines.Add(
                $"placement_tag_{index.ToString(CultureInfo.InvariantCulture)}=" +
                PlacementTagId(placementTagView[index]));
        }

        AddOperations(lines, "effect", effectView);
        AddOperations(lines, "on_captured", onCapturedView);
        return string.Join('\n', lines);
    }

    private static void AddOperations(
        ICollection<string> lines,
        string prefix,
        IReadOnlyList<CardOperationDefinition> operations)
    {
        lines.Add(
            $"{prefix}_count={operations.Count.ToString(CultureInfo.InvariantCulture)}");
        for (var index = 0; index < operations.Count; index++)
        {
            lines.Add(
                $"{prefix}_{index.ToString(CultureInfo.InvariantCulture)}=" +
                OperationText(operations[index]));
        }
    }

    private static string OperationText(CardOperationDefinition operation) => operation switch
    {
        PlaceStoneOperationDefinition placement =>
            $"place_stone;stone={StoneKindId(placement.StoneKind)}",
        DrawIfRealLibertiesAtLeastOperationDefinition draw =>
            "draw_if_real_liberties_at_least;minimum=" +
            draw.MinimumRealLiberties.ToString(CultureInfo.InvariantCulture) +
            ";cards=" + draw.Cards.ToString(CultureInfo.InvariantCulture),
        GainQiIfEnemyAtariOperationDefinition gain =>
            "gain_qi_if_enemy_atari;amount=" +
            gain.Amount.ToString(CultureInfo.InvariantCulture),
        ReserveDrawOperationDefinition reserve =>
            "reserve_draw;cards=" + reserve.Cards.ToString(CultureInfo.InvariantCulture),
        _ => throw new InvalidOperationException(
            "Unsupported starter stone-card operation reached canonical encoding."),
    };

    private static string ProfileId(StarterStoneCardProfile profile) => profile switch
    {
        StarterStoneCardProfile.BasicPlacement => "basic_placement",
        StarterStoneCardProfile.Extend => "extend",
        StarterStoneCardProfile.Contact => "contact",
        StarterStoneCardProfile.Lure => "lure",
        _ => throw new InvalidOperationException("Unsupported starter stone-card profile."),
    };

    private static string PlacementTagId(CardPlacementTag tag) => tag switch
    {
        CardPlacementTag.Contact => "contact",
        CardPlacementTag.Frontline => "frontline",
        CardPlacementTag.Terminal => "terminal",
        _ => throw new InvalidOperationException("Unsupported starter stone-card placement tag."),
    };

    private static string StoneKindId(StoneContentKind stoneKind) => stoneKind switch
    {
        StoneContentKind.Basic => "basic",
        StoneContentKind.Lure => "lure",
        _ => throw new InvalidOperationException("Unsupported starter stone kind."),
    };

    private static string EncodeStableText(string value) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
}

/// <summary>
/// Immutable, content-ID-sorted set containing one definition for every supported
/// starter stone-card operation profile.
/// </summary>
public sealed class StarterStoneCardPlayCatalog
{
    public const string EncodingVersion = "starter-stone-card-play-catalog-v1";

    private const int RequiredProfileCount = 4;

    private readonly ReadOnlyCollection<StarterStoneCardPlayDefinition> definitionView;
    private readonly ReadOnlyDictionary<string, StarterStoneCardPlayDefinition> definitionsById;

    private StarterStoneCardPlayCatalog(StarterStoneCardPlayDefinition[] definitions)
    {
        definitionView = Array.AsReadOnly(definitions);
        definitionsById = new ReadOnlyDictionary<string, StarterStoneCardPlayDefinition>(
            definitions.ToDictionary(definition => definition.ContentId, StringComparer.Ordinal));
        CanonicalText = CreateCanonicalText();
        Checksum = DeterministicChecksum.Sha256Hex(CanonicalText);
    }

    public IReadOnlyList<StarterStoneCardPlayDefinition> Definitions => definitionView;

    public string CanonicalText { get; }

    public string Checksum { get; }

    public static StarterStoneCardPlayCatalog Create(
        IEnumerable<CardContentDefinition> contents)
    {
        ArgumentNullException.ThrowIfNull(contents);

        var definitions = contents
            .Select(content => StarterStoneCardPlayDefinition.Create(
                content ?? throw new ArgumentException(
                    "Starter stone-card content cannot contain null.",
                    nameof(contents))))
            .ToArray();

        if (definitions.Length != RequiredProfileCount)
        {
            throw new ArgumentException(
                $"Starter stone-card catalog requires exactly {RequiredProfileCount} definitions.",
                nameof(contents));
        }

        Array.Sort(
            definitions,
            (left, right) => string.CompareOrdinal(left.ContentId, right.ContentId));

        if (definitions.Select(definition => definition.ContentId)
                .Distinct(StringComparer.Ordinal)
                .Count() != definitions.Length)
        {
            throw new ArgumentException(
                "Starter stone-card content IDs must be unique.",
                nameof(contents));
        }

        if (definitions.Select(definition => definition.Profile).Distinct().Count() !=
            RequiredProfileCount)
        {
            throw new ArgumentException(
                "Starter stone-card catalog requires one definition for each supported profile.",
                nameof(contents));
        }

        return new StarterStoneCardPlayCatalog(definitions);
    }

    public static StarterStoneCardPlayCatalog FromCoreDuelCatalog(
        CoreDuelContentCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        return Create(catalog.StarterCards.Where(card =>
            card.Type == CardContentType.Stone));
    }

    public StarterStoneCardPlayDefinition DefinitionFor(string contentId)
    {
        var stableId = StableDomainId.Validate(contentId, nameof(contentId));
        return definitionsById.TryGetValue(stableId, out var definition)
            ? definition
            : throw new KeyNotFoundException(
                $"Starter stone-card content was not found: {stableId}.");
    }

    public bool TryDefinition(
        string contentId,
        out StarterStoneCardPlayDefinition? definition)
    {
        var stableId = StableDomainId.Validate(contentId, nameof(contentId));
        return definitionsById.TryGetValue(stableId, out definition);
    }

    public string ToCanonicalText() => CanonicalText;

    internal bool ContainsExact(StarterStoneCardPlayDefinition definition) =>
        definitionsById.TryGetValue(definition.ContentId, out var candidate) &&
        ReferenceEquals(candidate, definition);

    private string CreateCanonicalText()
    {
        var lines = new List<string>
        {
            EncodingVersion,
            $"definition_count={definitionView.Count.ToString(CultureInfo.InvariantCulture)}",
        };

        for (var index = 0; index < definitionView.Count; index++)
        {
            lines.Add(
                $"definition_{index.ToString(CultureInfo.InvariantCulture)}=" +
                Convert.ToBase64String(
                    Encoding.UTF8.GetBytes(definitionView[index].CanonicalText)));
        }

        return string.Join('\n', lines);
    }
}

public enum StarterStoneCardPlayStatus : byte
{
    Authorized = 1,
    DefinitionNotInCatalog = 2,
    CardNotInHand = 3,
    CardContentMismatch = 4,
    InsufficientQi = 5,
    ActiveResolutionExists = 6,
    UnsupportedMode = 7,
    TargetOccupied = 8,
    FrontlineAdjacencyRequired = 9,
    ContactAdjacencyRequired = 10,
}

/// <summary>
/// Pure pre-authorization bound to exact immutable catalog, definition, deck,
/// and board snapshots. Terminal capture still requires the placement-legality
/// evaluator to prove immediate capture.
/// </summary>
public sealed class StarterStoneCardPlayEvaluation
{
    internal StarterStoneCardPlayEvaluation(
        StarterStoneCardPlayStatus status,
        StarterStoneCardPlayCatalog sourceCatalog,
        StarterStoneCardPlayDefinition sourceDefinition,
        BattleDeckState sourceDeck,
        int sourceQi,
        string requestedInstanceId,
        BattleCardInstance? card,
        BoardState sourceBoard,
        CanonicalPoint target,
        StoneCardPlacementMode requestedMode,
        PlacementAccessMode? accessMode)
    {
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown card-play status.");
        }

        ArgumentNullException.ThrowIfNull(sourceCatalog);
        ArgumentNullException.ThrowIfNull(sourceDefinition);
        ArgumentNullException.ThrowIfNull(sourceDeck);
        ArgumentNullException.ThrowIfNull(requestedInstanceId);
        ArgumentNullException.ThrowIfNull(sourceBoard);
        ArgumentNullException.ThrowIfNull(target);

        if (status == StarterStoneCardPlayStatus.Authorized &&
            (card is null || accessMode is null))
        {
            throw new ArgumentException(
                "Authorized card play requires a card and placement access mode.",
                nameof(status));
        }

        Status = status;
        SourceCatalog = sourceCatalog;
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

    public StarterStoneCardPlayStatus Status { get; }

    public bool IsAuthorized => Status == StarterStoneCardPlayStatus.Authorized;

    public string ReasonId => Status switch
    {
        StarterStoneCardPlayStatus.Authorized => "authorized",
        StarterStoneCardPlayStatus.DefinitionNotInCatalog => "definition_not_in_catalog",
        StarterStoneCardPlayStatus.CardNotInHand => "card_not_in_hand",
        StarterStoneCardPlayStatus.CardContentMismatch => "card_content_mismatch",
        StarterStoneCardPlayStatus.InsufficientQi => "insufficient_qi",
        StarterStoneCardPlayStatus.ActiveResolutionExists => "active_resolution_exists",
        StarterStoneCardPlayStatus.UnsupportedMode => "unsupported_placement_mode",
        StarterStoneCardPlayStatus.TargetOccupied => "target_occupied",
        StarterStoneCardPlayStatus.FrontlineAdjacencyRequired =>
            "frontline_adjacency_required",
        StarterStoneCardPlayStatus.ContactAdjacencyRequired =>
            "contact_adjacency_required",
        _ => throw new InvalidOperationException("Unknown card-play status."),
    };

    public StarterStoneCardPlayCatalog SourceCatalog { get; }

    public StarterStoneCardPlayDefinition SourceDefinition { get; }

    public BattleDeckState SourceDeck { get; }

    public int SourceQi { get; }

    public string RequestedInstanceId { get; }

    public BattleCardInstance? Card { get; }

    public BoardState SourceBoard { get; }

    public CanonicalPoint Target { get; }

    public StoneCardPlacementMode RequestedMode { get; }

    public PlacementAccessMode? AccessMode { get; }

    public bool IsBoundTo(
        StarterStoneCardPlayCatalog catalog,
        StarterStoneCardPlayDefinition definition,
        BattleDeckState deck,
        int qi,
        string instanceId,
        BoardState board,
        CanonicalPoint target,
        StoneCardPlacementMode mode,
        PlacementAccessMode accessMode) =>
        IsAuthorized &&
        ReferenceEquals(SourceCatalog, catalog) &&
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

public static class StarterStoneCardPlayEvaluator
{
    public static StarterStoneCardPlayEvaluation Evaluate(
        StarterStoneCardPlayCatalog catalog,
        StarterStoneCardPlayDefinition definition,
        BattleDeckState deck,
        int qi,
        string instanceId,
        BoardState board,
        CanonicalPoint target,
        StoneCardPlacementMode mode)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(deck);
        ArgumentNullException.ThrowIfNull(instanceId);
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(target);
        board.Geometry.ToCanonicalIndex(target);

        if (!catalog.ContainsExact(definition))
        {
            return Evaluation(
                StarterStoneCardPlayStatus.DefinitionNotInCatalog,
                catalog,
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

        var card = deck.Hand.FirstOrDefault(
            candidate => StringComparer.Ordinal.Equals(candidate.InstanceId, instanceId));
        if (card is null)
        {
            return Evaluation(
                StarterStoneCardPlayStatus.CardNotInHand,
                catalog,
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
                StarterStoneCardPlayStatus.CardContentMismatch,
                catalog,
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
                StarterStoneCardPlayStatus.InsufficientQi,
                catalog,
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
                StarterStoneCardPlayStatus.ActiveResolutionExists,
                catalog,
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
                StarterStoneCardPlayStatus.UnsupportedMode,
                catalog,
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
                StarterStoneCardPlayStatus.TargetOccupied,
                catalog,
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

        if (mode == StoneCardPlacementMode.Frontline &&
            !HasAdjacentStone(board, target, StoneColor.Black))
        {
            return Evaluation(
                StarterStoneCardPlayStatus.FrontlineAdjacencyRequired,
                catalog,
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

        if (mode == StoneCardPlacementMode.Contact &&
            (!HasAdjacentStone(board, target, StoneColor.Black) ||
             !HasAdjacentStone(board, target, StoneColor.White)))
        {
            return Evaluation(
                StarterStoneCardPlayStatus.ContactAdjacencyRequired,
                catalog,
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
            StarterStoneCardPlayStatus.Authorized,
            catalog,
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
        StarterStoneCardPlayDefinition definition,
        StoneCardPlacementMode mode) => mode switch
        {
            StoneCardPlacementMode.Frontline
                when definition.PlacementTags.Contains(CardPlacementTag.Frontline) =>
                PlacementAccessMode.Normal,
            StoneCardPlacementMode.Contact
                when definition.PlacementTags.Contains(CardPlacementTag.Contact) =>
                PlacementAccessMode.Normal,
            StoneCardPlacementMode.TerminalCapture when definition.AllowsTerminalCapture =>
                PlacementAccessMode.TerminalCapture,
            _ => null,
        };

    private static bool HasAdjacentStone(
        BoardState board,
        CanonicalPoint target,
        StoneColor color) =>
        board.Geometry.GetOrthogonalNeighbours(target).Any(
            neighbour => board.StoneAt(neighbour)?.Color == color);

    private static StarterStoneCardPlayEvaluation Evaluation(
        StarterStoneCardPlayStatus status,
        StarterStoneCardPlayCatalog catalog,
        StarterStoneCardPlayDefinition definition,
        BattleDeckState deck,
        int qi,
        string instanceId,
        BattleCardInstance? card,
        BoardState board,
        CanonicalPoint target,
        StoneCardPlacementMode mode,
        PlacementAccessMode? accessMode) =>
        new(
            status,
            catalog,
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
