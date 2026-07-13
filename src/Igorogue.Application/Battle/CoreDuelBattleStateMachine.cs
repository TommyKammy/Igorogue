using Igorogue.Application.Replay;
using Igorogue.Domain.Cards;
using Igorogue.Domain.Combat;
using Igorogue.Domain.Content;
using Igorogue.Domain.Determinism;

namespace Igorogue.Application.Battle;

public sealed class RestartBattleCommand : IBattleCommand
{
    public RestartBattleCommand(
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

    public string CommandType => "battle.restart";

    public int CommandSchemaVersion => 1;

    public string ExpectedStateChecksum { get; }

    public string ExpectedLogChecksum { get; }

    public string ToCanonicalPayload() =>
        "restart-battle-v1\n" +
        $"expected_state_checksum={ExpectedStateChecksum}\n" +
        $"expected_log_checksum={ExpectedLogChecksum}\n";
}

public static class CoreDuelBattleStateMachine
{
    public static CoreDuelBattleStartResult Start(
        BattleAuthoritativeInitialSnapshot initial,
        CoreDuelContentCatalog catalog,
        ReplayMetadata metadata)
    {
        var bootstrap = CoreDuelBattleBootstrap.Create(initial, catalog, metadata);
        var fresh = CreateFreshState(bootstrap, restartCount: 0);
        return new CoreDuelBattleStartResult(
            new CoreDuelBattleSession(
                fresh.State,
                OrderedCommandLog.Create(metadata)),
            fresh.OrderedFacts);
    }

    public static BanditMandatoryOverridePreview PreviewMandatoryOverride(
        CoreDuelBattleSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        return BanditEnemyTurnStateMachine.PreviewMandatoryOverride(
            CoreDuelBattleProjectionAdapter.CreateTemporaryBanditSession(session.State));
    }

    public static CoreDuelBattleCommandResult Execute(
        CoreDuelBattleSession session,
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

        if (command is RestartBattleCommand restart)
        {
            return ExecuteRestart(session, restart);
        }

        if (session.State.IsTerminal)
        {
            return Reject(session, command, "battle_terminal");
        }

        return command switch
        {
            PlayCardCommand playCard => ExecutePlayCard(session, playCard),
            EndPlayerTurnCommand endPlayerTurn =>
                ExecuteEndPlayerTurn(session, endPlayerTurn),
            ResolveBanditEnemyActionCommand resolveBandit =>
                ExecuteBanditEnemyAction(session, resolveBandit),
            _ => Reject(session, command, "unsupported_command"),
        };
    }

    private static CoreDuelBattleCommandResult ExecutePlayCard(
        CoreDuelBattleSession session,
        PlayCardCommand command)
    {
        if (session.State.BattleState.Phase != BattlePhase.PlayerAction)
        {
            return Reject(session, command, "wrong_phase");
        }

        var temporary = CoreDuelBattleProjectionAdapter.CreateTemporaryCardSession(
            session.State);
        var innerCommand = command.PlacementMode is StoneCardPlacementMode mode
            ? new PlayCardCommand(
                temporary.State.Checksum,
                temporary.CommandLog.CurrentChecksum,
                command.CardInstanceId,
                command.Target,
                mode)
            : new PlayCardCommand(
                temporary.State.Checksum,
                temporary.CommandLog.CurrentChecksum,
                command.CardInstanceId,
                command.Target);
        var innerResult = CoreDuelCardPlayStateMachine.Execute(
            temporary,
            innerCommand);
        if (!innerResult.Accepted)
        {
            return Reject(session, command, innerResult.ReasonId);
        }

        var cardStateAfter = innerResult.SessionAfter.State;
        var battleAfter = CoreDuelBattleProjectionAdapter.AttachCardBattle(cardStateAfter);
        var terminal = battleAfter.IsTerminal;
        var stateAfter = CoreDuelBattleState.Create(
            session.State.Bootstrap,
            battleAfter,
            cardStateAfter.CardTurnState,
            terminal ? null : session.State.NormalPlan,
            terminal ? null : session.State.BonusPlan,
            session.State.RestartCount);
        return Accept(session, command, stateAfter, innerResult.OrderedFacts);
    }

