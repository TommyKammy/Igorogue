using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

using Igorogue.Application.Replay;
using Igorogue.Domain.Cards;
using Igorogue.Domain.Combat;
using Igorogue.Domain.Determinism;
using Igorogue.Domain.Enemies;

namespace Igorogue.Application.Battle;

public sealed class CoreDuelBattleState
{
    public const string EncodingVersion = "headless-core-duel-state-v1";

    private CoreDuelBattleState(
        CoreDuelBattleBootstrap bootstrap,
        BattleState battleState,
        CoreDuelCardTurnState cardTurnState,
        PlannedEnemyIntent? normalPlan,
        PlannedEnemyIntent? bonusPlan,
        int restartCount)
    {
        ArgumentNullException.ThrowIfNull(bootstrap);
        ArgumentNullException.ThrowIfNull(battleState);
        ArgumentNullException.ThrowIfNull(cardTurnState);
        if (restartCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(restartCount),
                restartCount,
                "Restart count cannot be negative.");
        }

        var runtime = battleState.AuthoritativeRuntime
            ?? throw new ArgumentException(
                "Core Duel aggregate requires embedded authoritative runtime state.",
                nameof(battleState));
        if (battleState.RngState.InitialSeed != bootstrap.Metadata.InitialSeed ||
            cardTurnState.RngState.InitialSeed != bootstrap.Metadata.InitialSeed)
        {
            throw new ArgumentException(
                "Core Duel state RNG identity must match its immutable bootstrap.");
        }

        if (!battleState.RngState.Equals(cardTurnState.RngState))
        {
            throw new ArgumentException(
                "Battle and card-turn state must share the same authoritative RNG snapshot.",
                nameof(cardTurnState));
        }

        if (!ReferenceEquals(runtime.ClosedWindowResources, cardTurnState.ClosedWindowResources))
        {
            throw new ArgumentException(
                "Battle runtime and card-turn state must share the exact resource snapshot.",
                nameof(cardTurnState));
        }

        ValidatePlanLifecycle(battleState, runtime, normalPlan, bonusPlan);

        Bootstrap = bootstrap;
        BattleState = battleState;
        CardTurnState = cardTurnState;
        NormalPlan = normalPlan;
        BonusPlan = bonusPlan;
        RestartCount = restartCount;
        CanonicalText = CreateCanonicalText();
        Checksum = DeterministicChecksum.Sha256Hex(CanonicalText);
    }

    public CoreDuelBattleBootstrap Bootstrap { get; }

    public BattleState BattleState { get; }

    public BattleAuthoritativeRuntimeState RuntimeState =>
        BattleState.AuthoritativeRuntime
        ?? throw new InvalidOperationException("Core Duel aggregate lost authoritative runtime state.");

    public CoreDuelCardTurnState CardTurnState { get; }

    public PlannedEnemyIntent? NormalPlan { get; }

    public PlannedEnemyIntent? BonusPlan { get; }

    public PlannedEnemyIntent? DisplayedPlan => BattleState.Phase switch
    {
        BattlePhase.PlayerAction => NormalPlan,
        BattlePhase.EnemyAction when RuntimeState.EnemyActionStage == EnemyActionStage.NormalAction =>
            NormalPlan,
        BattlePhase.EnemyAction when RuntimeState.EnemyActionStage ==
            EnemyActionStage.CounterattackAction => BonusPlan,
        _ => null,
    };

    public int RestartCount { get; }

    public bool IsTerminal => BattleState.IsTerminal;

    public string CanonicalText { get; }

    public string Checksum { get; }

    public string ToCanonicalText() => CanonicalText;

    internal static CoreDuelBattleState Create(
        CoreDuelBattleBootstrap bootstrap,
        BattleState battleState,
        CoreDuelCardTurnState cardTurnState,
        PlannedEnemyIntent? normalPlan,
        PlannedEnemyIntent? bonusPlan,
        int restartCount) =>
        new(bootstrap, battleState, cardTurnState, normalPlan, bonusPlan, restartCount);

    private string CreateCanonicalText() => string.Join(
        '\n',
        EncodingVersion,
        $"restart_count={RestartCount.ToString(CultureInfo.InvariantCulture)}",
        $"bootstrap={EncodeStableText(Bootstrap.ToCanonicalText())}",
        $"battle_state={EncodeStableText(BattleState.ToCanonicalText())}",
        $"card_turn_state={EncodeStableText(CardTurnState.ToCanonicalText())}",
        $"normal_plan={EncodePlan(NormalPlan)}",
        $"bonus_plan={EncodePlan(BonusPlan)}");

    private static string EncodePlan(PlannedEnemyIntent? plan) =>
        plan is null ? "none" : EncodeStableText(plan.ToCanonicalText());

    private static string EncodeStableText(string value) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(value));

    private static void ValidatePlanLifecycle(
        BattleState battleState,
        BattleAuthoritativeRuntimeState runtime,
        PlannedEnemyIntent? normalPlan,
        PlannedEnemyIntent? bonusPlan)
    {
        if (battleState.IsTerminal)
        {
            if (normalPlan is not null || bonusPlan is not null)
            {
                throw new ArgumentException("Terminal Core Duel state cannot retain future plans.");
            }

            return;
        }

        if (battleState.Phase == BattlePhase.PlayerAction)
        {
            if (normalPlan is null ||
                ((bonusPlan is not null) != runtime.CounterattackState.Pending))
            {
                throw new ArgumentException(
                    "Player phase requires a normal plan and a bonus plan exactly when Pending is set.");
            }

            return;
        }

        if (runtime.EnemyActionStage == EnemyActionStage.NormalAction)
        {
            if (normalPlan is null ||
                ((bonusPlan is not null) !=
                 (runtime.PendingAtEnemyTurnStart?.PendingAtStart == true)))
            {
                throw new ArgumentException(
                    "Normal enemy action requires its normal plan and any snapshotted bonus plan.");
            }

            return;
        }

        if (runtime.EnemyActionStage == EnemyActionStage.CounterattackAction &&
            normalPlan is null &&
            bonusPlan is not null)
        {
            return;
        }

        throw new ArgumentException("Core Duel state contains an invalid enemy-plan lifecycle.");
    }
}

