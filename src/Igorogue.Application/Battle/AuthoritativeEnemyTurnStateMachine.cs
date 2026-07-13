using Igorogue.Domain.Board;
using Igorogue.Domain.Combat;
using Igorogue.Domain.Facilities;

namespace Igorogue.Application.Battle;

internal static class AuthoritativeEnemyTurnStateMachine
{
    private const string PlacementCaptureReasonId = "stone_placement";

    internal static BattleCommandResult Execute(
        HeadlessBattleSession session,
        IBattleCommand command) => command switch
    {
        AuthorizedRuntimeStonePlacementCommand placement =>
            ExecutePlacement(session, placement),
        AuthorizedStonePlacementCommand =>
            HeadlessBattleStateMachine.Reject(
                session,
                command,
                "runtime_descriptor_required"),
        AuthorizedFacilityBuildCommand =>
            HeadlessBattleStateMachine.Reject(
                session,
                command,
                "unsupported_command"),
        EndPlayerTurnCommand endPlayerTurn =>
            ExecuteEndPlayerTurn(session, endPlayerTurn),
        ResolveEnemyPassCommand enemyPass =>
            ExecuteEnemyPass(session, enemyPass),
        _ => HeadlessBattleStateMachine.Reject(
            session,
            command,
            "unsupported_command"),
    };

    private static BattleCommandResult ExecuteEndPlayerTurn(
        HeadlessBattleSession session,
        EndPlayerTurnCommand command)
    {
        var source = session.State;
        if (source.Phase != BattlePhase.PlayerAction)
        {
            return HeadlessBattleStateMachine.Reject(
                session,
                command,
                "wrong_phase");
        }

        var runtime = RequiredRuntime(source);
        var pendingAtStart = CounterattackBoundaryResolver
            .SnapshotPendingAtEnemyTurnStart(
                runtime.CounterattackState,
                runtime.CounterattackPolicy);
        var runtimeAfter = runtime.Transition(
            enemyActionStage: EnemyActionStage.NormalAction,
            pendingAtEnemyTurnStart: pendingAtStart);
        var stateAfter = Transition(
            source,
            runtimeAfter,
            source.Board,
            source.RepetitionHistory,
            source.FacilityState,
            source.TerritoryAnalysis,
            source.FacilityRuntimeAnalysis,
            source.PlayerTurnIndex,
            BattlePhase.EnemyAction,
            BattleOutcome.Ongoing,
            BattleEndReason.None);
        return HeadlessBattleStateMachine.Accept(session, command, stateAfter, []);
    }

    private static BattleCommandResult ExecuteEnemyPass(
        HeadlessBattleSession session,
        ResolveEnemyPassCommand command)
    {
        var source = session.State;
        if (source.Phase != BattlePhase.EnemyAction)
        {
            return HeadlessBattleStateMachine.Reject(
                session,
                command,
                "wrong_phase");
        }

        var facts = new List<IBattleFact>
        {
            ActionStageFact(source),
            new EnemyPassedFact(source.PlayerTurnIndex),
        };
        var stateAfter = CompleteAcceptedEnemyAction(source, facts);
        return HeadlessBattleStateMachine.Accept(
            session,
            command,
            stateAfter,
            facts);
    }

