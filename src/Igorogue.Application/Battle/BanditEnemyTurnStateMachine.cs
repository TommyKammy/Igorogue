using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

using Igorogue.Application.Replay;
using Igorogue.Domain.Board;
using Igorogue.Domain.Combat;
using Igorogue.Domain.Content;
using Igorogue.Domain.Determinism;
using Igorogue.Domain.Enemies;

namespace Igorogue.Application.Battle;

public sealed class ResolveBanditEnemyActionCommand : IBattleCommand
{
    public ResolveBanditEnemyActionCommand(
        string expectedStateChecksum,
        string expectedLogChecksum)
    {
        ExpectedStateChecksum = BattleCommandValidation.CanonicalChecksum(
            expectedStateChecksum,
            nameof(expectedStateChecksum));
        ExpectedLogChecksum = BattleCommandValidation.CanonicalChecksum(
            expectedLogChecksum,
            nameof(expectedLogChecksum));
    }

    public string CommandType => "battle.resolve_bandit_enemy_action";

    public int CommandSchemaVersion => 1;

    public string ExpectedStateChecksum { get; }

    public string ExpectedLogChecksum { get; }

    public string ToCanonicalPayload() =>
        "resolve-bandit-enemy-action-v1\n" +
        $"expected_state_checksum={ExpectedStateChecksum}\n" +
        $"expected_log_checksum={ExpectedLogChecksum}\n";
}

public enum BanditMandatoryOverrideKind : byte
{
    None = 1,
    Lethal = 2,
    Defense = 3,
}

public sealed class BanditMandatoryOverridePreview
{
    internal BanditMandatoryOverridePreview(
        BanditMandatoryOverrideKind kind,
        BanditExecutionDecision? decision)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown Bandit override kind.");
        }

        if ((kind == BanditMandatoryOverrideKind.None) != (decision is null))
        {
            throw new ArgumentException(
                "A mandatory override decision must be present exactly when an override exists.",
                nameof(decision));
        }

        Kind = kind;
        Decision = decision;
    }

    public BanditMandatoryOverrideKind Kind { get; }

    public bool HasOverride => Kind != BanditMandatoryOverrideKind.None;

    public BanditExecutionDecision? Decision { get; }

    public CanonicalPoint? Point => Decision?.Candidate?.Point;
}

public sealed class BanditEnemyTurnState
{
    public const string EncodingVersion = "bandit-enemy-turn-state-v1";

    private const string BanditContentId = "enemy_bandit";

