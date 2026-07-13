using System.Text.Json;

using Igorogue.Domain.Board;
using Igorogue.Domain.Combat;
using Igorogue.Domain.Content;
using Igorogue.Domain.Facilities;

namespace Igorogue.Content;

public sealed class CoreDuelContentCatalogLoader
{
    private const string CardsPath = "content/cards.json";
    private const string EnemiesPath = "content/enemies.json";
    private const string StartingDecksPath = "content/starting_decks.json";
    private const string SystemPath = "balance/system.json";
    private const string BanditContentId = "enemy_bandit";
    private const string CoreDuelStartingDeckId = "core_duel";

    private static readonly string[] RequiredStarterCardIds =
    [
        "card_basic_stone",
        "card_contact",
        "card_development",
        "card_extend",
        "card_lure_stone",
        "card_reinforce",
    ];

    private static readonly JsonDocumentOptions DocumentOptions = new()
    {
        AllowTrailingCommas = false,
        CommentHandling = JsonCommentHandling.Disallow,
        MaxDepth = 64,
    };

    private readonly ContentManifestLoader manifestLoader = new();

    public CoreDuelContentCatalog Load(string manifestPath)
        => Load(manifestLoader.Load(manifestPath));

    public CoreDuelContentCatalog Load(ContentSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var system = ParseSystem(snapshot.RequiredContent(SystemPath));
        var cards = ParseStarterCards(
            snapshot.RequiredContent(CardsPath),
            system.FacilityTypeIds);
        var startingDeck = ParseCoreDuelStartingDeck(
            snapshot.RequiredContent(StartingDecksPath),
            cards.CardRarities);
        var bandit = ParseBandit(snapshot.RequiredContent(EnemiesPath));

        return ConvertDomain(
            "core duel catalog",
            () => CoreDuelContentCatalog.Create(
                snapshot.ContentHash,
                cards.StarterCards,
                startingDeck,
                bandit,
                system.Policy,
                system.BattleSetup));
    }

    private static ParsedSystem ParseSystem(ReadOnlyMemory<byte> content)
    {
        using var document = OpenDocument(content, SystemPath);
        var root = RequireObject(document.RootElement, SystemPath);
        EnsureUniqueProperties(root, SystemPath);

        var baseQi = RequiredInt(root, "base_qi", SystemPath);
        var baseDraw = RequiredInt(root, "base_draw", SystemPath);
        var initialPosition = ParseInitialPosition(root);
        var playerTurnLimit = RequiredInt(root, "turn_limit", SystemPath);
        var capacityBands = ParseFacilityCapacity(root);
        var territoryIncomeDivisor = RequiredInt(
            root,
            "territory_income_divisor",
            SystemPath);
        var facilitySlotCap = RequiredInt(root, "facility_slot_cap", SystemPath);
        var facilityLimits = RequiredObject(
            root,
            "facility_type_limits_per_region",
            SystemPath);
        EnsureUniqueProperties(facilityLimits, $"{SystemPath}.facility_type_limits_per_region");

        var facilityTypeIds = new HashSet<string>(StringComparer.Ordinal);
        var facilityTypeLimits = new List<KeyValuePair<string, int>>();
        foreach (var property in facilityLimits.EnumerateObject())
        {
            ValidateStableId(property.Name, $"{SystemPath}.facility_type_limits_per_region");
            if (property.Value.ValueKind != JsonValueKind.Number ||
                !property.Value.TryGetInt32(out var limit) ||
                limit <= 0)
            {
                throw Invalid(
                    $"{SystemPath}.facility_type_limits_per_region.{property.Name} must be a positive integer.");
            }

            facilityTypeLimits.Add(new KeyValuePair<string, int>(property.Name, limit));
            if (!string.Equals(property.Name, "default", StringComparison.Ordinal))
            {
                facilityTypeIds.Add(property.Name);
            }
        }

        var facilityPolicy = ConvertDomain(
            $"{SystemPath}.facility runtime policy",
            () => FacilityRuntimePolicy.Create(
                territoryIncomeDivisor,
                capacityBands,
                facilitySlotCap,
                facilityTypeLimits));
        var counterattack = ParseCounterattack(root);
        var battleSetup = ConvertDomain(
            $"{SystemPath}.core duel battle setup",
            () => CoreDuelBattleSetupDefinition.Create(
                initialPosition,
                playerTurnLimit,
                facilityPolicy,
                counterattack.Policy,
                counterattack.StartGaugeUnits));

        return new ParsedSystem(
            ConvertDomain(
                SystemPath,
                () => CoreDuelSystemPolicy.Create(baseQi, baseDraw)),
            battleSetup,
            facilityTypeIds);
    }