    private static BattleCommandResult ExecutePlacement(
        HeadlessBattleSession session,
        AuthorizedRuntimeStonePlacementCommand command)
    {
        var source = session.State;
        if (source.Phase != BattlePhase.EnemyAction)
        {
            return HeadlessBattleStateMachine.Reject(
                session,
                command,
                "wrong_phase");
        }

        var runtime = RequiredRuntime(source);
        var preparation = AuthorizedRuntimeStonePlacementPipeline.Resolve(
            source,
            runtime,
            command.Actor,
            command.Point,
            command.AccessMode,
            command.PlacementDescriptor);
        if (!preparation.Accepted)
        {
            return HeadlessBattleStateMachine.Reject(
                session,
                command,
                preparation.ReasonId);
        }

        var legalPlacement = preparation.LegalPlacementCommit
            ?? throw new InvalidOperationException(
                "Accepted runtime placement preparation is missing its legal commit.");
        var runtimeCommit = preparation.RuntimePlacementCommit
            ?? throw new InvalidOperationException(
                "Accepted runtime placement preparation is missing its runtime commit.");
        var facilityCommit = FacilityPlacementIntegrator.Apply(
            source.FacilityState,
            legalPlacement);
        var facts = OrderedPlacementFacts(
            source,
            facilityCommit,
            runtimeCommit);
        CaptureBatch? captureBatch = null;
        ClosedWindowCaptureBenefitResolution? benefits = null;
        if (legalPlacement.Candidate.CapturedGroups.Count > 0)
        {
            captureBatch = CaptureBatch.Create(
                $"enemy_placement_capture_{command.StoneInstanceId}",
                PlacementCaptureReasonId,
                CaptureBoundary.PlacementResolution,
                boundaryEnemyTurnIndex: null,
                CapturingWindow.ClosedPlayerWindow,
                preparation.CaptureEffectiveLiberties?.SourceStones
                    ?? throw new InvalidOperationException(
                        "Accepted runtime placement preparation is missing its capture snapshot."),
                legalPlacement.Candidate.CapturedGroups);
            var selectedTriggers = captureBatch.ContainsKing
                ? Array.Empty<CaptureBenefitTrigger>()
                : SelectCaptureBenefitTriggers(
                    source,
                    facilityCommit.FacilityStateAfterCommit,
                    captureBatch);
            benefits = ClosedWindowCaptureBenefitResolver.ResolvePlacement(
                captureBatch,
                runtime.ClosedWindowResources,
                runtime.CounterattackState,
                runtime.CounterattackPolicy,
                selectedTriggers);
            facts.AddRange(benefits.OrderedFacts);
        }

        var territoryAfter = TerritoryAnalyzer.Analyze(facilityCommit.BoardAfterCommit);
        var continuousAfter = runtime.ContinuousLibertySnapshot.Rebind(
            runtimeCommit.StonesAfterCommit);
        var runtimeAfterPlacement = runtime.Transition(
            stoneRuntimeState: runtimeCommit.StonesAfterCommit,
            temporaryLibertyState: runtimeCommit.TemporaryLibertiesAfterCommit,
            continuousLibertySnapshot: continuousAfter,
            closedWindowResources:
                benefits?.ResourcesAfterResolution ?? runtime.ClosedWindowResources,
            counterattackState:
                benefits?.CounterattackAfterResolution ?? runtime.CounterattackState,
            registeredStoneInstanceId: command.StoneInstanceId);

        if (facilityCommit.KingCaptureResult.IsTerminal)
        {
            if (captureBatch is null ||
                benefits is null ||
                !captureBatch.ContainsKing ||
                !benefits.BenefitsSuppressed)
            {
                throw new InvalidOperationException(
                    "Terminal placement capture must suppress its closed-window capture benefits.");
            }

            var terminal = facilityCommit.KingCaptureResult;
            var facilityRuntimeAfter = FacilityRuntimeAnalyzer.Analyze(
                facilityCommit.FacilityStateAfterCommit,
                territoryAfter,
                source.RuntimePolicy.FacilityPolicy);
            var terminalRuntime = runtimeAfterPlacement.Transition(
                clearEnemyActionBoundary: true);
            var terminalState = Transition(
                source,
                terminalRuntime,
                facilityCommit.BoardAfterCommit,
                facilityCommit.HistoryAfterCommit,
                facilityCommit.FacilityStateAfterCommit,
                territoryAfter,
                facilityRuntimeAfter,
                source.PlayerTurnIndex,
                BattlePhase.Ended,
                terminal.Outcome,
                terminal.EndReason);
            facts.Add(new BattleEndedFact(terminal.Outcome, terminal.EndReason));
            return HeadlessBattleStateMachine.Accept(
                session,
                command,
                terminalState,
                facts);
        }

        var territoryEstablished = TerritoryDeltaResolver.Resolve(
            source.TerritoryAnalysis,
            territoryAfter,
            facilityCommit,
            command.Actor);
        if (territoryEstablished is not null)
        {
            facts.Add(territoryEstablished);
        }

        var facilityTransition = FacilityOperatingTransitionResolver
            .ReassociateAfterPlacement(
                source.FacilityRuntimeAnalysis,
                facilityCommit,
                territoryAfter);
        facts.AddRange(facilityTransition.OrderedFacts.Cast<IBattleFact>());
        var stateAfterAction = Transition(
            source,
            runtimeAfterPlacement,
            facilityCommit.BoardAfterCommit,
            facilityCommit.HistoryAfterCommit,
            facilityCommit.FacilityStateAfterCommit,
            territoryAfter,
            facilityTransition.AnalysisAfter,
            source.PlayerTurnIndex,
            BattlePhase.EnemyAction,
            BattleOutcome.Ongoing,
            BattleEndReason.None);
        var finalState = CompleteAcceptedEnemyAction(stateAfterAction, facts);
        return HeadlessBattleStateMachine.Accept(
            session,
            command,
            finalState,
            facts);
    }