    private BanditEnemyTurnState(
        string contentHash,
        EnemyContentDefinition enemyDefinition,
        HeadlessBattleSession battleSession,
        PlannedEnemyIntent? normalPlan,
        PlannedEnemyIntent? bonusPlan)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentHash);
        ArgumentNullException.ThrowIfNull(enemyDefinition);
        ArgumentNullException.ThrowIfNull(battleSession);
        if (!StringComparer.Ordinal.Equals(enemyDefinition.Id, BanditContentId))
        {
            throw new ArgumentException(
                $"Bandit sidecar requires enemy content {BanditContentId}.",
                nameof(enemyDefinition));
        }

        if (!StringComparer.Ordinal.Equals(
                battleSession.CommandLog.Metadata.ContentHash,
                contentHash))
        {
            throw new ArgumentException(
                "Bandit sidecar content hash must match the underlying battle log.",
                nameof(contentHash));
        }

        if (battleSession.State.AuthoritativeRuntime is null)
        {
            throw new ArgumentException(
                "Bandit sidecar requires the existing authoritative enemy boundary.",
                nameof(battleSession));
        }

        ValidatePlanLifecycle(battleSession.State, normalPlan, bonusPlan);

        ContentHash = contentHash;
        EnemyDefinition = enemyDefinition;
        BattleSession = battleSession;
        NormalPlan = normalPlan;
        BonusPlan = bonusPlan;
        CanonicalText = CreateCanonicalText();
        Checksum = DeterministicChecksum.Sha256Hex(CanonicalText);
    }

    public string ContentHash { get; }

    public EnemyContentDefinition EnemyDefinition { get; }

    public HeadlessBattleSession BattleSession { get; }

    public BattleState BattleState => BattleSession.State;

    public BattleAuthoritativeRuntimeState RuntimeState =>
        BattleState.AuthoritativeRuntime
        ?? throw new InvalidOperationException("Bandit sidecar lost its authoritative runtime.");

    public PlannedEnemyIntent? NormalPlan { get; }

    public PlannedEnemyIntent? BonusPlan { get; }

    public PlannedEnemyIntent? DisplayedPlan => BattleState.Phase switch
    {
        BattlePhase.PlayerAction => NormalPlan,
        BattlePhase.EnemyAction when RuntimeState.EnemyActionStage == EnemyActionStage.NormalAction =>
            NormalPlan,
        BattlePhase.EnemyAction when RuntimeState.EnemyActionStage == EnemyActionStage.CounterattackAction =>
            BonusPlan,
        _ => null,
    };

    public bool IsTerminal => BattleState.IsTerminal;

    public string CanonicalText { get; }

    public string Checksum { get; }

    public string ToCanonicalText() => CanonicalText;

    internal static BanditEnemyTurnState Create(
        string contentHash,
        EnemyContentDefinition enemyDefinition,
        HeadlessBattleSession battleSession,
        PlannedEnemyIntent? normalPlan,
        PlannedEnemyIntent? bonusPlan) =>
        new(contentHash, enemyDefinition, battleSession, normalPlan, bonusPlan);

    private string CreateCanonicalText() => string.Join(
        '\n',
        EncodingVersion,
        $"content_hash={ContentHash}",
        $"enemy_definition={EncodeStableText(EnemyDefinition.ToCanonicalText())}",
        $"battle_state={EncodeStableText(BattleState.ToCanonicalText())}",
        $"battle_log_checksum={BattleSession.CommandLog.CurrentChecksum}",
        $"normal_plan={EncodePlan(NormalPlan)}",
        $"bonus_plan={EncodePlan(BonusPlan)}");

    private static string EncodePlan(PlannedEnemyIntent? plan) =>
        plan is null ? "none" : EncodeStableText(plan.ToCanonicalText());

    private static string EncodeStableText(string value) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(value));

    private static void ValidatePlanLifecycle(
        BattleState battleState,
        PlannedEnemyIntent? normalPlan,
        PlannedEnemyIntent? bonusPlan)
    {
        var runtime = battleState.AuthoritativeRuntime!;
        if (battleState.IsTerminal)
        {
            if (normalPlan is not null || bonusPlan is not null)
            {
                throw new ArgumentException("Terminal Bandit state cannot retain future plans.");
            }

            return;
        }

        if (battleState.Phase == BattlePhase.PlayerAction)
        {
            if (normalPlan is null)
            {
                throw new ArgumentException("Player phase requires a displayed normal Bandit plan.");
            }

            if ((bonusPlan is not null) != runtime.CounterattackState.Pending)
            {
                throw new ArgumentException(
                    "Player phase may retain a bonus plan exactly when existing Pending is confirmed.");
            }

            return;
        }

        if (runtime.EnemyActionStage == EnemyActionStage.NormalAction)
        {
            if (normalPlan is null)
            {
                throw new ArgumentException("Normal enemy action requires its stored normal plan.");
            }

            if ((bonusPlan is not null) !=
                (runtime.PendingAtEnemyTurnStart?.PendingAtStart == true))
            {
                throw new ArgumentException(
                    "Normal action may retain a bonus plan exactly when Pending was snapshotted true.");
            }

            return;
        }

        if (runtime.EnemyActionStage == EnemyActionStage.CounterattackAction)
        {
            if (normalPlan is not null || bonusPlan is null)
            {
                throw new ArgumentException(
                    "Counterattack action requires only its confirmed bonus plan.");
            }

            return;
        }

        throw new ArgumentException("Ongoing Bandit state contains an unsupported battle phase.");
    }
}

public sealed class BanditEnemyTurnSession
{
    internal BanditEnemyTurnSession(
        BanditEnemyTurnState state,
        OrderedCommandLog commandLog)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(commandLog);
        if (commandLog.Metadata.InitialSeed != state.BattleState.RngState.InitialSeed ||
            !StringComparer.Ordinal.Equals(commandLog.Metadata.ContentHash, state.ContentHash))
        {
            throw new ArgumentException(
                "Outer Bandit command-log identity must match the sidecar state.",
                nameof(commandLog));
        }

