using System.Collections.ObjectModel;

namespace Igorogue.Domain.Content;

public enum EnemyPlacementMode : byte
{
    WhiteContact = 1,
    WhiteFacilityInvasion = 2,
    WhiteFrontline = 3,
    WhiteInvasion = 4,
    WhiteTerminal = 5,
}

public enum EnemyIntentKind : byte
{
    AdvanceTowardBlackKing = 1,
    CaptureBlackKing = 2,
    CaptureNonKing = 3,
    DefendWhiteKing = 4,
    PressureBlackKing = 5,
}

public enum EnemyScoreProfile : byte
{
    CaptureValue = 1,
    KingAdvance = 2,
    KingDefense = 3,
    KingExecution = 4,
    KingPressure = 5,
}

public enum EnemyTieBreak : byte
{
    CanonicalYThenX = 1,
}

public sealed class EnemyActionBudget
{
    public EnemyActionBudget(
        int normalActions,
        int counterattackBonusActions,
        int maxActionsPerEnemyTurn)
    {
        if (normalActions <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(normalActions),
                normalActions,
                "Normal action budget must be positive.");
        }

        if (counterattackBonusActions < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(counterattackBonusActions),
                counterattackBonusActions,
                "Counterattack bonus action budget cannot be negative.");
        }

        if (maxActionsPerEnemyTurn != normalActions + counterattackBonusActions)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxActionsPerEnemyTurn),
                maxActionsPerEnemyTurn,
                "Maximum actions must equal normal plus counterattack bonus actions.");
        }

        NormalActions = normalActions;
        CounterattackBonusActions = counterattackBonusActions;
        MaxActionsPerEnemyTurn = maxActionsPerEnemyTurn;
    }

    public int NormalActions { get; }

    public int CounterattackBonusActions { get; }

    public int MaxActionsPerEnemyTurn { get; }
}

public sealed class BanditParameters
{
    public BanditParameters(int defenseThreshold, int opportunisticCaptureMinStones)
    {
        if (defenseThreshold <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(defenseThreshold),
                defenseThreshold,
                "Defense threshold must be positive.");
        }

        if (opportunisticCaptureMinStones <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(opportunisticCaptureMinStones),
                opportunisticCaptureMinStones,
                "Opportunistic capture minimum must be positive.");
        }

        DefenseThreshold = defenseThreshold;
        OpportunisticCaptureMinStones = opportunisticCaptureMinStones;
    }

    public int DefenseThreshold { get; }

    public int OpportunisticCaptureMinStones { get; }
}

public sealed class EnemyIntentDefinition
{
    private readonly ReadOnlyCollection<EnemyPlacementMode> placementModeView;
    private readonly ReadOnlyCollection<EnemyIntentKind> fallbackView;

    private EnemyIntentDefinition(
        EnemyIntentKind kind,
        EnemyIntentKind candidateRule,
        EnemyPlacementMode[] placementModes,
        EnemyScoreProfile scoreProfile,
        EnemyIntentKind[] fallback)
    {
        Kind = kind;
        CandidateRule = candidateRule;
        placementModeView = Array.AsReadOnly(placementModes);
        ScoreProfile = scoreProfile;
        fallbackView = Array.AsReadOnly(fallback);
    }

    public EnemyIntentKind Kind { get; }

    public EnemyIntentKind CandidateRule { get; }

    public IReadOnlyList<EnemyPlacementMode> PlacementModes => placementModeView;

    public EnemyScoreProfile ScoreProfile { get; }

    public IReadOnlyList<EnemyIntentKind> Fallback => fallbackView;

    public static EnemyIntentDefinition Create(
        EnemyIntentKind kind,
        EnemyIntentKind candidateRule,
        IEnumerable<EnemyPlacementMode> placementModes,
        EnemyScoreProfile scoreProfile,
        IEnumerable<EnemyIntentKind> fallback)
    {
        ValidateIntent(kind, nameof(kind));
        ValidateIntent(candidateRule, nameof(candidateRule));
        if (candidateRule != kind)
        {
            throw new ArgumentException(
                "Bandit candidate rule must match its intent ID.",
                nameof(candidateRule));
        }

        if (!Enum.IsDefined(scoreProfile))
        {
            throw new ArgumentOutOfRangeException(
                nameof(scoreProfile),
                scoreProfile,
                "Unknown enemy score profile.");
        }

        ArgumentNullException.ThrowIfNull(placementModes);
        var canonicalModes = placementModes.ToArray();
        if (canonicalModes.Length == 0)
        {
            throw new ArgumentException(
                "Enemy intent must permit at least one placement mode.",
                nameof(placementModes));
        }

        foreach (var mode in canonicalModes)
        {
            if (!Enum.IsDefined(mode))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(placementModes),
                    mode,
                    "Unknown enemy placement mode.");
            }
        }

        if (canonicalModes.Distinct().Count() != canonicalModes.Length)
        {
            throw new ArgumentException(
                "Enemy intent placement modes must be unique.",
                nameof(placementModes));
        }

        Array.Sort(canonicalModes);

        ArgumentNullException.ThrowIfNull(fallback);
        var orderedFallback = fallback.ToArray();
        ValidateOrderedIntentReferences(orderedFallback, nameof(fallback));
        if (orderedFallback.Contains(kind))
        {
            throw new ArgumentException("Enemy intent cannot fall back to itself.", nameof(fallback));
        }

        return new EnemyIntentDefinition(
            kind,
            candidateRule,
            canonicalModes,
            scoreProfile,
            orderedFallback);
    }

    internal static void ValidateOrderedIntentReferences(
        IReadOnlyList<EnemyIntentKind> values,
        string parameterName)
    {
        foreach (var value in values)
        {
            ValidateIntent(value, parameterName);
        }

        if (values.Distinct().Count() != values.Count)
        {
            throw new ArgumentException("Ordered enemy intent references must be unique.", parameterName);
        }
    }

    private static void ValidateIntent(EnemyIntentKind value, string parameterName)
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Unknown enemy intent.");
        }
    }
}

