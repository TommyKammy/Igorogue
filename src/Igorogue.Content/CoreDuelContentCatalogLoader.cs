using System.Text.Json;

using Igorogue.Domain.Content;

namespace Igorogue.Content;

public sealed class CoreDuelContentCatalogLoader
{
    private const string CardsPath = "content/cards.json";
    private const string EnemiesPath = "content/enemies.json";
    private const string SystemPath = "balance/system.json";
    private const string BanditContentId = "enemy_bandit";

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
        var starterCards = ParseStarterCards(
            snapshot.RequiredContent(CardsPath),
            system.FacilityTypeIds);
        var bandit = ParseBandit(snapshot.RequiredContent(EnemiesPath));

        return ConvertDomain(
            "core duel catalog",
            () => CoreDuelContentCatalog.Create(
                snapshot.ContentHash,
                starterCards,
                bandit,
                system.Policy));
    }

    private static ParsedSystem ParseSystem(ReadOnlyMemory<byte> content)
    {
        using var document = OpenDocument(content, SystemPath);
        var root = RequireObject(document.RootElement, SystemPath);
        EnsureUniqueProperties(root, SystemPath);

        var baseQi = RequiredInt(root, "base_qi", SystemPath);
        var baseDraw = RequiredInt(root, "base_draw", SystemPath);
        var facilityLimits = RequiredObject(
            root,
            "facility_type_limits_per_region",
            SystemPath);
        EnsureUniqueProperties(facilityLimits, $"{SystemPath}.facility_type_limits_per_region");

        var facilityTypeIds = new HashSet<string>(StringComparer.Ordinal);
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

            if (!string.Equals(property.Name, "default", StringComparison.Ordinal))
            {
                facilityTypeIds.Add(property.Name);
            }
        }

        return new ParsedSystem(
            ConvertDomain(
                SystemPath,
                () => CoreDuelSystemPolicy.Create(baseQi, baseDraw)),
            facilityTypeIds);
    }

    private static IReadOnlyList<CardContentDefinition> ParseStarterCards(
        ReadOnlyMemory<byte> content,
        IReadOnlySet<string> facilityTypeIds)
    {
        using var document = OpenDocument(content, CardsPath);
        var root = RequireArray(document.RootElement, CardsPath);
        var seenIds = new HashSet<string>(StringComparer.Ordinal);
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
            if (rarity == CardRarity.Starter)
            {
                starterCards.Add(ParseStarterCard(card, id, context, facilityTypeIds));
            }

            index++;
        }

        return starterCards;
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
        var target = card.TryGetProperty("target", out var targetElement)
            ? ParseCardTarget(RequireString(targetElement, $"{context}.target"), context)
            : CardTargetKind.None;
        var placementTags = card.TryGetProperty("placement_tags", out var placementElement)
            ? ParseCardPlacementTags(placementElement, $"{context}.placement_tags")
            : [];
        var effects = ParseCardOperations(
            RequiredArray(card, "effects", context),
            $"{context}.effects",
            facilityTypeIds);
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

    private sealed record ParsedSystem(
        CoreDuelSystemPolicy Policy,
        IReadOnlySet<string> FacilityTypeIds);
}
