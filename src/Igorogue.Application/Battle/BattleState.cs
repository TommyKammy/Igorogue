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

public enum BattleEndReason : byte
{
    None = 1,
    WhiteKingCaptured = 2,
    BlackKingCaptured = 3,
    BothKingsCaptured = 4,
    TurnLimit = 5,
}

public sealed class BattleState
{
    public const string EncodingVersion = "headless-battle-state-v1";

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
        BattleEndReason endReason)
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
            runtimePolicy);
        ValidateLifecycle(playerTurnIndex, phase, outcome, endReason, runtimePolicy);

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

    public string EndReasonId => EndReason switch
    {
        BattleEndReason.None => "none",
        BattleEndReason.WhiteKingCaptured => "white_king_captured",
        BattleEndReason.BlackKingCaptured => "black_king_captured",
        BattleEndReason.BothKingsCaptured => "both_kings_captured",
        BattleEndReason.TurnLimit => "turn_limit",
        _ => throw new InvalidOperationException("Battle state contains an unknown end reason."),
    };

    public string CanonicalText { get; }

    public string Checksum { get; }

    public string ToCanonicalText() => CanonicalText;

    internal static BattleState Start(
        BoardState board,
        FacilityState facilityState,
        AuthoritativeRngState rngState,
        BattleRuntimePolicy runtimePolicy)
    {
        ArgumentNullException.ThrowIfNull(board);
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
            BattleRepetitionHistory.Start(board),
            facilityState,
            territory,
            facilityRuntime,
            rngState,
            runtimePolicy,
            1,
            BattlePhase.PlayerAction,
            BattleOutcome.Ongoing,
            BattleEndReason.None);
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
            endReason);
    }

    private string BuildCanonicalText() => string.Join(
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
        BattleRuntimePolicy runtimePolicy)
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
    }

    private static void ValidateLifecycle(
        int playerTurnIndex,
        BattlePhase phase,
        BattleOutcome outcome,
        BattleEndReason endReason,
        BattleRuntimePolicy runtimePolicy)
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

            return;
        }

        if (outcome != BattleOutcome.Ongoing || endReason != BattleEndReason.None)
        {
            throw new ArgumentException("Ongoing battle phase cannot contain a terminal outcome or reason.");
        }
    }
}