public sealed class EnemyContentDefinition
{
    private readonly ReadOnlyCollection<EnemyPlacementMode> placementPermissionView;
    private readonly ReadOnlyCollection<EnemyIntentKind> mandatoryOverrideView;
    private readonly ReadOnlyCollection<EnemyIntentKind> planPriorityView;
    private readonly ReadOnlyCollection<EnemyIntentKind> counterattackPriorityView;
    private readonly ReadOnlyCollection<EnemyIntentDefinition> intentView;

    private EnemyContentDefinition(
        string id,
        string behaviorSpec,
        string behaviorVersion,
        EnemyActionBudget actionBudget,
        EnemyPlacementMode[] placementPermissions,
        BanditParameters parameters,
        EnemyIntentKind[] mandatoryOverrides,
        EnemyIntentKind[] planPriority,
        EnemyIntentKind[] counterattackPriority,
        EnemyIntentDefinition[] intents,
        EnemyTieBreak tieBreak)
    {
        Id = id;
        BehaviorSpec = behaviorSpec;
        BehaviorVersion = behaviorVersion;
        ActionBudget = actionBudget;
        placementPermissionView = Array.AsReadOnly(placementPermissions);
        Parameters = parameters;
        mandatoryOverrideView = Array.AsReadOnly(mandatoryOverrides);
        planPriorityView = Array.AsReadOnly(planPriority);
        counterattackPriorityView = Array.AsReadOnly(counterattackPriority);
        intentView = Array.AsReadOnly(intents);
        TieBreak = tieBreak;
    }

    public string Id { get; }

    public string BehaviorSpec { get; }

    public string BehaviorVersion { get; }

    public EnemyActionBudget ActionBudget { get; }

    public IReadOnlyList<EnemyPlacementMode> PlacementPermissions => placementPermissionView;

    public BanditParameters Parameters { get; }

    public IReadOnlyList<EnemyIntentKind> MandatoryOverrides => mandatoryOverrideView;

    public IReadOnlyList<EnemyIntentKind> PlanPriority => planPriorityView;

    public IReadOnlyList<EnemyIntentKind> CounterattackPriority => counterattackPriorityView;

    public IReadOnlyList<EnemyIntentDefinition> Intents => intentView;

    public EnemyTieBreak TieBreak { get; }