        if (commandLog.Entries.Count > 0 &&
            !StringComparer.Ordinal.Equals(
                commandLog.Entries[^1].ResultChecksum,
                state.Checksum))
        {
            throw new ArgumentException(
                "The outer Bandit log must end at the sidecar checksum.",
                nameof(commandLog));
        }

        State = state;
        CommandLog = commandLog;
    }

    public BanditEnemyTurnState State { get; }

    public OrderedCommandLog CommandLog { get; }
}

public sealed class BanditEnemyTurnStartResult
{
    private readonly ReadOnlyCollection<IBattleFact> orderedFactView;

    internal BanditEnemyTurnStartResult(
        BanditEnemyTurnSession session,
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

    public BanditEnemyTurnSession Session { get; }

    public IReadOnlyList<IBattleFact> OrderedFacts => orderedFactView;
}

public sealed class BanditEnemyTurnResult
{
    private readonly ReadOnlyCollection<IBattleFact> orderedFactView;

    internal BanditEnemyTurnResult(
        BanditEnemyTurnSession sessionBefore,
        BanditEnemyTurnSession sessionAfter,
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
                "Rejected Bandit commands must preserve the exact session and one rejection fact.");
        }

        SessionBefore = sessionBefore;
        SessionAfter = sessionAfter;
        Command = command;
        Accepted = accepted;
        ReasonId = reasonId;
        orderedFactView = Array.AsReadOnly(facts);
    }

    public BanditEnemyTurnSession SessionBefore { get; }

    public BanditEnemyTurnSession SessionAfter { get; }

    public IBattleCommand Command { get; }

    public bool Accepted { get; }

    public string ReasonId { get; }

    public IReadOnlyList<IBattleFact> OrderedFacts => orderedFactView;

    public string StateChecksum => SessionAfter.State.Checksum;

    public string LogChecksum => SessionAfter.CommandLog.CurrentChecksum;
}

public static class BanditEnemyTurnStateMachine
{
    private const string StandardStoneKindId = "standard";

    public static BanditEnemyTurnStartResult Start(
        BattleAuthoritativeInitialSnapshot initial,
        EnemyContentDefinition enemyDefinition,
        ReplayMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(initial);
        ArgumentNullException.ThrowIfNull(enemyDefinition);
        ArgumentNullException.ThrowIfNull(metadata);

        var battleSession = HeadlessBattleStateMachine.Start(initial, metadata);
        var plans = PlanNextWindow(
            battleSession,
            enemyDefinition,
            planBonus: initial.CounterattackState.Pending);
        var normalPlan = plans.Normal
            ?? throw new InvalidOperationException(
                "Battle start must create the first normal Bandit plan.");
        var state = BanditEnemyTurnState.Create(
            metadata.ContentHash,
            enemyDefinition,
            battleSession,
            normalPlan,
            plans.Bonus);
        var session = new BanditEnemyTurnSession(
            state,
            OrderedCommandLog.Create(metadata));
        return new BanditEnemyTurnStartResult(
            session,
            PlannedFacts(normalPlan, plans.Bonus));
    }

    public static BanditMandatoryOverridePreview PreviewMandatoryOverride(
        BanditEnemyTurnSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        var state = session.State;
        if (state.IsTerminal)
        {
            throw new InvalidOperationException(
                "A terminal battle has no enemy override preview.");
        }

        if (state.BattleState.Phase != BattlePhase.PlayerAction)
        {
            throw new InvalidOperationException(
                "Bandit override preview is available only during the player phase.");
        }

        var plan = state.NormalPlan
            ?? throw new InvalidOperationException(
                "Player phase is missing its displayed Bandit plan.");
        var runtime = state.RuntimeState;
        var decision = BanditIntentExecutor.Decide(
            PlanningContext(state.BattleState),
            state.EnemyDefinition,
            plan,
            PlanningDescriptor(runtime));
        var kind = decision.Reason switch
        {
            BanditExecutionReason.MandatoryLethalOverride =>
                BanditMandatoryOverrideKind.Lethal,
            BanditExecutionReason.MandatoryDefenseOverride =>
                BanditMandatoryOverrideKind.Defense,
            _ => BanditMandatoryOverrideKind.None,
        };
        return new BanditMandatoryOverridePreview(
            kind,
            kind == BanditMandatoryOverrideKind.None ? null : decision);
    }