    private static CoreDuelBattleCommandResult ExecuteEndPlayerTurn(
        CoreDuelBattleSession session,
        EndPlayerTurnCommand command)
    {
        var source = session.State;
        if (source.BattleState.Phase != BattlePhase.PlayerAction)
        {
            return Reject(session, command, "wrong_phase");
        }

        var cardEnd = CoreDuelCardTurnKernel.EndPlayerTurn(source.CardTurnState);
        if (cardEnd.IsExactNoOp &&
            StringComparer.Ordinal.Equals(
                cardEnd.ReasonId,
                "active_resolution_exists"))
        {
            return Reject(session, command, cardEnd.ReasonId);
        }

        var cardAfter = cardEnd.IsExactNoOp
            ? source.CardTurnState
            : cardEnd.StateAfter;
        var battleBeforeBoundary =
            CoreDuelBattleProjectionAdapter.SynchronizeBattleWithCardTurn(
                source.BattleState,
                cardAfter);
        var boundarySource = CoreDuelBattleState.Create(
            source.Bootstrap,
            battleBeforeBoundary,
            cardAfter,
            source.NormalPlan,
            source.BonusPlan,
            source.RestartCount);
        var temporary = CoreDuelBattleProjectionAdapter.CreateTemporaryBanditSession(
            boundarySource);
        var innerResult = BanditEnemyTurnStateMachine.Execute(
            temporary,
            new EndPlayerTurnCommand(
                temporary.State.Checksum,
                temporary.CommandLog.CurrentChecksum));
        if (!innerResult.Accepted)
        {
            throw new InvalidOperationException(
                $"A validated Core Duel turn boundary was rejected: {innerResult.ReasonId}.");
        }

        var banditAfter = innerResult.SessionAfter.State;
        var stateAfter = CoreDuelBattleState.Create(
            source.Bootstrap,
            banditAfter.BattleState,
            cardAfter,
            banditAfter.NormalPlan,
            banditAfter.BonusPlan,
            source.RestartCount);
        return Accept(session, command, stateAfter, innerResult.OrderedFacts);
    }

    private static CoreDuelBattleCommandResult ExecuteBanditEnemyAction(
        CoreDuelBattleSession session,
        ResolveBanditEnemyActionCommand command)
    {
        var source = session.State;
        if (source.BattleState.Phase != BattlePhase.EnemyAction)
        {
            return Reject(session, command, "wrong_phase");
        }

        var temporary = CoreDuelBattleProjectionAdapter.CreateTemporaryBanditSession(source);
        var innerResult = BanditEnemyTurnStateMachine.Execute(
            temporary,
            new ResolveBanditEnemyActionCommand(
                temporary.State.Checksum,
                temporary.CommandLog.CurrentChecksum));
        if (!innerResult.Accepted)
        {
            throw new InvalidOperationException(
                $"A validated Core Duel Bandit action was rejected: {innerResult.ReasonId}.");
        }

        var banditAfter = innerResult.SessionAfter.State;
        var battleAfter = banditAfter.BattleState;
        var cardAfter = CoreDuelBattleProjectionAdapter.SynchronizeCardTurnResources(
            source.CardTurnState,
            banditAfter.RuntimeState);
        if (!battleAfter.IsTerminal && battleAfter.Phase == BattlePhase.PlayerAction)
        {
            if (cardAfter.ClosedWindowResources.DeferredPlayerChoices.Count != 0)
            {
                throw new InvalidOperationException(
                    "M2 Core Duel cannot auto-resolve a deferred player choice at turn start.");
            }

            var playerTurn = CoreDuelCardTurnKernel.StartPlayerTurn(
                cardAfter,
                battleAfter.Board,
                battleAfter.FacilityState,
                battleAfter.RuntimePolicy.FacilityPolicy,
                []);
            if (!playerTurn.Accepted)
            {
                throw new InvalidOperationException(
                    $"A completed enemy boundary could not start the next player turn: {playerTurn.ReasonId}.");
            }

            cardAfter = playerTurn.StateAfter;
            battleAfter = CoreDuelBattleProjectionAdapter.SynchronizeBattleWithCardTurn(
                battleAfter,
                cardAfter);
        }

        var stateAfter = CoreDuelBattleState.Create(
            source.Bootstrap,
            battleAfter,
            cardAfter,
            banditAfter.NormalPlan,
            banditAfter.BonusPlan,
            source.RestartCount);
        return Accept(session, command, stateAfter, innerResult.OrderedFacts);
    }