    private static BattleState CompleteAcceptedEnemyAction(
        BattleState stateAfterAction,
        List<IBattleFact> facts)
    {
        var runtime = RequiredRuntime(stateAfterAction);
        var pendingAtStart = runtime.PendingAtEnemyTurnStart
            ?? throw new InvalidOperationException(
                "Enemy action is missing its pending-at-start snapshot.");
        if (runtime.EnemyActionStage == EnemyActionStage.NormalAction &&
            pendingAtStart.PendingAtStart)
        {
            var counterattackStage = runtime.Transition(
                enemyActionStage: EnemyActionStage.CounterattackAction,
                pendingAtEnemyTurnStart: pendingAtStart);
            return Transition(
                stateAfterAction,
                counterattackStage,
                stateAfterAction.Board,
                stateAfterAction.RepetitionHistory,
                stateAfterAction.FacilityState,
                stateAfterAction.TerritoryAnalysis,
                stateAfterAction.FacilityRuntimeAnalysis,
                stateAfterAction.PlayerTurnIndex,
                BattlePhase.EnemyAction,
                BattleOutcome.Ongoing,
                BattleEndReason.None);
        }

        return CompleteEnemyBoundary(stateAfterAction, facts, pendingAtStart);
    }

    private static BattleState CompleteEnemyBoundary(
        BattleState source,
        List<IBattleFact> facts,
        CounterattackPendingAtStartSnapshot pendingAtStart)
    {
        var runtime = RequiredRuntime(source);
        facts.Add(StageFact(
            source.PlayerTurnIndex,
            EnemyTurnBoundaryStage.ConsumeCurrentPendingAndReprimeOverflow));
        var pendingTransition = CounterattackBoundaryResolver.ConsumeAndReprimeOnce(
            runtime.CounterattackState,
            pendingAtStart,
            runtime.CounterattackPolicy);
        facts.AddRange(pendingTransition.OrderedFacts);
        var counterattack = pendingTransition.StateAfterTransition;

        facts.Add(StageFact(
            source.PlayerTurnIndex,
            EnemyTurnBoundaryStage.TemporaryLibertyExpirySweep));
        var expiry = TemporaryLibertyExpiryResolver.Resolve(
            runtime.StoneRuntimeState,
            runtime.TemporaryLibertyState,
            runtime.ContinuousLibertySnapshot,
            source.RepetitionHistory,
            source.PlayerTurnIndex);
        var continuousAfterExpiry = ReferenceEquals(
                expiry.StonesAfterResolution,
                runtime.StoneRuntimeState)
            ? runtime.ContinuousLibertySnapshot
            : runtime.ContinuousLibertySnapshot.Rebind(expiry.StonesAfterResolution);

        if (expiry.KingCaptureResult.IsTerminal)
        {
            facts.AddRange(expiry.OrderedFacts);
            var terminalFacility = ReassociateFacilities(
                source,
                expiry,
                publishFacts: false,
                facts);
            var terminalRuntime = runtime.Transition(
                stoneRuntimeState: expiry.StonesAfterResolution,
                temporaryLibertyState: expiry.TemporaryLibertiesAfterResolution,
                continuousLibertySnapshot: continuousAfterExpiry,
                counterattackState: counterattack,
                clearEnemyActionBoundary: true);
            var terminal = expiry.KingCaptureResult;
            var terminalState = Transition(
                source,
                terminalRuntime,
                expiry.BoardAfterResolution,
                expiry.HistoryAfterResolution,
                terminalFacility.State,
                expiry.TerritoryAfterResolution,
                terminalFacility.Analysis,
                source.PlayerTurnIndex,
                BattlePhase.Ended,
                terminal.Outcome,
                terminal.EndReason);
            facts.Add(new BattleEndedFact(terminal.Outcome, terminal.EndReason));
            return terminalState;
        }

        var resolvedFact = expiry.OrderedFacts
            .OfType<TemporaryLibertyExpirySweepResolvedFact>()
            .SingleOrDefault();
        facts.AddRange(expiry.OrderedFacts.Where(fact =>
            fact is not TemporaryLibertyExpirySweepResolvedFact));
        var resources = runtime.ClosedWindowResources;
        if (expiry.CaptureBatch is not null)
        {
            var benefits = ClosedWindowCaptureBenefitResolver.Resolve(
                expiry.CaptureBatch,
                resources,
                counterattack,
                runtime.CounterattackPolicy,
                SelectCaptureBenefitTriggers(
                    source,
                    source.FacilityState,
                    expiry.CaptureBatch));
            resources = benefits.ResourcesAfterResolution;
            counterattack = benefits.CounterattackAfterResolution;
            facts.AddRange(benefits.OrderedFacts);
        }

        var territoryEstablished = TerritoryDeltaResolver.ResolveAfterExpiry(
            source.TerritoryAnalysis,
            expiry);
        if (territoryEstablished is not null)
        {
            facts.Add(territoryEstablished);
        }

        var facility = ReassociateFacilities(
            source,
            expiry,
            publishFacts: true,
            facts);
        if (resolvedFact is not null)
        {
            facts.Add(resolvedFact);
        }

        facts.Add(StageFact(
            source.PlayerTurnIndex,
            EnemyTurnBoundaryStage.EnemyTurnEndCounterattackGain));
        var natural = CounterattackBoundaryResolver.AdvanceEnemyTurnEnd(
            counterattack,
            runtime.CounterattackPolicy);
        counterattack = natural.StateAfterTransition;
        facts.AddRange(natural.OrderedFacts);
        facts.Add(StageFact(
            source.PlayerTurnIndex,
            EnemyTurnBoundaryStage.PlanNextIntents));

        var completedRuntime = runtime.Transition(
            stoneRuntimeState: expiry.StonesAfterResolution,
            temporaryLibertyState: expiry.TemporaryLibertiesAfterResolution,
            continuousLibertySnapshot: continuousAfterExpiry,
            closedWindowResources: resources,
            counterattackState: counterattack,
            clearEnemyActionBoundary: true);
        if (source.PlayerTurnIndex >= source.RuntimePolicy.PlayerTurnLimit)
        {
            facts.Add(new BattleEndedFact(
                BattleOutcome.PlayerDefeat,
                BattleEndReason.TurnLimit));
            return Transition(
                source,
                completedRuntime,
                expiry.BoardAfterResolution,
                expiry.HistoryAfterResolution,
                facility.State,
                facility.Territory,
                facility.Analysis,
                source.PlayerTurnIndex,
                BattlePhase.Ended,
                BattleOutcome.PlayerDefeat,
                BattleEndReason.TurnLimit);
        }

        return Transition(
            source,
            completedRuntime,
            expiry.BoardAfterResolution,
            expiry.HistoryAfterResolution,
            facility.State,
            facility.Territory,
            facility.Analysis,
            source.PlayerTurnIndex + 1,
            BattlePhase.PlayerAction,
            BattleOutcome.Ongoing,
            BattleEndReason.None);
    }

