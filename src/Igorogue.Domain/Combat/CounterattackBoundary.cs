using System.Collections.ObjectModel;
using System.Globalization;

namespace Igorogue.Domain.Combat;

public sealed class CounterattackBoundaryPolicy
{
    public CounterattackBoundaryPolicy(
        int thresholdUnits,
        int enemyTurnEndNaturalGainUnits,
        int sacrificeStonesPerBatch,
        int sacrificeUnitsPerBatch)
    {
        ValidatePositive(thresholdUnits, nameof(thresholdUnits));
        ValidatePositive(
            enemyTurnEndNaturalGainUnits,
            nameof(enemyTurnEndNaturalGainUnits));
        ValidatePositive(sacrificeStonesPerBatch, nameof(sacrificeStonesPerBatch));
        ValidatePositive(sacrificeUnitsPerBatch, nameof(sacrificeUnitsPerBatch));

        ThresholdUnits = thresholdUnits;
        EnemyTurnEndNaturalGainUnits = enemyTurnEndNaturalGainUnits;
        SacrificeStonesPerBatch = sacrificeStonesPerBatch;
        SacrificeUnitsPerBatch = sacrificeUnitsPerBatch;
    }

    public int ThresholdUnits { get; }

    public int EnemyTurnEndNaturalGainUnits { get; }

    public int SacrificeStonesPerBatch { get; }

    public int SacrificeUnitsPerBatch { get; }

    public string ToCanonicalText() => string.Join(
        '\n',
        "counterattack-boundary-policy-v1",
        $"threshold_units={ThresholdUnits.ToString(CultureInfo.InvariantCulture)}",
        $"enemy_turn_end_natural_gain_units={EnemyTurnEndNaturalGainUnits.ToString(CultureInfo.InvariantCulture)}",
        $"sacrifice_stones_per_batch={SacrificeStonesPerBatch.ToString(CultureInfo.InvariantCulture)}",
        $"sacrifice_units_per_batch={SacrificeUnitsPerBatch.ToString(CultureInfo.InvariantCulture)}");

    private static void ValidatePositive(int value, string parameterName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "Counterattack boundary policy values must be positive.");
        }
    }
}

public sealed class CounterattackBoundaryState
{
    public const string EncodingVersion = "counterattack-boundary-state-v1";

    internal CounterattackBoundaryState(
        int gaugeUnits,
        bool pending,
        int sacrificeStoneRemainder,
        object? pendingToken)
    {
        if (gaugeUnits < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(gaugeUnits),
                gaugeUnits,
                "Counterattack gauge units cannot be negative.");
        }

        if (sacrificeStoneRemainder < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sacrificeStoneRemainder),
                sacrificeStoneRemainder,
                "Sacrifice stone remainder cannot be negative.");
        }

        GaugeUnits = gaugeUnits;
        Pending = pending;
        SacrificeStoneRemainder = sacrificeStoneRemainder;
        if (pending != (pendingToken is not null))
        {
            throw new ArgumentException(
                "Pending counterattack state and its exact pending token must agree.",
                nameof(pendingToken));
        }

        PendingToken = pendingToken;
    }

    public int GaugeUnits { get; }

    public bool Pending { get; }

    public int SacrificeStoneRemainder { get; }

    internal object? PendingToken { get; }

    public static CounterattackBoundaryState Create(
        int gaugeUnits,
        bool pending,
        int sacrificeStoneRemainder,
        CounterattackBoundaryPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        var state = new CounterattackBoundaryState(
            gaugeUnits,
            pending,
            sacrificeStoneRemainder,
            pending ? new object() : null);
        CounterattackBoundaryResolver.ValidateStateAgainstPolicy(state, policy);
        return state;
    }

    public string ToCanonicalText() => string.Join(
        '\n',
        EncodingVersion,
        $"gauge_units={GaugeUnits.ToString(CultureInfo.InvariantCulture)}",
        $"pending={(Pending ? "1" : "0")}",
        $"sacrifice_stone_remainder={SacrificeStoneRemainder.ToString(CultureInfo.InvariantCulture)}");
}

