using System.Globalization;
using Igorogue.Domain.Board;
using Igorogue.Domain.Combat;
using Igorogue.Domain.Determinism;
using Igorogue.Domain.Facilities;

namespace Igorogue.Application.Battle;

public enum BattlePhase : byte
{
    PlayerAction = 1,
    EnemyAction = 2,
    Ended = 3,
}

public sealed class BattleState
{
    public const string EncodingVersion = "headless-battle-state-v1";
    public const string AuthoritativeEncodingVersion = "headless-battle-state-v2";

    private BattleState(
        BoardState board,
        BattleRepetitionHistory repetitionHistory,
        FacilityState facilityState,
        TerritoryAnalysis territoryAnalysis,
        FacilityRuntimeAnalysis facilityRuntimeAnalysis,
        AuthoritativeRngState rngState,
        BattleRuntimePolicy runtimePolicy,
        int playerTurnIndex,
        BattlePhase phase,
        BattleOutcome outcome,
        BattleEndReason endReason,
        BattleAuthoritativeRuntimeState? authoritativeRuntime)
    {
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(repetitionHistory);
        ArgumentNullException.ThrowIfNull(facilityState);
        ArgumentNullException.ThrowIfNull(territoryAnalysis);
        ArgumentNullException.ThrowIfNull(facilityRuntimeAnalysis);
        ArgumentNullException.ThrowIfNull(rngState);
        ArgumentNullException.ThrowIfNull(runtimePolicy);
        ValidateBindings(
            board,
            repetitionHistory,
            facilityState,
            territoryAnalysis,
            facilityRuntimeAnalysis,
            runtimePolicy,
            authoritativeRuntime);
        ValidateLifecycle(
            playerTurnIndex,
            phase,
            outcome,
            endReason,
            runtimePolicy,
            authoritativeRuntime);

        Board = board;
        RepetitionHistory = repetitionHistory;
        FacilityState = facilityState;
        TerritoryAnalysis = territoryAnalysis;
        FacilityRuntimeAnalysis = facilityRuntimeAnalysis;
        RngState = rngState;
        RuntimePolicy = runtimePolicy;
        PlayerTurnIndex = playerTurnIndex;
        Phase = phase;
        Outcome = outcome;
        EndReason = endReason;
        AuthoritativeRuntime = authoritativeRuntime;
        CanonicalText = BuildCanonicalText();
        Checksum = DeterministicChecksum.Sha256Hex(CanonicalText);
    }

    public BoardState Board { get; }

    public BattleRepetitionHistory RepetitionHistory { get; }

    public FacilityState FacilityState { get; }

    public TerritoryAnalysis TerritoryAnalysis { get; }

    public FacilityRuntimeAnalysis FacilityRuntimeAnalysis { get; }

    public AuthoritativeRngState RngState { get; }

    public BattleRuntimePolicy RuntimePolicy { get; }

    public int PlayerTurnIndex { get; }

    public BattlePhase Phase { get; }

    public BattleOutcome Outcome { get; }

    public BattleEndReason EndReason { get; }

    public BattleAuthoritativeRuntimeState? AuthoritativeRuntime { get; }

    public string StateProjectionId => AuthoritativeRuntime is null
        ? EncodingVersion
        : AuthoritativeEncodingVersion;

    public bool IsTerminal => Phase == BattlePhase.Ended;

    public string PhaseId => Phase switch
    {
        BattlePhase.PlayerAction => "player_action",
        BattlePhase.EnemyAction => "enemy_action",
        BattlePhase.Ended => "ended",
        _ => throw new InvalidOperationException("Battle state contains an unknown phase."),
    };

    public string OutcomeId => Outcome switch
    {
        BattleOutcome.Ongoing => "ongoing",
        BattleOutcome.PlayerVictory => "win",
        BattleOutcome.PlayerDefeat => "loss",
        _ => throw new InvalidOperationException("Battle state contains an unknown outcome."),
    };

    public string EndReasonId => BattleEndReasonRules.ToReasonId(EndReason);

    public string CanonicalText { get; }

    public string Checksum { get; }

    public string ToCanonicalText() => CanonicalText;

    internal static BattleState Start(
        BoardState board,
        FacilityState facilityState,
        AuthoritativeRngState rngState,
        BattleRuntimePolicy runtimePolicy) =>
        Start(
            board,
            BattleRepetitionHistory.Start(board),
            facilityState,
            rngState,
            runtimePolicy);