    private static IReadOnlyList<CaptureBenefitTrigger> SelectCaptureBenefitTriggers(
        BattleState source,
        FacilityState eligibleFacilityState,
        CaptureBatch captureBatch)
    {
        var runtime = RequiredRuntime(source);
        return runtime.CaptureBenefitTriggerPlan.SelectFor(captureBatch)
            .Where(trigger => trigger.Source.Kind != CaptureBenefitSourceKind.Facility ||
                IsEligibleFacilityTrigger(
                    source,
                    eligibleFacilityState,
                    trigger.Source))
            .ToArray();
    }

    private static bool IsEligibleFacilityTrigger(
        BattleState source,
        FacilityState eligibleFacilityState,
        CaptureBenefitSource triggerSource)
    {
        var sourceFacility = source.FacilityState.FacilityById(triggerSource.SourceId);
        var eligibleFacility = eligibleFacilityState.FacilityById(triggerSource.SourceId);
        if (sourceFacility is null ||
            !ReferenceEquals(sourceFacility, eligibleFacility) ||
            triggerSource.FacilityPoint is null ||
            !sourceFacility.Point.Equals(triggerSource.FacilityPoint))
        {
            return false;
        }

        return source.FacilityRuntimeAnalysis
            .OperatingStateFor(sourceFacility)
            .IsActive;
    }