public enum CounterattackAdvanceReason : byte
{
    SacrificeBatch = 1,
    EnemyTurnEnd = 2,
}

public sealed class SacrificeRemainderChangedFact : IBattleFact
{
    internal SacrificeRemainderChangedFact(
        int capturedNonKingBlackStoneCount,
        int remainderBefore,
        int remainderAfter)
    {
        CapturedNonKingBlackStoneCount = capturedNonKingBlackStoneCount;
        RemainderBefore = remainderBefore;
        RemainderAfter = remainderAfter;
    }

    public int CapturedNonKingBlackStoneCount { get; }

    public int RemainderBefore { get; }

    public int RemainderAfter { get; }
}

public sealed class SacrificeBatchAdvancedFact : IBattleFact
{
    internal SacrificeBatchAdvancedFact(
        int capturedNonKingBlackStoneCount,
        int completedBatchCount,
        int remainderBefore,
        int remainderAfter,
        int deltaUnits)
    {
        CapturedNonKingBlackStoneCount = capturedNonKingBlackStoneCount;
        CompletedBatchCount = completedBatchCount;
        RemainderBefore = remainderBefore;
        RemainderAfter = remainderAfter;
        DeltaUnits = deltaUnits;
    }

    public int CapturedNonKingBlackStoneCount { get; }

    public int CompletedBatchCount { get; }

    public int RemainderBefore { get; }

    public int RemainderAfter { get; }

    public int DeltaUnits { get; }
}

public sealed class CounterattackAdvancedFact : IBattleFact
{
    internal CounterattackAdvancedFact(
        CounterattackAdvanceReason reason,
        int gaugeUnitsBefore,
        int unitsAfterGainBeforePrime,
        int gaugeUnitsAfter,
        int deltaUnits,
        bool pendingBefore,
        bool pendingAfter)
    {
        Reason = reason;
        GaugeUnitsBefore = gaugeUnitsBefore;
        UnitsAfterGainBeforePrime = unitsAfterGainBeforePrime;
        GaugeUnitsAfter = gaugeUnitsAfter;
        DeltaUnits = deltaUnits;
        PendingBefore = pendingBefore;
        PendingAfter = pendingAfter;
    }

    public CounterattackAdvanceReason Reason { get; }

    public string ReasonId => Reason switch
    {
        CounterattackAdvanceReason.SacrificeBatch => "sacrifice_batch",
        CounterattackAdvanceReason.EnemyTurnEnd => "enemy_turn_end",
        _ => throw new InvalidOperationException("Unknown counterattack advance reason."),
    };

    public int GaugeUnitsBefore { get; }

    public int UnitsAfterGainBeforePrime { get; }

    public int GaugeUnitsAfter { get; }

    public int DeltaUnits { get; }

    public bool PendingBefore { get; }

    public bool PendingAfter { get; }
}

public enum CounterattackPrimeReason : byte
{
    ThresholdCrossed = 1,
    OverflowAfterPendingConsumption = 2,
}

public sealed class CounterattackPendingPrimedFact : IBattleFact
{
    internal CounterattackPendingPrimedFact(
        CounterattackPrimeReason reason,
        int residualGaugeUnits)
    {
        Reason = reason;
        ResidualGaugeUnits = residualGaugeUnits;
    }

    public CounterattackPrimeReason Reason { get; }

    public int ResidualGaugeUnits { get; }
}

public sealed class CounterattackPendingConsumedFact : IBattleFact
{
    internal CounterattackPendingConsumedFact(bool reprimed)
    {
        Reprimed = reprimed;
    }

    public bool Reprimed { get; }
}

