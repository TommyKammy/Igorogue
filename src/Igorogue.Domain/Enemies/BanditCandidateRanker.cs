using Igorogue.Domain.Content;

namespace Igorogue.Domain.Enemies;

public static class BanditCandidateRanker
{
    public static IReadOnlyList<BanditPlacementCandidate> Rank(
        IEnumerable<BanditPlacementCandidate> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        var materialized = candidates.ToArray();
        foreach (var candidate in materialized)
        {
            ArgumentNullException.ThrowIfNull(candidate);
        }

        if (materialized.Length == 0)
        {
            return Array.Empty<BanditPlacementCandidate>();
        }

        var intentKind = materialized[0].IntentKind;
        if (materialized.Any(candidate => candidate.IntentKind != intentKind))
        {
            throw new ArgumentException(
                "Bandit candidates must be ranked within one intent.",
                nameof(candidates));
        }

        Array.Sort(materialized, (left, right) => Compare(intentKind, left, right));
        return Array.AsReadOnly(materialized);
    }

    private static int Compare(
        EnemyIntentKind intentKind,
        BanditPlacementCandidate left,
        BanditPlacementCandidate right)
    {
        var result = intentKind switch
        {
            EnemyIntentKind.CaptureBlackKing => CompareValues(
                Descending(left.CapturedStoneCount, right.CapturedStoneCount),
                Descending(
                    left.PlacedGroupEffectiveLiberties,
                    right.PlacedGroupEffectiveLiberties)),
            EnemyIntentKind.DefendWhiteKing => CompareValues(
                Descending(
                    Required(left.WhiteKingEffectiveLibertiesAfter),
                    Required(right.WhiteKingEffectiveLibertiesAfter)),
                Descending(left.CapturedStoneCount, right.CapturedStoneCount),
                Descending(
                    left.ConnectedOtherWhiteGroupCount,
                    right.ConnectedOtherWhiteGroupCount)),
            EnemyIntentKind.CaptureNonKing => CompareValues(
                Descending(left.CapturedStoneCount, right.CapturedStoneCount),
                left.TargetGroupToBlackKingDistance.CompareTo(
                    right.TargetGroupToBlackKingDistance),
                Descending(
                    left.PlacedGroupEffectiveLiberties,
                    right.PlacedGroupEffectiveLiberties)),
            EnemyIntentKind.PressureBlackKing => CompareValues(
                Required(left.BlackKingEffectiveLibertiesAfter).CompareTo(
                    Required(right.BlackKingEffectiveLibertiesAfter)),
                Descending(
                    left.PlacedGroupEffectiveLiberties,
                    right.PlacedGroupEffectiveLiberties),
                Descending(
                    left.ConnectedOtherWhiteGroupCount,
                    right.ConnectedOtherWhiteGroupCount)),
            EnemyIntentKind.AdvanceTowardBlackKing => CompareValues(
                left.DistanceToBlackKingAdvanceTarget.CompareTo(
                    right.DistanceToBlackKingAdvanceTarget),
                Descending(
                    left.PlacedGroupEffectiveLiberties,
                    right.PlacedGroupEffectiveLiberties),
                left.DistanceToCenter.CompareTo(right.DistanceToCenter)),
            _ => throw new ArgumentOutOfRangeException(
                nameof(intentKind),
                intentKind,
                "Unknown enemy intent."),
        };
        return result != 0 ? result : left.Point.CompareTo(right.Point);
    }

    private static int Required(int? value) =>
        value ?? throw new InvalidOperationException(
            "Bandit candidate is missing a metric required by its intent.");

    private static int Descending(int left, int right) => right.CompareTo(left);

    private static int CompareValues(params int[] comparisons) =>
        comparisons.FirstOrDefault(comparison => comparison != 0);
}