    private static List<IBattleFact> OrderedPlacementFacts(
        BattleState source,
        FacilityPlacementCommit facilityCommit,
        StoneRuntimePlacementCommit runtimeCommit)
    {
        var facts = new List<IBattleFact> { ActionStageFact(source) };
        facts.AddRange(facilityCommit.OrderedFacts.Cast<IBattleFact>());
        if (runtimeCommit.OrderedRemovalFacts.Count == 0)
        {
            return facts;
        }

        var insertionIndex = facts.FindLastIndex(fact => fact is GroupCapturedFact);
        if (insertionIndex < 0)
        {
            throw new InvalidOperationException(
                "Carrier removal facts require a captured placement group.");
        }

        facts.InsertRange(
            insertionIndex + 1,
            runtimeCommit.OrderedRemovalFacts.Cast<IBattleFact>());
        return facts;
    }

    private static FacilityBoundaryResult ReassociateFacilities(
        BattleState source,
        TemporaryLibertyExpiryResolution expiry,
        bool publishFacts,
        List<IBattleFact> facts)
    {
        if (ReferenceEquals(expiry.BoardAfterResolution, source.Board))
        {
            return new FacilityBoundaryResult(
                source.FacilityState,
                source.TerritoryAnalysis,
                source.FacilityRuntimeAnalysis);
        }

        var transition = FacilityOperatingTransitionResolver.Reassociate(
            source.FacilityRuntimeAnalysis,
            expiry.TerritoryAfterResolution);
        if (publishFacts)
        {
            facts.AddRange(transition.OrderedFacts.Cast<IBattleFact>());
        }

        return new FacilityBoundaryResult(
            transition.StateAfterTransition,
            expiry.TerritoryAfterResolution,
            transition.AnalysisAfter);
    }

    private static EnemyTurnBoundaryStageFact ActionStageFact(BattleState state)
    {
        var stage = RequiredRuntime(state).EnemyActionStage switch
        {
            EnemyActionStage.NormalAction =>
                EnemyTurnBoundaryStage.EnemyNormalAction,
            EnemyActionStage.CounterattackAction =>
                EnemyTurnBoundaryStage.EnemyCounterattackAction,
            _ => throw new InvalidOperationException(
                "Enemy action phase is missing its authoritative stage."),
        };
        return StageFact(state.PlayerTurnIndex, stage);
    }

    private static EnemyTurnBoundaryStageFact StageFact(
        int enemyTurnIndex,
        EnemyTurnBoundaryStage stage) =>
        new(enemyTurnIndex, stage);

    private static BattleAuthoritativeRuntimeState RequiredRuntime(BattleState state) =>
        state.AuthoritativeRuntime
        ?? throw new InvalidOperationException(
            "Authoritative state machine requires authoritative runtime state.");

    private static BattleState Transition(
        BattleState source,
        BattleAuthoritativeRuntimeState runtime,
        BoardState board,
        BattleRepetitionHistory history,
        FacilityState facilities,
        TerritoryAnalysis territory,
        FacilityRuntimeAnalysis facilityRuntime,
        int playerTurnIndex,
        BattlePhase phase,
        BattleOutcome outcome,
        BattleEndReason reason) =>
        BattleState.TransitionAuthoritative(
            source,
            board,
            history,
            facilities,
            territory,
            facilityRuntime,
            runtime,
            playerTurnIndex,
            phase,
            outcome,
            reason);

    private sealed record FacilityBoundaryResult(
        FacilityState State,
        TerritoryAnalysis Territory,
        FacilityRuntimeAnalysis Analysis);
}