    public static EnemyContentDefinition Create(
        string id,
        string behaviorSpec,
        string behaviorVersion,
        EnemyActionBudget actionBudget,
        IEnumerable<EnemyPlacementMode> placementPermissions,
        BanditParameters parameters,
        IEnumerable<EnemyIntentKind> mandatoryOverrides,
        IEnumerable<EnemyIntentKind> planPriority,
        IEnumerable<EnemyIntentKind> counterattackPriority,
        IEnumerable<EnemyIntentDefinition> intents,
        EnemyTieBreak tieBreak)
    {
        var stableId = StableDomainId.Validate(id, nameof(id));
        if (!stableId.StartsWith("enemy_", StringComparison.Ordinal))
        {
            throw new ArgumentException("Enemy content IDs must start with 'enemy_'.", nameof(id));
        }

        var stableSpec = StableDomainId.Validate(behaviorSpec, nameof(behaviorSpec));
        var stableVersion = StableDomainId.Validate(behaviorVersion, nameof(behaviorVersion));
        ArgumentNullException.ThrowIfNull(actionBudget);
        ArgumentNullException.ThrowIfNull(parameters);
        if (!Enum.IsDefined(tieBreak))
        {
            throw new ArgumentOutOfRangeException(nameof(tieBreak), tieBreak, "Unknown tie break.");
        }

        var canonicalPermissions = CanonicalPermissions(placementPermissions);
        var orderedOverrides = OrderedReferences(mandatoryOverrides, nameof(mandatoryOverrides));
        var orderedPlan = OrderedReferences(planPriority, nameof(planPriority));
        var orderedCounterattack = OrderedReferences(
            counterattackPriority,
            nameof(counterattackPriority));

        ArgumentNullException.ThrowIfNull(intents);
        var canonicalIntents = intents.ToArray();
        foreach (var intent in canonicalIntents)
        {
            ArgumentNullException.ThrowIfNull(intent);
        }

        Array.Sort(canonicalIntents, (left, right) => left.Kind.CompareTo(right.Kind));
        if (canonicalIntents.Select(intent => intent.Kind).Distinct().Count() != canonicalIntents.Length)
        {
            throw new ArgumentException("Enemy intent IDs must be unique.", nameof(intents));
        }

        var defined = canonicalIntents.Select(intent => intent.Kind).ToHashSet();
        ValidateReferencesDefined(orderedOverrides, defined, nameof(mandatoryOverrides));
        ValidateReferencesDefined(orderedPlan, defined, nameof(planPriority));
        ValidateReferencesDefined(orderedCounterattack, defined, nameof(counterattackPriority));
        ValidateDisjoint(
            orderedOverrides,
            orderedPlan,
            nameof(planPriority));
        ValidateDisjoint(
            orderedOverrides,
            orderedCounterattack,
            nameof(counterattackPriority));

        foreach (var intent in canonicalIntents)
        {
            ValidateReferencesDefined(intent.Fallback, defined, nameof(intents));
            if (intent.PlacementModes.Any(mode => !canonicalPermissions.Contains(mode)))
            {
                throw new ArgumentException(
                    $"Intent {intent.Kind} uses a placement mode outside enemy permissions.",
                    nameof(intents));
            }
        }

        ValidateFallbackGraph(canonicalIntents);

        var normallyReachable = orderedOverrides.Concat(orderedPlan).ToHashSet();
        if (!normallyReachable.SetEquals(defined))
        {
            throw new ArgumentException(
                "Mandatory overrides and plan priority must cover every defined intent exactly by reference.",
                nameof(planPriority));
        }

        var counterattackReachable = orderedOverrides.Concat(orderedCounterattack).ToHashSet();
        if (!counterattackReachable.SetEquals(defined))
        {
            throw new ArgumentException(
                "Mandatory overrides and counterattack priority must cover every defined intent exactly by reference.",
                nameof(counterattackPriority));
        }

        return new EnemyContentDefinition(
            stableId,
            stableSpec,
            stableVersion,
            actionBudget,
            canonicalPermissions,
            parameters,
            orderedOverrides,
            orderedPlan,
            orderedCounterattack,
            canonicalIntents,
            tieBreak);
    }

    private static EnemyPlacementMode[] CanonicalPermissions(
        IEnumerable<EnemyPlacementMode> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var result = values.ToArray();
        foreach (var value in result)
        {
            if (!Enum.IsDefined(value))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(values),
                    value,
                    "Unknown enemy placement permission.");
            }
        }

        if (result.Distinct().Count() != result.Length)
        {
            throw new ArgumentException("Enemy placement permissions must be unique.", nameof(values));
        }

        Array.Sort(result);
        return result;
    }

    private static EnemyIntentKind[] OrderedReferences(
        IEnumerable<EnemyIntentKind> values,
        string parameterName)
    {
        ArgumentNullException.ThrowIfNull(values);
        var result = values.ToArray();
        EnemyIntentDefinition.ValidateOrderedIntentReferences(result, parameterName);
        return result;
    }

    private static void ValidateReferencesDefined(
        IEnumerable<EnemyIntentKind> references,
        IReadOnlySet<EnemyIntentKind> defined,
        string parameterName)
    {
        foreach (var reference in references)
        {
            if (!defined.Contains(reference))
            {
                throw new ArgumentException(
                    $"Enemy intent reference {reference} is not defined.",
                    parameterName);
            }
        }
    }

    private static void ValidateDisjoint(
        IReadOnlyCollection<EnemyIntentKind> mandatoryOverrides,
        IReadOnlyCollection<EnemyIntentKind> priority,
        string parameterName)
    {
        var duplicated = mandatoryOverrides.Intersect(priority).ToArray();
        if (duplicated.Length > 0)
        {
            throw new ArgumentException(
                $"Mandatory overrides must remain outside {parameterName}.",
                parameterName);
        }
    }

    private static void ValidateFallbackGraph(
        IReadOnlyList<EnemyIntentDefinition> intents)
    {
        var byKind = intents.ToDictionary(intent => intent.Kind);
        var completed = new HashSet<EnemyIntentKind>();
        var active = new HashSet<EnemyIntentKind>();

        foreach (var intent in intents)
        {
            Visit(intent.Kind);
        }

        void Visit(EnemyIntentKind kind)
        {
            if (completed.Contains(kind))
            {
                return;
            }

            if (!active.Add(kind))
            {
                throw new ArgumentException(
                    $"Enemy fallback graph contains a cycle at {kind}.",
                    nameof(intents));
            }

            foreach (var fallback in byKind[kind].Fallback)
            {
                Visit(fallback);
            }

            active.Remove(kind);
            completed.Add(kind);
        }
    }
}
