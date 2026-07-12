using System.Globalization;

using Igorogue.Domain.Board;
using Igorogue.Domain.Combat;
using Igorogue.Domain.Determinism;
using Igorogue.Domain.Facilities;

namespace Igorogue.Application.Battle;

public sealed class BattleAuthoritativeInitialSnapshot
{
    public const string EncodingVersion = "battle-authoritative-initial-snapshot-v1";

    private BattleAuthoritativeInitialSnapshot(
        StoneRuntimeState stoneRuntimeState,
        TemporaryLibertyState temporaryLibertyState,
        ContinuousLibertySnapshot continuousLibertySnapshot,
        BattleRepetitionHistory repetitionHistory,
        FacilityState facilityState,
        ClosedWindowResourceState closedWindowResources,
        CaptureBenefitTriggerPlan captureBenefitTriggerPlan,
        CounterattackBoundaryState counterattackState,
        CounterattackBoundaryPolicy counterattackPolicy,
        BattleRuntimePolicy runtimePolicy,
        int playerTurnIndex)
    {
        StoneRuntimeState = stoneRuntimeState;
        TemporaryLibertyState = temporaryLibertyState;
        ContinuousLibertySnapshot = continuousLibertySnapshot;
        RepetitionHistory = repetitionHistory;
        FacilityState = facilityState;
        ClosedWindowResources = closedWindowResources;
        CaptureBenefitTriggerPlan = captureBenefitTriggerPlan;
        CounterattackState = counterattackState;
        CounterattackPolicy = counterattackPolicy;
        RuntimePolicy = runtimePolicy;
        PlayerTurnIndex = playerTurnIndex;
        CanonicalText = CreateCanonicalText();
        Checksum = DeterministicChecksum.Sha256Hex(CanonicalText);
    }

    public BoardState Board => StoneRuntimeState.SourceBoard;

    public StoneRuntimeState StoneRuntimeState { get; }

    public TemporaryLibertyState TemporaryLibertyState { get; }

    public ContinuousLibertySnapshot ContinuousLibertySnapshot { get; }

    public BattleRepetitionHistory RepetitionHistory { get; }

    public FacilityState FacilityState { get; }

    public ClosedWindowResourceState ClosedWindowResources { get; }

    public CaptureBenefitTriggerPlan CaptureBenefitTriggerPlan { get; }

    public CounterattackBoundaryState CounterattackState { get; }

    public CounterattackBoundaryPolicy CounterattackPolicy { get; }

    public BattleRuntimePolicy RuntimePolicy { get; }

    public int PlayerTurnIndex { get; }

    public string CanonicalText { get; }

    public string Checksum { get; }