    private static InitialPositionDefinition ParseInitialPosition(JsonElement root)
    {
        var context = $"{SystemPath}.initial_position";
        var initial = RequiredObject(root, "initial_position", SystemPath);
        EnsureExactProperties(initial, context, "id", "symmetry", "stones");
        var symmetry = RequiredString(initial, "symmetry", context);
        if (!string.Equals(
                symmetry,
                "point_reflection_with_color_and_role_swap",
                StringComparison.Ordinal))
        {
            throw Invalid($"{context}.symmetry uses unsupported value '{symmetry}'.");
        }

        var geometry = ConvertDomain(
            SystemPath,
            () => BoardGeometry.Create(RequiredInt(root, "board_size", SystemPath)));
        var stones = new List<InitialStonePlacement>();
        var index = 0;
        foreach (var element in RequiredArray(initial, "stones", context).EnumerateArray())
        {
            var stoneContext = $"{context}.stones[{index}]";
            var stone = RequireObject(element, stoneContext);
            EnsureExactProperties(stone, stoneContext, "color", "role", "point");
            var color = RequiredString(stone, "color", stoneContext) switch
            {
                "black" => StoneColor.Black,
                "white" => StoneColor.White,
                var value => throw Invalid($"{stoneContext}.color uses unsupported value '{value}'."),
            };
            var role = RequiredString(stone, "role", stoneContext) switch
            {
                "king" => InitialStoneRole.King,
                "guard" => InitialStoneRole.Guard,
                var value => throw Invalid($"{stoneContext}.role uses unsupported value '{value}'."),
            };
            var point = RequiredArray(stone, "point", stoneContext).EnumerateArray().ToArray();
            if (point.Length != 2 ||
                point.Any(value => value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out _)))
            {
                throw Invalid($"{stoneContext}.point must contain exactly two 32-bit integers.");
            }

            stones.Add(ConvertDomain(
                stoneContext,
                () => new InitialStonePlacement(
                    color,
                    role,
                    geometry.CreateCanonicalPoint(point[0].GetInt32(), point[1].GetInt32()))));
            index++;
        }

        var positionId = RequiredString(initial, "id", context);
        ValidateStableId(positionId, $"{context}.id");
        var position = ConvertDomain(
            context,
            () => InitialPositionDefinition.Create(
                geometry,
                positionId,
                stones));
        if (!position.HasRoleAwarePointReflectionSymmetry())
        {
            throw Invalid($"{context} does not match its declared role-aware point symmetry.");
        }

        if (position.Stones.Count(stone =>
                stone.Color == StoneColor.Black && stone.Role == InitialStoneRole.King) != 1 ||
            position.Stones.Count(stone =>
                stone.Color == StoneColor.White && stone.Role == InitialStoneRole.King) != 1)
        {
            throw Invalid($"{context} must contain exactly one king for each color.");
        }