public sealed class CounterattackPendingAtStartSnapshot
{
    internal CounterattackPendingAtStartSnapshot(CounterattackBoundaryState sourceState)
    {
        SourceState = sourceState;
        PendingAtStart = sourceState.Pending;
        PendingToken = sourceState.PendingToken;
    }

    internal CounterattackBoundaryState SourceState { get; }

    internal object? PendingToken { get; }

    public bool PendingAtStart { get; }
}

public sealed class CounterattackBoundaryTransition
{
    private readonly ReadOnlyCollection<IBattleFact> orderedFactView;

    internal CounterattackBoundaryTransition(
        CounterattackBoundaryState stateAfterTransition,
        IBattleFact[] orderedFacts)
    {
        StateAfterTransition = stateAfterTransition;
        orderedFactView = Array.AsReadOnly((IBattleFact[])orderedFacts.Clone());
    }

    public CounterattackBoundaryState StateAfterTransition { get; }

    public IReadOnlyList<IBattleFact> OrderedFacts => orderedFactView;
}

public static class CounterattackBoundaryResolver
{
    public static CounterattackBoundaryTransition AdvanceSacrifice(
        CounterattackBoundaryState sourceState,
        CaptureBatch captureBatch,
        CounterattackBoundaryPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(sourceState);
        ArgumentNullException.ThrowIfNull(captureBatch);
        ArgumentNullException.ThrowIfNull(policy);
        ValidateStateAgainstPolicy(sourceState, policy);

        if (captureBatch.ContainsKing || captureBatch.NonKingBlackStoneCount == 0)
        {
            return new CounterattackBoundaryTransition(sourceState, []);
        }

        var totalStones = checked(
            sourceState.SacrificeStoneRemainder + captureBatch.NonKingBlackStoneCount);
        var completedBatches = totalStones / policy.SacrificeStonesPerBatch;
        var remainderAfter = totalStones % policy.SacrificeStonesPerBatch;
        var deltaUnits = checked(completedBatches * policy.SacrificeUnitsPerBatch);
        var stateWithRemainder = new CounterattackBoundaryState(
            sourceState.GaugeUnits,
            sourceState.Pending,
            remainderAfter,
            sourceState.PendingToken);
        var remainderFact = new SacrificeRemainderChangedFact(
            captureBatch.NonKingBlackStoneCount,
            sourceState.SacrificeStoneRemainder,
            remainderAfter);
        if (deltaUnits == 0)
        {
            return new CounterattackBoundaryTransition(stateWithRemainder, [remainderFact]);
        }

        var sacrificeFact = new SacrificeBatchAdvancedFact(
            captureBatch.NonKingBlackStoneCount,
            completedBatches,
            sourceState.SacrificeStoneRemainder,
            remainderAfter,
            deltaUnits);
        var advance = AdvanceGauge(
            stateWithRemainder,
            deltaUnits,
            CounterattackAdvanceReason.SacrificeBatch,
            policy);
        return new CounterattackBoundaryTransition(
            advance.StateAfterTransition,
            [remainderFact, sacrificeFact, .. advance.OrderedFacts]);
    }

    public static CounterattackBoundaryTransition AdvanceEnemyTurnEnd(
        CounterattackBoundaryState sourceState,
        CounterattackBoundaryPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(sourceState);
        ArgumentNullException.ThrowIfNull(policy);
        ValidateStateAgainstPolicy(sourceState, policy);
        return AdvanceGauge(
            sourceState,
            policy.EnemyTurnEndNaturalGainUnits,
            CounterattackAdvanceReason.EnemyTurnEnd,
            policy);
    }

    public static CounterattackPendingAtStartSnapshot SnapshotPendingAtEnemyTurnStart(
        CounterattackBoundaryState sourceState,
        CounterattackBoundaryPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(sourceState);
        ArgumentNullException.ThrowIfNull(policy);
        ValidateStateAgainstPolicy(sourceState, policy);
        return new CounterattackPendingAtStartSnapshot(sourceState);
    }

