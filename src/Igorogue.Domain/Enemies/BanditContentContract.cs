using Igorogue.Domain.Content;

namespace Igorogue.Domain.Enemies;

internal static class BanditContentContract
{
    private static readonly EnemyIntentKind[] AcceptedMandatoryOverrides =
    [
        EnemyIntentKind.CaptureBlackKing,
        EnemyIntentKind.DefendWhiteKing,
    ];

    private static readonly EnemyIntentKind[] AcceptedPriority =
    [
        EnemyIntentKind.CaptureNonKing,
        EnemyIntentKind.PressureBlackKing,
        EnemyIntentKind.AdvanceTowardBlackKing,
    ];

    private static readonly (EnemyIntentKind Intent, EnemyScoreProfile Profile)[]
        AcceptedScoreProfiles =
    [
        (EnemyIntentKind.CaptureBlackKing, EnemyScoreProfile.KingExecution),
        (EnemyIntentKind.DefendWhiteKing, EnemyScoreProfile.KingDefense),
        (EnemyIntentKind.CaptureNonKing, EnemyScoreProfile.CaptureValue),
        (EnemyIntentKind.PressureBlackKing, EnemyScoreProfile.KingPressure),
        (EnemyIntentKind.AdvanceTowardBlackKing, EnemyScoreProfile.KingAdvance),
    ];

    internal static void Validate(EnemyContentDefinition enemy)
    {
        ArgumentNullException.ThrowIfNull(enemy);

        if (enemy.ActionBudget.NormalActions != 1 ||
            enemy.ActionBudget.CounterattackBonusActions != 1 ||
            enemy.ActionBudget.MaxActionsPerEnemyTurn != 2)
        {
            throw Invalid(
                "action_budget must be normal=1, counterattack_bonus=1, max=2.");
        }

        ValidateSequence(
            enemy.MandatoryOverrides,
            AcceptedMandatoryOverrides,
            "mandatory_overrides");
        ValidateSequence(enemy.PlanPriority, AcceptedPriority, "plan_priority");
        ValidateSequence(
            enemy.CounterattackPriority,
            AcceptedPriority,
            "counterattack_priority");

        foreach (var (intent, profile) in AcceptedScoreProfiles)
        {
            var definition = enemy.Intents.SingleOrDefault(candidate => candidate.Kind == intent);
            if (definition?.ScoreProfile != profile)
            {
                throw Invalid(
                    $"intent {intent} must use score profile {profile}.");
            }
        }

        if (enemy.TieBreak != EnemyTieBreak.CanonicalYThenX)
        {
            throw Invalid("tie_break must be canonical_y_then_x.");
        }
    }

    private static void ValidateSequence(
        IReadOnlyList<EnemyIntentKind> actual,
        IReadOnlyList<EnemyIntentKind> expected,
        string fieldName)
    {
        if (!actual.SequenceEqual(expected))
        {
            throw Invalid($"{fieldName} does not match the accepted Bandit order.");
        }
    }

    private static ArgumentException Invalid(string detail) => new(
        $"Bandit content contract mismatch: {detail}",
        "enemy");
}