    public static BattleAuthoritativeInitialSnapshot Create(
        StoneRuntimeState stoneRuntimeState,
        TemporaryLibertyState temporaryLibertyState,
        ContinuousLibertySnapshot continuousLibertySnapshot,
        BattleRepetitionHistory repetitionHistory,
        FacilityState facilityState,
        ClosedWindowResourceState closedWindowResources,
        CaptureBenefitTriggerPlan captureBenefitTriggerPlan,
        CounterattackBoundaryState counterattackState,
        CounterattackBoundaryPolicy counterattackPolicy,
        BattleRuntimePolicy runtimePolicy,
        int playerTurnIndex)
    {
        ArgumentNullException.ThrowIfNull(stoneRuntimeState);
        ArgumentNullException.ThrowIfNull(temporaryLibertyState);
        ArgumentNullException.ThrowIfNull(continuousLibertySnapshot);
        ArgumentNullException.ThrowIfNull(repetitionHistory);
        ArgumentNullException.ThrowIfNull(facilityState);
        ArgumentNullException.ThrowIfNull(closedWindowResources);
        ArgumentNullException.ThrowIfNull(captureBenefitTriggerPlan);
        ArgumentNullException.ThrowIfNull(counterattackState);
        ArgumentNullException.ThrowIfNull(counterattackPolicy);
        ArgumentNullException.ThrowIfNull(runtimePolicy);

        if (!ReferenceEquals(
                temporaryLibertyState.SourceStones,
                stoneRuntimeState) ||
            !ReferenceEquals(
                continuousLibertySnapshot.SourceStones,
                stoneRuntimeState))
        {
            throw new ArgumentException(
                "Temporary and continuous liberties must belong to the exact initial stone runtime.",
                nameof(stoneRuntimeState));
        }

        if (!ReferenceEquals(facilityState.SourceBoard, stoneRuntimeState.SourceBoard))
        {
            throw new ArgumentException(
                "Initial facilities must belong to the exact runtime board.",
                nameof(facilityState));
        }

        if (!repetitionHistory.Current.Equals(
                StoneTopologyKey.FromBoard(stoneRuntimeState.SourceBoard)))
        {
            throw new ArgumentException(
                "Initial repetition history must end at the runtime board topology.",
                nameof(repetitionHistory));
        }

        if (playerTurnIndex <= 0 || playerTurnIndex > runtimePolicy.PlayerTurnLimit)
        {
            throw new ArgumentOutOfRangeException(
                nameof(playerTurnIndex),
                playerTurnIndex,
                "Initial player-turn index must be within the configured limit.");
        }

        if (temporaryLibertyState.Effects.Any(effect =>
                effect.ExpiresAfterEnemyTurnIndex < playerTurnIndex))
        {
            throw new ArgumentException(
                "Initial temporary liberty state cannot contain an overdue effect.",
                nameof(temporaryLibertyState));
        }

        if (temporaryLibertyState.ExpirySweepStartedForEnemyTurnIndex is int sweepMarker &&
            sweepMarker >= playerTurnIndex)
        {
            throw new ArgumentException(
                "Initial temporary-liberty sweep marker must precede the initial player turn.",
                nameof(temporaryLibertyState));
        }

        _ = CounterattackBoundaryResolver.SnapshotPendingAtEnemyTurnStart(
            counterattackState,
            counterattackPolicy);
        ValidateTriggerBindings(
            stoneRuntimeState,
            facilityState,
            closedWindowResources,
            captureBenefitTriggerPlan);

        return new BattleAuthoritativeInitialSnapshot(
            stoneRuntimeState,
            temporaryLibertyState,
            continuousLibertySnapshot,
            repetitionHistory,
            facilityState,
            closedWindowResources,
            captureBenefitTriggerPlan,
            counterattackState,
            counterattackPolicy,
            runtimePolicy,
            playerTurnIndex);
    }

    public string ToCanonicalText() => CanonicalText;

    private string CreateCanonicalText() => string.Join(
        '\n',
        EncodingVersion,
        $"player_turn_index={PlayerTurnIndex.ToString(CultureInfo.InvariantCulture)}",
        "runtime_policy_begin",
        RuntimePolicy.ToCanonicalText(),
        "runtime_policy_end",
        "stone_runtime_begin",
        StoneRuntimeState.ToCanonicalText(),
        "stone_runtime_end",
        "temporary_liberties_begin",
        TemporaryLibertyState.ToCanonicalText(),
        "temporary_liberties_end",
        "continuous_liberties_begin",
        ContinuousLibertySnapshot.ToCanonicalText(),
        "continuous_liberties_end",
        "repetition_history_begin",
        RepetitionHistory.ToCanonicalText(),
        "repetition_history_end",
        "facility_state_begin",
        FacilityState.ToCanonicalText(),
        "facility_state_end",
        "closed_window_resources_begin",
        ClosedWindowResources.ToCanonicalText(),
        "closed_window_resources_end",
        "capture_benefit_trigger_plan_begin",
        CaptureBenefitTriggerPlan.ToCanonicalText(),
        "capture_benefit_trigger_plan_end",
        "counterattack_policy_begin",
        CounterattackPolicy.ToCanonicalText(),
        "counterattack_policy_end",
        "counterattack_state_begin",
        CounterattackState.ToCanonicalText(),
        "counterattack_state_end");