public sealed class CoreDuelBattleSession
{
    internal CoreDuelBattleSession(
        CoreDuelBattleState state,
        OrderedCommandLog commandLog)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(commandLog);
        if (!StringComparer.Ordinal.Equals(
                commandLog.Metadata.ToCanonicalText(),
                state.Bootstrap.Metadata.ToCanonicalText()))
        {
            throw new ArgumentException(
                "Core Duel command-log identity must match its immutable bootstrap.",
                nameof(commandLog));
        }

        if (commandLog.Entries.Count > 0 &&
            !StringComparer.Ordinal.Equals(
                commandLog.Entries[^1].ResultChecksum,
                state.Checksum))
        {
            throw new ArgumentException(
                "The outer Core Duel log must end at the aggregate state checksum.",
                nameof(commandLog));
        }

        State = state;
        CommandLog = commandLog;
    }

    public CoreDuelBattleState State { get; }

    public OrderedCommandLog CommandLog { get; }
}

public sealed class CoreDuelBattleStartResult
{
    private readonly ReadOnlyCollection<IBattleFact> orderedFactView;

    internal CoreDuelBattleStartResult(
        CoreDuelBattleSession session,
        IEnumerable<IBattleFact> orderedFacts)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(orderedFacts);
        var facts = orderedFacts.ToArray();
        foreach (var fact in facts)
        {
            ArgumentNullException.ThrowIfNull(fact);
        }

        Session = session;
        orderedFactView = Array.AsReadOnly(facts);
    }

    public CoreDuelBattleSession Session { get; }

    public IReadOnlyList<IBattleFact> OrderedFacts => orderedFactView;
}

public sealed class CoreDuelBattleCommandResult
{
    private readonly ReadOnlyCollection<IBattleFact> orderedFactView;

    internal CoreDuelBattleCommandResult(
        CoreDuelBattleSession sessionBefore,
        CoreDuelBattleSession sessionAfter,
        IBattleCommand command,
        bool accepted,
        string reasonId,
        IEnumerable<IBattleFact> orderedFacts)
    {
        ArgumentNullException.ThrowIfNull(sessionBefore);
        ArgumentNullException.ThrowIfNull(sessionAfter);
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(reasonId);
        ArgumentNullException.ThrowIfNull(orderedFacts);
        var facts = orderedFacts.ToArray();
        foreach (var fact in facts)
        {
            ArgumentNullException.ThrowIfNull(fact);
        }

        if (!accepted &&
            (!ReferenceEquals(sessionBefore, sessionAfter) ||
             facts.Length != 1 ||
             facts[0] is not CommandRejectedFact))
        {
            throw new ArgumentException(
                "Rejected Core Duel commands must preserve the exact session and one rejection fact.");
        }

        SessionBefore = sessionBefore;
        SessionAfter = sessionAfter;
        Command = command;
        Accepted = accepted;
        ReasonId = reasonId;
        orderedFactView = Array.AsReadOnly(facts);
    }

    public CoreDuelBattleSession SessionBefore { get; }

    public CoreDuelBattleSession SessionAfter { get; }

    public IBattleCommand Command { get; }

    public bool Accepted { get; }

    public string ReasonId { get; }

    public IReadOnlyList<IBattleFact> OrderedFacts => orderedFactView;

    public string StateChecksum => SessionAfter.State.Checksum;

    public string LogChecksum => SessionAfter.CommandLog.CurrentChecksum;
}