    public static BanditEnemyTurnResult Execute(
        BanditEnemyTurnSession session,
        IBattleCommand command)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(command);
        if (!StringComparer.Ordinal.Equals(
                command.ExpectedStateChecksum,
                session.State.Checksum))
        {
            return Reject(session, command, "stale_state");
        }

        if (!StringComparer.Ordinal.Equals(
                command.ExpectedLogChecksum,
                session.CommandLog.CurrentChecksum))
        {
            return Reject(session, command, "stale_session");
        }

        if (session.State.IsTerminal)
        {
            return Reject(session, command, "battle_terminal");
        }

        return command switch
        {
            EndPlayerTurnCommand endPlayerTurn =>
                ExecuteEndPlayerTurn(session, endPlayerTurn),
            ResolveBanditEnemyActionCommand resolveEnemyAction =>
                ExecuteEnemyAction(session, resolveEnemyAction),
            _ => Reject(session, command, "unsupported_command"),
        };
    }

    private static BanditEnemyTurnResult ExecuteEndPlayerTurn(
        BanditEnemyTurnSession session,
        EndPlayerTurnCommand command)
    {
        var source = session.State;
        if (source.BattleState.Phase != BattlePhase.PlayerAction)
        {
            return Reject(session, command, "wrong_phase");
        }

        var inner = source.BattleSession;
        var innerCommand = new EndPlayerTurnCommand(
            inner.State.Checksum,
            inner.CommandLog.CurrentChecksum);
        var innerResult = HeadlessBattleStateMachine.Execute(inner, innerCommand);
        if (!innerResult.Accepted)
        {
            throw new InvalidOperationException(
                $"A valid Bandit player-turn boundary was rejected: {innerResult.ReasonId}.");
        }

        var stateAfter = BanditEnemyTurnState.Create(
            source.ContentHash,
            source.EnemyDefinition,
            innerResult.SessionAfter,
            source.NormalPlan,
            source.BonusPlan);
        return Accept(
            session,
            command,
            stateAfter,
            innerResult.OrderedFacts);
    }

    private static BanditEnemyTurnResult ExecuteEnemyAction(
        BanditEnemyTurnSession session,
        ResolveBanditEnemyActionCommand command)
    {
        var source = session.State;
        if (source.BattleState.Phase != BattlePhase.EnemyAction)
        {
            return Reject(session, command, "wrong_phase");
        }

        var runtime = source.RuntimeState;
        var isCounterattack = runtime.EnemyActionStage ==
            EnemyActionStage.CounterattackAction;
        var actionIndex = isCounterattack ? 2 : 1;
        var plan = isCounterattack ? source.BonusPlan : source.NormalPlan;
        if (plan is null)
        {
            throw new InvalidOperationException(
                "Active Bandit enemy stage is missing its stored plan.");
        }

        var descriptor = ExecutionDescriptor(
            runtime,
            source.BattleState.PlayerTurnIndex,
            actionIndex);
        var decision = BanditIntentExecutor.Decide(
            PlanningContext(source.BattleState),
            source.EnemyDefinition,
            plan,
            descriptor);
        var innerResult = ExecuteDecision(source.BattleSession, decision, descriptor);
        if (!innerResult.Accepted)
        {
            throw new InvalidOperationException(
                $"A Domain-authorized Bandit action was rejected: {innerResult.ReasonId}.");
        }

        var nextPlans = PlansAfterAction(
            source,
            innerResult.SessionAfter,
            isCounterattack);
        var stateAfter = BanditEnemyTurnState.Create(
            source.ContentHash,
            source.EnemyDefinition,
            innerResult.SessionAfter,
            nextPlans.Normal,
            nextPlans.Bonus);
        var facts = OrderedActionFacts(
            source.BattleState.PlayerTurnIndex,
            actionIndex,
            isCounterattack,
            plan,
            decision,
            innerResult.OrderedFacts,
            nextPlans);
        return Accept(session, command, stateAfter, facts);
    }

    private static BattleCommandResult ExecuteDecision(
        HeadlessBattleSession battleSession,
        BanditExecutionDecision decision,
        StoneRuntimePlacementDescriptor descriptor)
    {
        IBattleCommand innerCommand = decision.Kind switch
        {
            BanditExecutionDecisionKind.Placement =>
                PlacementCommand(
                    battleSession,
                    decision.Candidate
                    ?? throw new InvalidOperationException(
                        "Placement decision is missing its candidate."),
                    descriptor),
            BanditExecutionDecisionKind.Pass =>
                new ResolveEnemyPassCommand(
                    battleSession.State.Checksum,
                    battleSession.CommandLog.CurrentChecksum),
            _ => throw new InvalidOperationException("Unknown Bandit execution decision kind."),
        };
        return HeadlessBattleStateMachine.Execute(battleSession, innerCommand);
    }

    private static AuthorizedRuntimeStonePlacementCommand PlacementCommand(
        HeadlessBattleSession battleSession,
        BanditPlacementCandidate candidate,
        StoneRuntimePlacementDescriptor descriptor) =>
        new(
            battleSession.State.Checksum,
            battleSession.CommandLog.CurrentChecksum,
            StoneColor.White,
            candidate.Point,
            candidate.AccessMode,
            descriptor.InstanceId,
            descriptor.KindId,
            descriptor.OrderedEffectMetadata);

    private static PlannedPlanPair PlansAfterAction(
        BanditEnemyTurnState source,
        HeadlessBattleSession battleSessionAfter,
        bool wasCounterattack)
    {
        var battleAfter = battleSessionAfter.State;
        if (battleAfter.IsTerminal)
        {
            return PlannedPlanPair.None;
        }

        if (battleAfter.Phase == BattlePhase.EnemyAction)
        {
            if (wasCounterattack ||
                battleAfter.AuthoritativeRuntime?.EnemyActionStage !=
                    EnemyActionStage.CounterattackAction ||
                source.BonusPlan is null)
            {
                throw new InvalidOperationException(
                    "Only a completed normal action with confirmed Pending may enter the bonus stage.");
            }

            return new PlannedPlanPair(
                Normal: null,
                Bonus: source.BonusPlan,
                PlannedAfterBoundary: false);
        }

        if (battleAfter.Phase != BattlePhase.PlayerAction)
        {
            throw new InvalidOperationException(
                "Nonterminal Bandit action ended in an unsupported battle phase.");
        }

        var plans = PlanNextWindow(
            battleSessionAfter,
            source.EnemyDefinition,
            planBonus: battleAfter.AuthoritativeRuntime!.CounterattackState.Pending);
        return plans with { PlannedAfterBoundary = true };
    }

    private static PlannedPlanPair PlanNextWindow(
        HeadlessBattleSession battleSession,
        EnemyContentDefinition enemyDefinition,
        bool planBonus)
    {
        var state = battleSession.State;
        var runtime = state.AuthoritativeRuntime
            ?? throw new InvalidOperationException(
                "Bandit planning requires authoritative runtime state.");
        var context = PlanningContext(state);
        var normal = BanditIntentPlanner.Plan(
            context,
            enemyDefinition,
            state.Checksum,
            PlanningDescriptor(runtime));
        var bonus = planBonus
            ? BanditIntentPlanner.Plan(
                context,
                enemyDefinition,
                state.Checksum,
                PlanningDescriptor(runtime))
            : null;
        return new PlannedPlanPair(normal, bonus, PlannedAfterBoundary: false);
    }

    private static BanditPlanningContext PlanningContext(BattleState state)
    {
        var runtime = state.AuthoritativeRuntime
            ?? throw new InvalidOperationException(
                "Bandit planning requires authoritative runtime state.");
        return new BanditPlanningContext(
            runtime.StoneRuntimeState,
            runtime.TemporaryLibertyState,
            runtime.ContinuousLibertySnapshot,
            state.RepetitionHistory,
            state.FacilityRuntimeAnalysis);
    }

    private static StoneRuntimePlacementDescriptor PlanningDescriptor(
        BattleAuthoritativeRuntimeState runtime)
    {
        var stem =
            $"enemy_bandit.probe.{runtime.StoneRuntimeState.NextCreatedSequence.ToString(CultureInfo.InvariantCulture)}";
        var instanceId = stem;
        var collision = 0;
        while (runtime.StoneRuntimeState.InstanceById(instanceId) is not null)
        {
            collision++;
            instanceId = $"{stem}.{collision.ToString(CultureInfo.InvariantCulture)}";
        }

        return new StoneRuntimePlacementDescriptor(
            instanceId,
            StandardStoneKindId,
            []);
    }

    private static StoneRuntimePlacementDescriptor ExecutionDescriptor(
        BattleAuthoritativeRuntimeState runtime,
        int enemyTurnIndex,
        int actionIndex)
    {
        var stem =
            $"enemy_bandit.turn.{enemyTurnIndex.ToString(CultureInfo.InvariantCulture)}" +
            $".action.{actionIndex.ToString(CultureInfo.InvariantCulture)}" +
            $".stone.{runtime.StoneRuntimeState.NextCreatedSequence.ToString(CultureInfo.InvariantCulture)}";
        var instanceId = stem;
        var collision = 0;
        while (runtime.HasUsedStoneInstanceId(instanceId))
        {
            collision++;
            instanceId = $"{stem}.{collision.ToString(CultureInfo.InvariantCulture)}";
        }

        return new StoneRuntimePlacementDescriptor(
            instanceId,
            StandardStoneKindId,
            []);
    }

    private static IReadOnlyList<IBattleFact> PlannedFacts(
        PlannedEnemyIntent normal,
        PlannedEnemyIntent? bonus)
    {
        var facts = new List<IBattleFact>
        {
            new EnemyIntentPlannedFact(normal, isCounterattackAction: false),
        };
        if (bonus is not null)
        {
            facts.Add(new EnemyIntentPlannedFact(
                bonus,
                isCounterattackAction: true));
        }

        return facts;
    }

    private static IReadOnlyList<IBattleFact> OrderedActionFacts(
        int enemyTurnIndex,
        int actionIndex,
        bool isCounterattack,
        PlannedEnemyIntent plan,
        BanditExecutionDecision decision,
        IReadOnlyList<IBattleFact> innerFacts,
        PlannedPlanPair nextPlans)
    {
        var facts = new List<IBattleFact>();
        if (decision.Retargeted)
        {
            facts.Add(new EnemyIntentRetargetedFact(decision));
        }

        facts.Add(new EnemyActionStartedFact(
            enemyTurnIndex,
            actionIndex,
            isCounterattack,
            plan));
        facts.AddRange(innerFacts);
        var resolved = new EnemyActionResolvedFact(
            enemyTurnIndex,
            actionIndex,
            isCounterattack,
            decision);
        facts.Insert(ActionResolutionInsertionIndex(facts), resolved);

        if (nextPlans.PlannedAfterBoundary)
        {
            var normal = nextPlans.Normal
                ?? throw new InvalidOperationException(
                    "A completed nonterminal enemy boundary must create its next normal plan.");
            facts.AddRange(PlannedFacts(normal, nextPlans.Bonus));
        }

        return facts;
    }

    private static int ActionResolutionInsertionIndex(IReadOnlyList<IBattleFact> facts)
    {
        for (var index = 0; index < facts.Count; index++)
        {
            if (facts[index] is EnemyTurnBoundaryStageFact stage &&
                stage.Stage >= EnemyTurnBoundaryStage.ConsumeCurrentPendingAndReprimeOverflow)
            {
                return index;
            }

            if (facts[index] is BattleEndedFact)
            {
                return index;
            }
        }

        return facts.Count;
    }

    private static BanditEnemyTurnResult Accept(
        BanditEnemyTurnSession session,
        IBattleCommand command,
        BanditEnemyTurnState stateAfter,
        IEnumerable<IBattleFact> orderedFacts)
    {
        var nextLog = session.CommandLog.Append(command, stateAfter.Checksum);
        var nextSession = new BanditEnemyTurnSession(stateAfter, nextLog);
        return new BanditEnemyTurnResult(
            session,
            nextSession,
            command,
            true,
            "accepted",
            orderedFacts);
    }

    private static BanditEnemyTurnResult Reject(
        BanditEnemyTurnSession session,
        IBattleCommand command,
        string reasonId) =>
        new(
            session,
            session,
            command,
            false,
            reasonId,
            [new CommandRejectedFact(reasonId)]);

    private sealed record PlannedPlanPair(
        PlannedEnemyIntent? Normal,
        PlannedEnemyIntent? Bonus,
        bool PlannedAfterBoundary)
    {
        internal static PlannedPlanPair None { get; } = new(null, null, false);
    }
}