    private static void ValidateTriggerBindings(
        StoneRuntimeState stones,
        FacilityState facilities,
        ClosedWindowResourceState resources,
        CaptureBenefitTriggerPlan plan)
    {
        foreach (var trigger in plan.Triggers)
        {
            if (trigger.FirstUseFlagId is not null)
            {
                _ = resources.IsFirstUseConsumed(trigger.FirstUseFlagId);
            }

            if (trigger.Source.Kind == CaptureBenefitSourceKind.CapturedStoneSelf &&
                stones.InstanceById(trigger.Source.SourceId) is null)
            {
                throw new ArgumentException(
                    $"Captured-stone trigger source {trigger.Source.SourceId} is not present in the initial runtime.",
                    nameof(plan));
            }

            if (trigger.Source.Kind == CaptureBenefitSourceKind.Facility)
            {
                var facility = facilities.FacilityById(trigger.Source.SourceId);
                if (facility is null ||
                    trigger.Source.FacilityPoint is null ||
                    !facility.Point.Equals(trigger.Source.FacilityPoint))
                {
                    throw new ArgumentException(
                        $"Facility trigger source {trigger.Source.SourceId} is not exact-bound to the initial facility state.",
                        nameof(plan));
                }
            }
        }
    }
}

public sealed class BattleAuthoritativeRuntimeState
{
    public const string EncodingVersion = "battle-authoritative-runtime-state-v1";

    internal BattleAuthoritativeRuntimeState(
        StoneRuntimeState stoneRuntimeState,
        TemporaryLibertyState temporaryLibertyState,
        ContinuousLibertySnapshot continuousLibertySnapshot,
        ClosedWindowResourceState closedWindowResources,
        CaptureBenefitTriggerPlan captureBenefitTriggerPlan,
        CounterattackBoundaryState counterattackState,
        CounterattackBoundaryPolicy counterattackPolicy,
        EnemyActionStage? enemyActionStage,
        CounterattackPendingAtStartSnapshot? pendingAtEnemyTurnStart)
    {
        ArgumentNullException.ThrowIfNull(stoneRuntimeState);
        ArgumentNullException.ThrowIfNull(temporaryLibertyState);
        ArgumentNullException.ThrowIfNull(continuousLibertySnapshot);
        ArgumentNullException.ThrowIfNull(closedWindowResources);
        ArgumentNullException.ThrowIfNull(captureBenefitTriggerPlan);
        ArgumentNullException.ThrowIfNull(counterattackState);
        ArgumentNullException.ThrowIfNull(counterattackPolicy);
        if (!ReferenceEquals(temporaryLibertyState.SourceStones, stoneRuntimeState) ||
            !ReferenceEquals(continuousLibertySnapshot.SourceStones, stoneRuntimeState))
        {
            throw new ArgumentException(
                "Authoritative liberties must belong to the exact stone runtime.",
                nameof(stoneRuntimeState));
        }

        _ = CounterattackBoundaryResolver.SnapshotPendingAtEnemyTurnStart(
            counterattackState,
            counterattackPolicy);
        if ((enemyActionStage is null) != (pendingAtEnemyTurnStart is null))
        {
            throw new ArgumentException(
                "Enemy action stage and pending-at-start snapshot must be present together.",
                nameof(enemyActionStage));
        }

        if (enemyActionStage is not null &&
            enemyActionStage is not global::Igorogue.Application.Battle.EnemyActionStage.NormalAction and
            not global::Igorogue.Application.Battle.EnemyActionStage.CounterattackAction)
        {
            throw new ArgumentOutOfRangeException(
                nameof(enemyActionStage),
                enemyActionStage,
                "Unknown enemy action stage.");
        }

        if (enemyActionStage ==
                global::Igorogue.Application.Battle.EnemyActionStage.CounterattackAction &&
            pendingAtEnemyTurnStart?.PendingAtStart != true)
        {
            throw new ArgumentException(
                "Counterattack stage requires a Pending=true enemy-turn snapshot.",
                nameof(pendingAtEnemyTurnStart));
        }

        StoneRuntimeState = stoneRuntimeState;
        TemporaryLibertyState = temporaryLibertyState;
        ContinuousLibertySnapshot = continuousLibertySnapshot;
        ClosedWindowResources = closedWindowResources;
        CaptureBenefitTriggerPlan = captureBenefitTriggerPlan;
        CounterattackState = counterattackState;
        CounterattackPolicy = counterattackPolicy;
        EnemyActionStage = enemyActionStage;
        PendingAtEnemyTurnStart = pendingAtEnemyTurnStart;
        CanonicalText = CreateCanonicalText();
    }