    internal static BattleState Start(
        BoardState board,
        BattleRepetitionHistory repetitionHistory,
        FacilityState facilityState,
        AuthoritativeRngState rngState,
        BattleRuntimePolicy runtimePolicy) =>
        Start(
            board,
            repetitionHistory,
            facilityState,
            rngState,
            runtimePolicy,
            playerTurnIndex: 1);

    internal static BattleState Start(
        BoardState board,
        BattleRepetitionHistory repetitionHistory,
        FacilityState facilityState,
        AuthoritativeRngState rngState,
        BattleRuntimePolicy runtimePolicy,
        int playerTurnIndex)
    {
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(repetitionHistory);
        ArgumentNullException.ThrowIfNull(facilityState);
        ArgumentNullException.ThrowIfNull(rngState);
        ArgumentNullException.ThrowIfNull(runtimePolicy);
        if (!ReferenceEquals(facilityState.SourceBoard, board))
        {
            throw new ArgumentException(
                "Initial facility state must belong to the exact initial board snapshot.",
                nameof(facilityState));
        }

        var territory = TerritoryAnalyzer.Analyze(board);
        var facilityRuntime = FacilityRuntimeAnalyzer.Analyze(
            facilityState,
            territory,
            runtimePolicy.FacilityPolicy);
        return new BattleState(
            board,
            repetitionHistory,
            facilityState,
            territory,
            facilityRuntime,
            rngState,
            runtimePolicy,
            playerTurnIndex,
            BattlePhase.PlayerAction,
            BattleOutcome.Ongoing,
            BattleEndReason.None,
            null);
    }

    internal static BattleState Start(
        BattleAuthoritativeInitialSnapshot initial,
        AuthoritativeRngState rngState)
    {
        ArgumentNullException.ThrowIfNull(initial);
        ArgumentNullException.ThrowIfNull(rngState);
        var territory = TerritoryAnalyzer.Analyze(initial.Board);
        var facilityRuntime = FacilityRuntimeAnalyzer.Analyze(
            initial.FacilityState,
            territory,
            initial.RuntimePolicy.FacilityPolicy);
        return new BattleState(
            initial.Board,
            initial.RepetitionHistory,
            initial.FacilityState,
            territory,
            facilityRuntime,
            rngState,
            initial.RuntimePolicy,
            initial.PlayerTurnIndex,
            BattlePhase.PlayerAction,
            BattleOutcome.Ongoing,
            BattleEndReason.None,
            BattleAuthoritativeRuntimeState.FromInitial(initial));
    }

    internal static BattleState Transition(
        BattleState source,
        BoardState board,
        BattleRepetitionHistory repetitionHistory,
        FacilityState facilityState,
        TerritoryAnalysis territoryAnalysis,
        FacilityRuntimeAnalysis facilityRuntimeAnalysis,
        int playerTurnIndex,
        BattlePhase phase,
        BattleOutcome outcome,
        BattleEndReason endReason)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (source.AuthoritativeRuntime is not null)
        {
            throw new ArgumentException(
                "Legacy battle transition cannot discard authoritative runtime state.",
                nameof(source));
        }