    private static CoreDuelBattleCommandResult ExecuteRestart(
        CoreDuelBattleSession session,
        RestartBattleCommand command)
    {
        if (!session.State.IsTerminal)
        {
            return Reject(session, command, "battle_not_terminal");
        }

        if (session.State.RestartCount == int.MaxValue)
        {
            return Reject(session, command, "restart_count_overflow");
        }

        var fresh = CreateFreshState(
            session.State.Bootstrap,
            checked(session.State.RestartCount + 1));
        return Accept(session, command, fresh.State, fresh.OrderedFacts);
    }

    private static FreshStateResult CreateFreshState(
        CoreDuelBattleBootstrap bootstrap,
        int restartCount)
    {
        var initial = bootstrap.InitialSnapshot;
        var initialCardTurn = CoreDuelCardTurnKernel.StartBattle(
            bootstrap.PhysicalDeckRecipe,
            AuthoritativeRngState.Create(bootstrap.Metadata.InitialSeed),
            bootstrap.Catalog.SystemPolicy,
            initial.ClosedWindowResources,
            []);

        var preTurnBattle = BattleState.Start(initial, initialCardTurn.RngState);
        var preTurnHeadless =
            CoreDuelBattleProjectionAdapter.CreateTemporaryHeadlessSession(
                preTurnBattle,
                bootstrap.Metadata);
        var initialPlanning = BanditEnemyTurnStateMachine.StartFromExistingBattle(
            preTurnHeadless,
            bootstrap.Catalog.Bandit);

        var playerTurn = CoreDuelCardTurnKernel.StartPlayerTurn(
            initialCardTurn,
            initial.Board,
            initial.FacilityState,
            initial.RuntimePolicy.FacilityPolicy,
            []);
        if (!playerTurn.Accepted)
        {
            throw new InvalidOperationException(
                $"Fresh Core Duel could not start its first player turn: {playerTurn.ReasonId}.");
        }

        var cardTurn = playerTurn.StateAfter;
        var battle = CoreDuelBattleProjectionAdapter.SynchronizeBattleWithCardTurn(
            preTurnBattle,
            cardTurn);
        var planned = initialPlanning.Session.State;
        return new FreshStateResult(
            CoreDuelBattleState.Create(
                bootstrap,
                battle,
                cardTurn,
                planned.NormalPlan,
                planned.BonusPlan,
                restartCount),
            initialPlanning.OrderedFacts);
    }

    private static CoreDuelBattleCommandResult Accept(
        CoreDuelBattleSession session,
        IBattleCommand command,
        CoreDuelBattleState stateAfter,
        IEnumerable<IBattleFact> orderedFacts)
    {
        var nextLog = session.CommandLog.Append(command, stateAfter.Checksum);
        return new CoreDuelBattleCommandResult(
            session,
            new CoreDuelBattleSession(stateAfter, nextLog),
            command,
            true,
            "accepted",
            orderedFacts);
    }

    private static CoreDuelBattleCommandResult Reject(
        CoreDuelBattleSession session,
        IBattleCommand command,
        string reasonId) =>
        new(
            session,
            session,
            command,
            false,
            reasonId,
            [new CommandRejectedFact(reasonId)]);

    private sealed record FreshStateResult(
        CoreDuelBattleState State,
        IReadOnlyList<IBattleFact> OrderedFacts);
}