    public StoneRuntimeState StoneRuntimeState { get; }

    public TemporaryLibertyState TemporaryLibertyState { get; }

    public ContinuousLibertySnapshot ContinuousLibertySnapshot { get; }

    public ClosedWindowResourceState ClosedWindowResources { get; }

    public CaptureBenefitTriggerPlan CaptureBenefitTriggerPlan { get; }

    public CounterattackBoundaryState CounterattackState { get; }

    public CounterattackBoundaryPolicy CounterattackPolicy { get; }

    public EnemyActionStage? EnemyActionStage { get; }

    public CounterattackPendingAtStartSnapshot? PendingAtEnemyTurnStart { get; }

    public string CanonicalText { get; }

    public string ToCanonicalText() => CanonicalText;

    internal static BattleAuthoritativeRuntimeState FromInitial(
        BattleAuthoritativeInitialSnapshot initial) =>
        new(
            initial.StoneRuntimeState,
            initial.TemporaryLibertyState,
            initial.ContinuousLibertySnapshot,
            initial.ClosedWindowResources,
            initial.CaptureBenefitTriggerPlan,
            initial.CounterattackState,
            initial.CounterattackPolicy,
            null,
            null);

    internal BattleAuthoritativeRuntimeState Transition(
        StoneRuntimeState? stoneRuntimeState = null,
        TemporaryLibertyState? temporaryLibertyState = null,
        ContinuousLibertySnapshot? continuousLibertySnapshot = null,
        ClosedWindowResourceState? closedWindowResources = null,
        CounterattackBoundaryState? counterattackState = null,
        EnemyActionStage? enemyActionStage = null,
        CounterattackPendingAtStartSnapshot? pendingAtEnemyTurnStart = null,
        bool clearEnemyActionBoundary = false) =>
        new(
            stoneRuntimeState ?? StoneRuntimeState,
            temporaryLibertyState ?? TemporaryLibertyState,
            continuousLibertySnapshot ?? ContinuousLibertySnapshot,
            closedWindowResources ?? ClosedWindowResources,
            CaptureBenefitTriggerPlan,
            counterattackState ?? CounterattackState,
            CounterattackPolicy,
            clearEnemyActionBoundary ? null : enemyActionStage ?? EnemyActionStage,
            clearEnemyActionBoundary
                ? null
                : pendingAtEnemyTurnStart ?? PendingAtEnemyTurnStart);

    private string CreateCanonicalText() => string.Join(
        '\n',
        EncodingVersion,
        $"enemy_action_stage={EnemyActionStageId(EnemyActionStage)}",
        $"pending_at_enemy_turn_start={PendingAtEnemyTurnStart switch { null => "none", { PendingAtStart: true } => "1", _ => "0" }}",
        "stone_runtime_begin",
        StoneRuntimeState.ToCanonicalText(),
        "stone_runtime_end",
        "temporary_liberties_begin",
        TemporaryLibertyState.ToCanonicalText(),
        "temporary_liberties_end",
        "continuous_liberties_begin",
        ContinuousLibertySnapshot.ToCanonicalText(),
        "continuous_liberties_end",
        "closed_window_resources_begin",
        ClosedWindowResources.ToCanonicalText(),
        "closed_window_resources_end",
        "capture_benefit_trigger_plan_begin",
        CaptureBenefitTriggerPlan.ToCanonicalText(),
        "capture_benefit_trigger_plan_end",
        "counterattack_policy_begin",
        CounterattackPolicy.ToCanonicalText(),
        "counterattack_policy_end",
        "counterattack_state_begin",
        CounterattackState.ToCanonicalText(),
        "counterattack_state_end");

    private static string EnemyActionStageId(EnemyActionStage? stage) => stage switch
    {
        null => "none",
        global::Igorogue.Application.Battle.EnemyActionStage.NormalAction =>
            "normal_action",
        global::Igorogue.Application.Battle.EnemyActionStage.CounterattackAction =>
            "counterattack_action",
        _ => throw new InvalidOperationException("Unknown authoritative enemy action stage."),
    };
}