        return position;
    }

    private static FacilityCapacityBand[] ParseFacilityCapacity(JsonElement root)
    {
        var context = $"{SystemPath}.facility_capacity";
        var bands = new List<FacilityCapacityBand>();
        var index = 0;
        foreach (var element in RequiredArray(root, "facility_capacity", SystemPath).EnumerateArray())
        {
            var bandContext = $"{context}[{index}]";
            var band = RequireObject(element, bandContext);
            EnsureExactProperties(band, bandContext, "min", "max", "slots");
            bands.Add(ConvertDomain(
                bandContext,
                () => new FacilityCapacityBand(
                    RequiredInt(band, "min", bandContext),
                    RequiredInt(band, "max", bandContext),
                    RequiredInt(band, "slots", bandContext))));
            index++;
        }

        return bands.ToArray();
    }

    private static ParsedCounterattack ParseCounterattack(JsonElement root)
    {
        var context = $"{SystemPath}.counterattack";
        var counterattack = RequiredObject(root, "counterattack", SystemPath);
        EnsureUniqueProperties(counterattack, context);
        var battleStart = RequiredObject(counterattack, "battle_start", context);
        var enemyTurnEnd = RequiredObject(counterattack, "enemy_turn_end_gain", context);
        var sacrifice = RequiredObject(counterattack, "sacrifice", context);
        EnsureUniqueProperties(battleStart, $"{context}.battle_start");
        EnsureUniqueProperties(enemyTurnEnd, $"{context}.enemy_turn_end_gain");
        EnsureUniqueProperties(sacrifice, $"{context}.sacrifice");

        var policy = ConvertDomain(
            $"{context}.boundary policy",
            () => new CounterattackBoundaryPolicy(
                RequiredInt(counterattack, "threshold_units", context),
                RequiredInt(enemyTurnEnd, "base_units", $"{context}.enemy_turn_end_gain"),
                RequiredInt(
                    sacrifice,
                    "non_king_black_stones_per_batch",
                    $"{context}.sacrifice"),
                RequiredInt(sacrifice, "gain_units_per_batch", $"{context}.sacrifice")));
        return new ParsedCounterattack(
            policy,
            RequiredInt(battleStart, "base_units", $"{context}.battle_start"));
    }

    private static ParsedCards ParseStarterCards(
        ReadOnlyMemory<byte> content,
        IReadOnlySet<string> facilityTypeIds)
    {
        using var document = OpenDocument(content, CardsPath);
        var root = RequireArray(document.RootElement, CardsPath);
        var seenIds = new HashSet<string>(StringComparer.Ordinal);
        var cardRarities = new Dictionary<string, CardRarity>(StringComparer.Ordinal);
        var starterCards = new List<CardContentDefinition>();

        var index = 0;
        foreach (var element in root.EnumerateArray())
        {
            var context = $"{CardsPath}[{index}]";
            var card = RequireObject(element, context);
            EnsureUniqueProperties(card, context);
            var id = RequiredString(card, "id", context);
            ValidatePrefixedContentId(id, "card_", context);
            if (!seenIds.Add(id))
            {
                throw Invalid($"Duplicate card content ID: {id}.");
            }

            var rarity = ParseCardRarity(RequiredString(card, "rarity", context), context);
            cardRarities.Add(id, rarity);
            if (rarity == CardRarity.Starter)
            {
                starterCards.Add(ParseStarterCard(card, id, context, facilityTypeIds));
            }

            index++;
        }

        var actualStarterCardIds = starterCards
            .Select(card => card.Id)
            .Order(StringComparer.Ordinal)
            .ToArray();
        if (!actualStarterCardIds.SequenceEqual(RequiredStarterCardIds, StringComparer.Ordinal))
        {
            throw Invalid(
                $"Core Duel starter IDs must be exactly: " +
                $"{string.Join(", ", RequiredStarterCardIds)}. Got: " +
                $"{string.Join(", ", actualStarterCardIds)}.");
        }

        return new ParsedCards(starterCards, cardRarities);
    }

    private static StartingDeckRecipe ParseCoreDuelStartingDeck(
        ReadOnlyMemory<byte> content,
        IReadOnlyDictionary<string, CardRarity> cardRarities)
    {
        using var document = OpenDocument(content, StartingDecksPath);
        var root = RequireArray(document.RootElement, StartingDecksPath);
        var seenIds = new HashSet<string>(StringComparer.Ordinal);
        StartingDeckRecipe? coreDuel = null;

        var index = 0;
        foreach (var element in root.EnumerateArray())
        {
            var context = $"{StartingDecksPath}[{index}]";
            var recipe = RequireObject(element, context);
            EnsureExactProperties(recipe, context, "id", "total_cards", "entries");
            var id = RequiredString(recipe, "id", context);
            ValidateStableId(id, $"{context}.id");
            if (!seenIds.Add(id))
            {
                throw Invalid($"Duplicate starting-deck recipe ID: {id}.");
            }

            if (string.Equals(id, CoreDuelStartingDeckId, StringComparison.Ordinal))
            {
                coreDuel = ParseStartingDeckRecipe(recipe, id, context, cardRarities);
            }

            index++;
        }

        return coreDuel ?? throw Invalid(
            $"Required starting-deck recipe was not found: {CoreDuelStartingDeckId}.");
    }

    private static StartingDeckRecipe ParseStartingDeckRecipe(
        JsonElement recipe,
        string id,
        string context,
        IReadOnlyDictionary<string, CardRarity> cardRarities)
    {
        var entriesElement = RequiredArray(recipe, "entries", context);
        var entries = new List<StartingDeckCardCount>();
        var index = 0;
        foreach (var element in entriesElement.EnumerateArray())
        {
            var entryContext = $"{context}.entries[{index}]";
            var entry = RequireObject(element, entryContext);
            EnsureExactProperties(entry, entryContext, "card_id", "count");
            var cardId = RequiredString(entry, "card_id", entryContext);
            ValidatePrefixedContentId(cardId, "card_", entryContext);
            if (!cardRarities.TryGetValue(cardId, out var rarity))
            {
                throw Invalid($"{entryContext}.card_id references unknown card '{cardId}'.");
            }

            if (rarity != CardRarity.Starter)
            {
                throw Invalid($"{entryContext}.card_id references non-starter card '{cardId}'.");
            }

            entries.Add(ConvertDomain(
                entryContext,
                () => new StartingDeckCardCount(
                    cardId,
                    RequiredInt(entry, "count", entryContext))));
            index++;
        }

        return ConvertDomain(
            context,
            () => StartingDeckRecipe.Create(
                id,
                RequiredInt(recipe, "total_cards", context),
                entries));
    }

    private static CardContentDefinition ParseStarterCard(
        JsonElement card,
        string id,
        string context,
        IReadOnlySet<string> facilityTypeIds)
    {
        foreach (var property in card.EnumerateObject())
        {
            if (property.Name.StartsWith("on_", StringComparison.Ordinal) &&
                !string.Equals(property.Name, "on_captured", StringComparison.Ordinal))
            {
                throw Invalid($"{context} uses unsupported trigger collection '{property.Name}'.");
            }
        }

        var cost = RequiredInt(card, "cost", context);
        var type = ParseCardType(RequiredString(card, "type", context), context);
        var target = ParseStarterTarget(card, type, context);
        var placementTags = ParseStarterPlacementTags(card, type, context);
        var effects = ParseCardOperations(
            RequiredArray(card, "effects", context),
            $"{context}.effects",
            facilityTypeIds);
        ValidateAcceptedStarterOperationOrder(id, effects, context);
        var onCaptured = card.TryGetProperty("on_captured", out var capturedElement)
            ? ParseCardOperations(
                RequireArray(capturedElement, $"{context}.on_captured"),
                $"{context}.on_captured",
                facilityTypeIds)
            : [];

        return ConvertDomain(
            context,
            () => CardContentDefinition.Create(
                id,
                CardRarity.Starter,
                cost,
                type,
                target,
                placementTags,
                effects,
                onCaptured));
    }

    private static CardTargetKind ParseStarterTarget(
        JsonElement card,
        CardContentType type,
        string context)
    {
        if (card.TryGetProperty("target", out var targetElement))
        {
            return ParseCardTarget(
                RequireString(targetElement, $"{context}.target"),
                context);
        }

        if (type != CardContentType.Stone)
        {
            throw Invalid($"{context}.target is required for a non-stone starter card.");
        }

        return CardTargetKind.None;
    }

    private static CardPlacementTag[] ParseStarterPlacementTags(
        JsonElement card,
        CardContentType type,
        string context)
    {
        if (!card.TryGetProperty("placement_tags", out var placementElement))
        {
            if (type == CardContentType.Stone)
            {
                throw Invalid($"{context}.placement_tags is required for a stone starter card.");
            }

            return [];
        }

        var placementTags = ParseCardPlacementTags(
            placementElement,
            $"{context}.placement_tags");
        if (type == CardContentType.Stone && placementTags.Length == 0)
        {
            throw Invalid($"{context}.placement_tags must not be empty for a stone starter card.");
        }

        return placementTags;
    }

    private static void ValidateAcceptedStarterOperationOrder(
        string cardId,
        IReadOnlyList<CardOperationDefinition> effects,
        string context)
    {
        if (!string.Equals(cardId, "card_reinforce", StringComparison.Ordinal))
        {
            return;
        }

        CardOperationKind[] expected =
        [
            CardOperationKind.DrawIfTargetAtari,
            CardOperationKind.TemporaryLiberty,
        ];
        if (!effects.Select(effect => effect.Kind).SequenceEqual(expected))
        {
            throw Invalid(
                $"{context}.effects must check draw_if_target_atari before granting " +
                "temporary_liberty as required by FEAT-011.");
        }
    }

    private static CardPlacementTag[] ParseCardPlacementTags(
        JsonElement element,
        string context)
    {
        var array = RequireArray(element, context);
        return array.EnumerateArray()
            .Select((value, index) => ParseCardPlacementTag(
                RequireString(value, $"{context}[{index}]"),
                context))
            .ToArray();
    }

    private static CardOperationDefinition[] ParseCardOperations(
        JsonElement array,
        string context,
        IReadOnlySet<string> facilityTypeIds)
    {
        var operations = new List<CardOperationDefinition>();
        var index = 0;
        foreach (var element in array.EnumerateArray())
        {
            var operationContext = $"{context}[{index}]";
            var operation = RequireObject(element, operationContext);
            EnsureUniqueProperties(operation, operationContext);
            var operationId = RequiredString(operation, "op", operationContext);
            operations.Add(ParseCardOperation(
                operation,
                operationId,
                operationContext,
                facilityTypeIds));
            index++;
        }

        return operations.ToArray();
    }

    private static CardOperationDefinition ParseCardOperation(
        JsonElement operation,
        string operationId,
        string context,
        IReadOnlySet<string> facilityTypeIds) => operationId switch
    {
        "place_stone" => ParsePlaceStone(operation, context),
        "draw_if_real_liberties_at_least" => ParseDrawIfRealLiberties(operation, context),
        "gain_qi_if_enemy_atari" => ParseGainQiIfEnemyAtari(operation, context),
        "temporary_liberty" => ParseTemporaryLiberty(operation, context),
        "draw_if_target_atari" => ParseDrawIfTargetAtari(operation, context),
        "build_facility" => ParseBuildFacility(operation, context, facilityTypeIds),
        "reserve_draw" => ParseReserveDraw(operation, context),
        _ => throw Invalid($"{context} uses unsupported card operation '{operationId}'."),
    };

    private static CardOperationDefinition ParsePlaceStone(JsonElement operation, string context)
    {
        EnsureExactProperties(operation, context, "op", "stone");
        var stone = RequiredString(operation, "stone", context) switch
        {
            "basic" => StoneContentKind.Basic,
            "lure" => StoneContentKind.Lure,
            var value => throw Invalid($"{context}.stone uses unsupported value '{value}'."),
        };
        return new PlaceStoneOperationDefinition(stone);
    }

    private static CardOperationDefinition ParseDrawIfRealLiberties(
        JsonElement operation,
        string context)
    {
        EnsureExactProperties(operation, context, "op", "value", "cards");
        return ConvertDomain(
            context,
            () => new DrawIfRealLibertiesAtLeastOperationDefinition(
                RequiredInt(operation, "value", context),
                RequiredInt(operation, "cards", context)));
    }

    private static CardOperationDefinition ParseGainQiIfEnemyAtari(
        JsonElement operation,
        string context)
    {
        EnsureExactProperties(operation, context, "op", "value");
        return ConvertDomain(
            context,
            () => new GainQiIfEnemyAtariOperationDefinition(
                RequiredInt(operation, "value", context)));
    }

    private static CardOperationDefinition ParseTemporaryLiberty(
        JsonElement operation,
        string context)
    {
        EnsureExactProperties(operation, context, "op", "value", "duration", "stacking");
        var duration = RequiredObject(operation, "duration", context);
        EnsureExactProperties(duration, $"{context}.duration", "kind", "timing");

        var durationKind = RequiredString(duration, "kind", $"{context}.duration") switch
        {
            "enemy_turn_end" => TemporaryLibertyDurationKind.EnemyTurnEnd,
            var value => throw Invalid($"{context}.duration.kind uses unsupported value '{value}'."),
        };
        var timing = RequiredString(duration, "timing", $"{context}.duration") switch
        {
            "first_enemy_turn_end_at_or_after_grant" =>
                TemporaryLibertyTiming.FirstEnemyTurnEndAtOrAfterGrant,
            var value => throw Invalid($"{context}.duration.timing uses unsupported value '{value}'."),
        };
        var stacking = RequiredString(operation, "stacking", context) switch
        {
            "additive_per_effect_instance" => TemporaryLibertyStacking.AdditivePerEffectInstance,
            var value => throw Invalid($"{context}.stacking uses unsupported value '{value}'."),
        };

        return ConvertDomain(
            context,
            () => new TemporaryLibertyOperationDefinition(
                RequiredInt(operation, "value", context),
                durationKind,
                timing,
                stacking));
    }

    private static CardOperationDefinition ParseDrawIfTargetAtari(
        JsonElement operation,
        string context)
    {
        EnsureExactProperties(operation, context, "op", "cards");
        return ConvertDomain(
            context,
            () => new DrawIfTargetAtariOperationDefinition(
                RequiredInt(operation, "cards", context)));
    }

    private static CardOperationDefinition ParseBuildFacility(
        JsonElement operation,
        string context,
        IReadOnlySet<string> facilityTypeIds)
    {
        EnsureExactProperties(operation, context, "op", "facility");
        var facilityId = RequiredString(operation, "facility", context);
        ValidateStableId(facilityId, $"{context}.facility");
        if (!facilityTypeIds.Contains(facilityId))
        {
            throw Invalid($"{context}.facility references unknown facility type '{facilityId}'.");
        }

        return ConvertDomain(context, () => new BuildFacilityOperationDefinition(facilityId));
    }

    private static CardOperationDefinition ParseReserveDraw(JsonElement operation, string context)
    {
        EnsureExactProperties(operation, context, "op", "cards");
        return ConvertDomain(
            context,
            () => new ReserveDrawOperationDefinition(RequiredInt(operation, "cards", context)));
    }

    private static EnemyContentDefinition ParseBandit(ReadOnlyMemory<byte> content)
    {
        using var document = OpenDocument(content, EnemiesPath);
        var root = RequireArray(document.RootElement, EnemiesPath);
        var seenIds = new HashSet<string>(StringComparer.Ordinal);
        EnemyContentDefinition? bandit = null;

        var index = 0;
        foreach (var element in root.EnumerateArray())
        {
            var context = $"{EnemiesPath}[{index}]";
            var enemy = RequireObject(element, context);
            EnsureUniqueProperties(enemy, context);
            var id = RequiredString(enemy, "id", context);
            ValidateEnemyContentId(id, context);
            if (!seenIds.Add(id))
            {
                throw Invalid($"Duplicate enemy content ID: {id}.");
            }

            if (string.Equals(id, BanditContentId, StringComparison.Ordinal))
            {
                if (bandit is not null)
                {
                    throw Invalid($"Duplicate enemy content ID: {BanditContentId}.");
                }

                bandit = ParseBanditDefinition(enemy, context);
            }

            index++;
        }

        return bandit ?? throw Invalid($"Required enemy content was not found: {BanditContentId}.");
    }

    private static EnemyContentDefinition ParseBanditDefinition(
        JsonElement enemy,
        string context)
    {
        var implementationStatus = RequiredString(enemy, "implementation_status", context);
        if (!string.Equals(implementationStatus, "specified", StringComparison.Ordinal))
        {
            throw Invalid($"{context}.implementation_status must be 'specified'.");
        }

        var actionBudgetElement = RequiredObject(enemy, "action_budget", context);
        EnsureExactProperties(
            actionBudgetElement,
            $"{context}.action_budget",
            "normal_actions",
            "counterattack_bonus_actions",
            "max_actions_per_enemy_turn");
        var normalActions = RequiredInt(
            actionBudgetElement,
            "normal_actions",
            $"{context}.action_budget");
        var counterattackBonusActions = RequiredInt(
            actionBudgetElement,
            "counterattack_bonus_actions",
            $"{context}.action_budget");
        var maxActionsPerEnemyTurn = RequiredInt(
            actionBudgetElement,
            "max_actions_per_enemy_turn",
            $"{context}.action_budget");
        var actionBudget = ConvertDomain(
            $"{context}.action_budget",
            () => new EnemyActionBudget(
                normalActions,
                counterattackBonusActions,
                maxActionsPerEnemyTurn));

        var parametersElement = RequiredObject(enemy, "parameters", context);
        EnsureExactProperties(
            parametersElement,
            $"{context}.parameters",
            "defense_threshold",
            "opportunistic_capture_min_stones");
        var parameters = ConvertDomain(
            $"{context}.parameters",
            () => new BanditParameters(
                RequiredInt(parametersElement, "defense_threshold", $"{context}.parameters"),
                RequiredInt(
                    parametersElement,
                    "opportunistic_capture_min_stones",
                    $"{context}.parameters")));

        var permissions = ParsePlacementModes(
            RequiredArray(enemy, "placement_permissions", context),
            $"{context}.placement_permissions");
        var mandatoryOverrides = ParseIntentReferences(
            RequiredArray(enemy, "mandatory_overrides", context),
            $"{context}.mandatory_overrides");
        var planPriority = ParseIntentReferences(
            RequiredArray(enemy, "plan_priority", context),
            $"{context}.plan_priority");
        var counterattackPriority = ParseIntentReferences(
            RequiredArray(enemy, "counterattack_priority", context),
            $"{context}.counterattack_priority");
        var intents = ParseEnemyIntents(
            RequiredArray(enemy, "intents", context),
            $"{context}.intents");
        var tieBreak = RequiredString(enemy, "tie_break", context) switch
        {
            "canonical_y_then_x" => EnemyTieBreak.CanonicalYThenX,
            var value => throw Invalid($"{context}.tie_break uses unsupported value '{value}'."),
        };

        return ConvertDomain(
            context,
            () => EnemyContentDefinition.Create(
                RequiredString(enemy, "id", context),
                RequiredString(enemy, "behavior_spec", context),
                RequiredString(enemy, "behavior_version", context),
                actionBudget,
                permissions,
                parameters,
                mandatoryOverrides,
                planPriority,
                counterattackPriority,
                intents,
                tieBreak));
    }

    private static EnemyIntentDefinition[] ParseEnemyIntents(
        JsonElement array,
        string context)
    {
        var intents = new List<EnemyIntentDefinition>();
        var index = 0;
        foreach (var element in array.EnumerateArray())
        {
            var intentContext = $"{context}[{index}]";
            var intent = RequireObject(element, intentContext);
            EnsureExactProperties(
                intent,
                intentContext,
                "id",
                "candidate_rule",
                "placement_modes",
                "score_profile",
                "fallback");
            intents.Add(ConvertDomain(
                intentContext,
                () => EnemyIntentDefinition.Create(
                    ParseEnemyIntent(
                        RequiredString(intent, "id", intentContext),
                        $"{intentContext}.id"),
                    ParseEnemyIntent(
                        RequiredString(intent, "candidate_rule", intentContext),
                        $"{intentContext}.candidate_rule"),
                    ParsePlacementModes(
                        RequiredArray(intent, "placement_modes", intentContext),
                        $"{intentContext}.placement_modes"),
                    ParseEnemyScoreProfile(
                        RequiredString(intent, "score_profile", intentContext),
                        $"{intentContext}.score_profile"),
                    ParseIntentReferences(
                        RequiredArray(intent, "fallback", intentContext),
                        $"{intentContext}.fallback"))));
            index++;
        }

        return intents.ToArray();
    }

    private static EnemyPlacementMode[] ParsePlacementModes(
        JsonElement array,
        string context) => array.EnumerateArray()
        .Select((element, index) => ParseEnemyPlacementMode(
            RequireString(element, $"{context}[{index}]"),
            context))
        .ToArray();

    private static EnemyIntentKind[] ParseIntentReferences(
        JsonElement array,
        string context) => array.EnumerateArray()
        .Select((element, index) => ParseEnemyIntent(
            RequireString(element, $"{context}[{index}]"),
            context))
        .ToArray();

    private static CardRarity ParseCardRarity(string value, string context) => value switch
    {
        "starter" => CardRarity.Starter,
        "common" => CardRarity.Common,
        "uncommon" => CardRarity.Uncommon,
        "rare" => CardRarity.Rare,
        "curse" => CardRarity.Curse,
        _ => throw Invalid($"{context}.rarity uses unsupported value '{value}'."),
    };

    private static CardContentType ParseCardType(string value, string context) => value switch
    {
        "stone" => CardContentType.Stone,
        "technique" => CardContentType.Technique,
        "territory" => CardContentType.Territory,
        "catalyst" => CardContentType.Catalyst,
        "curse" => CardContentType.Curse,
        _ => throw Invalid($"{context}.type uses unsupported value '{value}'."),
    };

    private static CardTargetKind ParseCardTarget(string value, string context) => value switch
    {
        "friendly_group" => CardTargetKind.FriendlyGroup,
        "black_territory_empty" => CardTargetKind.BlackTerritoryEmpty,
        _ => throw Invalid($"{context}.target uses unsupported value '{value}'."),
    };

    private static CardPlacementTag ParseCardPlacementTag(string value, string context) => value switch
    {
        "contact" => CardPlacementTag.Contact,
        "edge" => CardPlacementTag.Edge,
        "frontline" => CardPlacementTag.Frontline,
        "invasion" => CardPlacementTag.Invasion,
        "jump" => CardPlacementTag.Jump,
        "terminal" => CardPlacementTag.Terminal,
        _ => throw Invalid($"{context} uses unsupported placement tag '{value}'."),
    };

    private static EnemyPlacementMode ParseEnemyPlacementMode(string value, string context) =>
        value switch
        {
            "white_contact" => EnemyPlacementMode.WhiteContact,
            "white_facility_invasion" => EnemyPlacementMode.WhiteFacilityInvasion,
            "white_frontline" => EnemyPlacementMode.WhiteFrontline,
            "white_invasion" => EnemyPlacementMode.WhiteInvasion,
            "white_terminal" => EnemyPlacementMode.WhiteTerminal,
            _ => throw Invalid($"{context} uses unsupported enemy placement mode '{value}'."),
        };

    private static EnemyIntentKind ParseEnemyIntent(string value, string context) => value switch
    {
        "advance_toward_black_king" => EnemyIntentKind.AdvanceTowardBlackKing,
        "capture_black_king" => EnemyIntentKind.CaptureBlackKing,
        "capture_non_king" => EnemyIntentKind.CaptureNonKing,
        "defend_white_king" => EnemyIntentKind.DefendWhiteKing,
        "pressure_black_king" => EnemyIntentKind.PressureBlackKing,
        _ => throw Invalid($"{context} uses unsupported enemy intent '{value}'."),
    };

    private static EnemyScoreProfile ParseEnemyScoreProfile(string value, string context) =>
        value switch
        {
            "capture_value" => EnemyScoreProfile.CaptureValue,
            "king_advance" => EnemyScoreProfile.KingAdvance,
            "king_defense" => EnemyScoreProfile.KingDefense,
            "king_execution" => EnemyScoreProfile.KingExecution,
            "king_pressure" => EnemyScoreProfile.KingPressure,
            _ => throw Invalid($"{context} uses unsupported score profile '{value}'."),
        };

    private static JsonDocument OpenDocument(ReadOnlyMemory<byte> content, string context)
    {
        try
        {
            return JsonDocument.Parse(content, DocumentOptions);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException(
                $"Generated content JSON is invalid: {context}.",
                exception);
        }
    }

    private static JsonElement RequireObject(JsonElement element, string context)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw Invalid($"{context} must be an object.");
        }

        return element;
    }

    private static JsonElement RequireArray(JsonElement element, string context)
    {
        if (element.ValueKind != JsonValueKind.Array)
        {
            throw Invalid($"{context} must be an array.");
        }

        return element;
    }

    private static JsonElement RequiredObject(
        JsonElement parent,
        string propertyName,
        string context) => RequireObject(RequiredProperty(parent, propertyName, context), $"{context}.{propertyName}");

    private static JsonElement RequiredArray(
        JsonElement parent,
        string propertyName,
        string context) => RequireArray(RequiredProperty(parent, propertyName, context), $"{context}.{propertyName}");

    private static JsonElement RequiredProperty(
        JsonElement parent,
        string propertyName,
        string context)
    {
        if (!parent.TryGetProperty(propertyName, out var value))
        {
            throw Invalid($"{context} is missing required property '{propertyName}'.");
        }

        return value;
    }

    private static string RequiredString(
        JsonElement parent,
        string propertyName,
        string context) => RequireString(
        RequiredProperty(parent, propertyName, context),
        $"{context}.{propertyName}");

    private static string RequireString(JsonElement element, string context)
    {
        if (element.ValueKind != JsonValueKind.String)
        {
            throw Invalid($"{context} must be a string.");
        }

        var value = element.GetString();
        if (string.IsNullOrWhiteSpace(value))
        {
            throw Invalid($"{context} cannot be blank.");
        }

        return value;
    }

    private static int RequiredInt(
        JsonElement parent,
        string propertyName,
        string context)
    {
        var element = RequiredProperty(parent, propertyName, context);
        if (element.ValueKind != JsonValueKind.Number || !element.TryGetInt32(out var value))
        {
            throw Invalid($"{context}.{propertyName} must be a 32-bit integer.");
        }

        return value;
    }

    private static void EnsureUniqueProperties(JsonElement element, string context)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in element.EnumerateObject())
        {
            if (!seen.Add(property.Name))
            {
                throw Invalid($"{context} contains duplicate property '{property.Name}'.");
            }
        }
    }

    private static void EnsureExactProperties(
        JsonElement element,
        string context,
        params string[] allowedProperties)
    {
        EnsureUniqueProperties(element, context);
        var allowed = allowedProperties.ToHashSet(StringComparer.Ordinal);
        foreach (var property in element.EnumerateObject())
        {
            if (!allowed.Contains(property.Name))
            {
                throw Invalid($"{context} contains unsupported property '{property.Name}'.");
            }
        }

        foreach (var propertyName in allowedProperties)
        {
            if (!element.TryGetProperty(propertyName, out _))
            {
                throw Invalid($"{context} is missing required property '{propertyName}'.");
            }
        }
    }

    private static void ValidatePrefixedContentId(
        string value,
        string prefix,
        string context)
    {
        ValidateStableId(value, context);
        if (!value.StartsWith(prefix, StringComparison.Ordinal))
        {
            throw Invalid($"{context}.id must start with '{prefix}'.");
        }
    }

    private static void ValidateEnemyContentId(string value, string context)
    {
        ValidateStableId(value, context);
        if (!value.StartsWith("enemy_", StringComparison.Ordinal) &&
            !value.StartsWith("boss_", StringComparison.Ordinal))
        {
            throw Invalid($"{context}.id must start with 'enemy_' or 'boss_'.");
        }
    }

    private static void ValidateStableId(string value, string context)
    {
        if (value.Any(character =>
                !char.IsAsciiLetterOrDigit(character) &&
                character is not '.' and not '_' and not '-'))
        {
            throw Invalid($"{context} contains an invalid stable ID '{value}'.");
        }
    }

    private static T ConvertDomain<T>(string context, Func<T> factory)
    {
        try
        {
            return factory();
        }
        catch (ArgumentException exception)
        {
            throw new InvalidDataException($"{context} cannot be converted to a Domain definition.", exception);
        }
    }

    private static InvalidDataException Invalid(string message) => new(message);

    private sealed record ParsedCards(
        IReadOnlyList<CardContentDefinition> StarterCards,
        IReadOnlyDictionary<string, CardRarity> CardRarities);

    private sealed record ParsedSystem(
        CoreDuelSystemPolicy Policy,
        CoreDuelBattleSetupDefinition BattleSetup,
        IReadOnlySet<string> FacilityTypeIds);

    private sealed record ParsedCounterattack(
        CounterattackBoundaryPolicy Policy,
        int StartGaugeUnits);
}