        return new BattleState(
            board,
            repetitionHistory,
            facilityState,
            territoryAnalysis,
            facilityRuntimeAnalysis,
            source.RngState,
            source.RuntimePolicy,
            playerTurnIndex,
            phase,
            outcome,
            endReason,
            null);
    }

    internal static BattleState RebindRng(
        BattleState source,
        AuthoritativeRngState rngState)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(rngState);
        if (source.AuthoritativeRuntime is not null)
        {
            throw new ArgumentException(
                "Legacy RNG rebind cannot replace authoritative runtime ownership.",
                nameof(source));
        }

        if (ReferenceEquals(source.RngState, rngState))
        {
            return source;
        }

        return new BattleState(
            source.Board,
            source.RepetitionHistory,
            source.FacilityState,
            source.TerritoryAnalysis,
            source.FacilityRuntimeAnalysis,
            rngState,
            source.RuntimePolicy,
            source.PlayerTurnIndex,
            source.Phase,
            source.Outcome,
            source.EndReason,
            null);
    }

    internal static BattleState TransitionAuthoritative(
        BattleState source,
        BoardState board,
        BattleRepetitionHistory repetitionHistory,
        FacilityState facilityState,
        TerritoryAnalysis territoryAnalysis,
        FacilityRuntimeAnalysis facilityRuntimeAnalysis,
        BattleAuthoritativeRuntimeState authoritativeRuntime,
        int playerTurnIndex,
        BattlePhase phase,
        BattleOutcome outcome,
        BattleEndReason endReason)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(authoritativeRuntime);
        if (source.AuthoritativeRuntime is null)
        {
            throw new ArgumentException(
                "Authoritative battle transition requires an authoritative source state.",
                nameof(source));
        }

        return new BattleState(
            board,
            repetitionHistory,
            facilityState,
            territoryAnalysis,
            facilityRuntimeAnalysis,
            source.RngState,
            source.RuntimePolicy,
            playerTurnIndex,
            phase,
            outcome,
            endReason,
            authoritativeRuntime);
    }

    private string BuildCanonicalText() => AuthoritativeRuntime is null
        ? BuildLegacyCanonicalText()
        : BuildAuthoritativeCanonicalText();

    private string BuildLegacyCanonicalText() => string.Join(
        '\n',
        EncodingVersion,
        $"player_turn_index={PlayerTurnIndex.ToString(CultureInfo.InvariantCulture)}",
        $"phase={PhaseId}",
        $"outcome={OutcomeId}",
        $"end_reason={EndReasonId}",
        "policy_begin",
        RuntimePolicy.ToCanonicalText(),
        "policy_end",
        "board_topology_begin",
        StoneTopologyKey.FromBoard(Board).ToCanonicalText(),
        "board_topology_end",
        "repetition_history_begin",
        RepetitionHistory.ToCanonicalText(),
        "repetition_history_end",
        "facility_state_begin",
        FacilityState.ToCanonicalText(),
        "facility_state_end",
        "territory_begin",
        TerritoryToCanonicalText(TerritoryAnalysis),
        "territory_end",
        "rng_begin",
        RngState.ToCanonicalText(),
        "rng_end");

    private string BuildAuthoritativeCanonicalText() => string.Join(
        '\n',
        AuthoritativeEncodingVersion,
        $"player_turn_index={PlayerTurnIndex.ToString(CultureInfo.InvariantCulture)}",
        $"phase={PhaseId}",
        $"outcome={OutcomeId}",
        $"end_reason={EndReasonId}",
        "policy_begin",
        RuntimePolicy.ToCanonicalText(),
        "policy_end",
        "board_topology_begin",
        StoneTopologyKey.FromBoard(Board).ToCanonicalText(),
        "board_topology_end",
        "repetition_history_begin",
        RepetitionHistory.ToCanonicalText(),
        "repetition_history_end",
        "facility_state_begin",
        FacilityState.ToCanonicalText(),
        "facility_state_end",
        "territory_begin",
        TerritoryToCanonicalText(TerritoryAnalysis),
        "territory_end",
        "authoritative_runtime_begin",
        AuthoritativeRuntime!.ToCanonicalText(),
        "authoritative_runtime_end",
        "rng_begin",
        RngState.ToCanonicalText(),
        "rng_end");

    private static string TerritoryToCanonicalText(TerritoryAnalysis territory)
    {
        var lines = new List<string>
        {
            "territory-analysis-v1",
            $"region_count={territory.Regions.Count.ToString(CultureInfo.InvariantCulture)}",
        };
        for (var index = 0; index < territory.Regions.Count; index++)
        {
            var region = territory.Regions[index];
            lines.Add($"region_index={index.ToString(CultureInfo.InvariantCulture)}");
            lines.Add($"owner={TerritoryOwnerId(region.Owner)}");
            lines.Add(
                $"anchor={region.Anchor.X.ToString(CultureInfo.InvariantCulture)}," +
                region.Anchor.Y.ToString(CultureInfo.InvariantCulture));
            lines.Add($"point_count={region.Points.Count.ToString(CultureInfo.InvariantCulture)}");
            foreach (var point in region.Points)
            {
                lines.Add(
                    $"point={point.X.ToString(CultureInfo.InvariantCulture)}," +
                    point.Y.ToString(CultureInfo.InvariantCulture));
            }
        }

        return string.Join('\n', lines);
    }

    private static string TerritoryOwnerId(TerritoryOwner owner) => owner switch
    {
        TerritoryOwner.Neutral => "neutral",
        TerritoryOwner.Black => "black",
        TerritoryOwner.White => "white",
        _ => throw new InvalidOperationException("Territory analysis contains an unknown owner."),
    };

    private static void ValidateBindings(
        BoardState board,
        BattleRepetitionHistory repetitionHistory,
        FacilityState facilityState,
        TerritoryAnalysis territoryAnalysis,
        FacilityRuntimeAnalysis facilityRuntimeAnalysis,
        BattleRuntimePolicy runtimePolicy,
        BattleAuthoritativeRuntimeState? authoritativeRuntime)
    {
        if (!repetitionHistory.Current.Equals(StoneTopologyKey.FromBoard(board)))
        {
            throw new ArgumentException(
                "Battle repetition history must end at the exact board topology.",
                nameof(repetitionHistory));
        }

        if (!ReferenceEquals(facilityState.SourceBoard, board) ||
            !ReferenceEquals(territoryAnalysis.SourceBoard, board))
        {
            throw new ArgumentException(
                "Facility and territory state must belong to the exact board snapshot.",
                nameof(board));
        }

        if (!ReferenceEquals(facilityRuntimeAnalysis.FacilityState, facilityState) ||
            !ReferenceEquals(facilityRuntimeAnalysis.TerritoryAnalysis, territoryAnalysis) ||
            !ReferenceEquals(facilityRuntimeAnalysis.Policy, runtimePolicy.FacilityPolicy))
        {
            throw new ArgumentException(
                "Facility runtime analysis must belong to the exact battle snapshots and policy.",
                nameof(facilityRuntimeAnalysis));
        }

        if (authoritativeRuntime is not null &&
            (!ReferenceEquals(authoritativeRuntime.StoneRuntimeState.SourceBoard, board) ||
             !ReferenceEquals(
                 authoritativeRuntime.TemporaryLibertyState.SourceStones,
                 authoritativeRuntime.StoneRuntimeState) ||
             !ReferenceEquals(
                 authoritativeRuntime.ContinuousLibertySnapshot.SourceStones,
                 authoritativeRuntime.StoneRuntimeState)))
        {
            throw new ArgumentException(
                "Authoritative runtime must belong to the exact battle board and stone snapshot.",
                nameof(authoritativeRuntime));
        }
    }

    private static void ValidateLifecycle(
        int playerTurnIndex,
        BattlePhase phase,
        BattleOutcome outcome,
        BattleEndReason endReason,
        BattleRuntimePolicy runtimePolicy,
        BattleAuthoritativeRuntimeState? authoritativeRuntime)
    {
        if (playerTurnIndex <= 0 || playerTurnIndex > runtimePolicy.PlayerTurnLimit)
        {
            throw new ArgumentOutOfRangeException(
                nameof(playerTurnIndex),
                playerTurnIndex,
                "Player turn index must be within the configured battle limit.");
        }

        if (phase is not BattlePhase.PlayerAction and
            not BattlePhase.EnemyAction and
            not BattlePhase.Ended)
        {
            throw new ArgumentOutOfRangeException(nameof(phase), phase, "Unknown battle phase.");
        }

        if (phase == BattlePhase.Ended)
        {
            if (outcome == BattleOutcome.Ongoing || endReason == BattleEndReason.None)
            {
                throw new ArgumentException("Ended battle state requires a terminal outcome and reason.");
            }

            BattleEndReasonRules.ValidateTerminalPair(outcome, endReason);
            if (authoritativeRuntime?.EnemyActionStage is not null ||
                authoritativeRuntime?.PendingAtEnemyTurnStart is not null)
            {
                throw new ArgumentException(
                    "Ended authoritative battle state cannot retain an enemy action boundary.");
            }

            return;
        }

        if (outcome != BattleOutcome.Ongoing || endReason != BattleEndReason.None)
        {
            throw new ArgumentException("Ongoing battle phase cannot contain a terminal outcome or reason.");
        }

        if (authoritativeRuntime is null)
        {
            return;
        }

        var hasEnemyBoundary = authoritativeRuntime.EnemyActionStage is not null &&
            authoritativeRuntime.PendingAtEnemyTurnStart is not null;
        if ((phase == BattlePhase.EnemyAction) != hasEnemyBoundary)
        {
            throw new ArgumentException(
                "Authoritative enemy phase and enemy action boundary state must agree.");
        }
    }
}