    public static CounterattackBoundaryTransition ConsumeAndReprimeOnce(
        CounterattackBoundaryState sourceState,
        CounterattackPendingAtStartSnapshot pendingAtStart,
        CounterattackBoundaryPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(sourceState);
        ArgumentNullException.ThrowIfNull(pendingAtStart);
        ArgumentNullException.ThrowIfNull(policy);
        ValidateStateAgainstPolicy(sourceState, policy);

        if (!pendingAtStart.PendingAtStart)
        {
            return new CounterattackBoundaryTransition(sourceState, []);
        }

        if (!sourceState.Pending)
        {
            throw new InvalidOperationException(
                "The pending counterattack fixed at enemy-turn start was already consumed.");
        }

        if (!ReferenceEquals(sourceState.PendingToken, pendingAtStart.PendingToken))
        {
            throw new InvalidOperationException(
                "The pending-at-start snapshot belongs to a different or already-consumed pending generation.");
        }

        var gaugeUnits = sourceState.GaugeUnits;
        var reprimed = gaugeUnits >= policy.ThresholdUnits;
        if (reprimed)
        {
            gaugeUnits -= policy.ThresholdUnits;
        }

        var stateAfter = new CounterattackBoundaryState(
            gaugeUnits,
            reprimed,
            sourceState.SacrificeStoneRemainder,
            reprimed ? new object() : null);
        var facts = new List<IBattleFact>
        {
            new CounterattackPendingConsumedFact(reprimed),
        };
        if (reprimed)
        {
            facts.Add(new CounterattackPendingPrimedFact(
                CounterattackPrimeReason.OverflowAfterPendingConsumption,
                gaugeUnits));
        }

        return new CounterattackBoundaryTransition(stateAfter, facts.ToArray());
    }

    private static CounterattackBoundaryTransition AdvanceGauge(
        CounterattackBoundaryState sourceState,
        int deltaUnits,
        CounterattackAdvanceReason reason,
        CounterattackBoundaryPolicy policy)
    {
        var unitsAfterGain = checked(sourceState.GaugeUnits + deltaUnits);
        var gaugeUnitsAfter = unitsAfterGain;
        var pendingAfter = sourceState.Pending;
        var pendingTokenAfter = sourceState.PendingToken;
        var primed = false;
        if (!pendingAfter && gaugeUnitsAfter >= policy.ThresholdUnits)
        {
            gaugeUnitsAfter -= policy.ThresholdUnits;
            pendingAfter = true;
            pendingTokenAfter = new object();
            primed = true;
        }

        var stateAfter = new CounterattackBoundaryState(
            gaugeUnitsAfter,
            pendingAfter,
            sourceState.SacrificeStoneRemainder,
            pendingTokenAfter);
        var facts = new List<IBattleFact>
        {
            new CounterattackAdvancedFact(
                reason,
                sourceState.GaugeUnits,
                unitsAfterGain,
                gaugeUnitsAfter,
                deltaUnits,
                sourceState.Pending,
                pendingAfter),
        };
        if (primed)
        {
            facts.Add(new CounterattackPendingPrimedFact(
                CounterattackPrimeReason.ThresholdCrossed,
                gaugeUnitsAfter));
        }

        return new CounterattackBoundaryTransition(stateAfter, facts.ToArray());
    }

    internal static void ValidateStateAgainstPolicy(
        CounterattackBoundaryState state,
        CounterattackBoundaryPolicy policy)
    {
        if (state.SacrificeStoneRemainder >= policy.SacrificeStonesPerBatch)
        {
            throw new ArgumentException(
                "Sacrifice stone remainder must be below the injected stones-per-batch policy.",
                nameof(state));
        }


        if (!state.Pending && state.GaugeUnits >= policy.ThresholdUnits)
        {
            throw new ArgumentException(
                "A non-pending counterattack state must remain below the injected threshold.",
                nameof(state));
        }
    }
}
