using System.Globalization;

using Igorogue.Application.Battle;
using Igorogue.Application.Replay;
using Igorogue.Domain.Board;
using Igorogue.Domain.Cards;
using Igorogue.Domain.Combat;
using Igorogue.Domain.Enemies;
using Igorogue.Domain.Facilities;

namespace Igorogue.Application.Tests;

public sealed class CoreDuelPreviewQueryTests
{
    [Fact]
    public void EveryResolvedStarterCardShapeUsesTheAuthoritativeCandidateCommandPath()
    {
        var cardShapes = new[]
        {
            (ContentId: "card_basic_stone", IsStone: true),
            (ContentId: "card_extend", IsStone: true),
            (ContentId: "card_contact", IsStone: true),
            (ContentId: "card_lure_stone", IsStone: true),
            (ContentId: "card_reinforce", IsStone: false),
            (ContentId: "card_development", IsStone: false),
        };

        foreach (var shape in cardShapes)
        {
            var session = StartWithCardInHand(shape.ContentId);
            var card = session.State.CardTurnState.Deck.Hand.First(candidate =>
                StringComparer.Ordinal.Equals(candidate.ContentId, shape.ContentId));
            var preview = Query(session, card.InstanceId);

            Assert.True(preview.Accepted, preview.ReasonId);
            Assert.Equal(shape.ContentId, preview.CardContentId);
            Assert.Equal(shape.IsStone ? 49 * 3 : 49, preview.Candidates.Count);
            Assert.All(
                preview.Candidates,
                candidate => Assert.Equal(card.InstanceId, candidate.CommitCommand.CardInstanceId));
            foreach (var candidate in preview.Candidates.Take(5))
            {
                var direct = CoreDuelBattleStateMachine.Execute(
                    session,
                    candidate.CommitCommand);
                Assert.Equal(candidate.Accepted, direct.Accepted);
                Assert.Equal(candidate.ReasonId, direct.ReasonId);
            }
        }
    }

    [Fact]
    public void BattleProjectionKeepsIntentAvailableAcrossPlayerEnemyAndTerminalPhases()
    {
        var session = CoreDuelBattleTestFixture.Start().Session;
        var plan = Assert.IsType<PlannedEnemyIntent>(session.State.NormalPlan);

        var player = QueryBattle(session);

        Assert.True(player.Accepted, player.ReasonId);
        Assert.Equal("player_action", player.PhaseId);
        Assert.False(player.IsTerminal);
        Assert.Equal(session.State.BattleState.PlayerTurnIndex, player.PlayerTurnIndex);
        Assert.Equal(session.State.RestartCount, player.RestartCount);
        AssertIntentMatches(
            Assert.IsType<CoreDuelIntentPreview>(player.DisplayedIntent),
            plan,
            isCounterattackAction: false);
        AssertCurrentTargetMatches(
            session.State,
            plan,
            Assert.IsType<CoreDuelIntentPreview>(player.DisplayedIntent));

        var endTurn = CoreDuelBattleStateMachine.Execute(
            session,
            new EndPlayerTurnCommand(
                session.State.Checksum,
                session.CommandLog.CurrentChecksum));
        Assert.True(endTurn.Accepted, endTurn.ReasonId);
        session = endTurn.SessionAfter;

        var enemy = QueryBattle(session);

        Assert.True(enemy.Accepted, enemy.ReasonId);
        Assert.Equal("enemy_action", enemy.PhaseId);
        Assert.False(enemy.IsTerminal);
        AssertIntentMatches(
            Assert.IsType<CoreDuelIntentPreview>(enemy.DisplayedIntent),
            plan,
            isCounterattackAction: false);
        Assert.Equal("none", enemy.MandatoryOverride!.KindId);

        var victory = StartVictorySession();
        var terminalCandidate = Candidate(
            Query(victory, BasicCard(victory).InstanceId),
            3,
            3,
            "terminal_capture");
        var terminalCommit = CoreDuelBattleStateMachine.Execute(
            victory,
            terminalCandidate.CommitCommand);
        Assert.True(terminalCommit.Accepted, terminalCommit.ReasonId);

        var terminal = QueryBattle(terminalCommit.SessionAfter);

        Assert.True(terminal.Accepted, terminal.ReasonId);
        Assert.True(terminal.IsTerminal);
        Assert.Equal("ended", terminal.PhaseId);
        Assert.Equal("win", terminal.OutcomeId);
        Assert.Equal("white_king_captured", terminal.EndReasonId);
        Assert.Null(terminal.NormalIntent);
        Assert.Null(terminal.BonusIntent);
        Assert.Null(terminal.DisplayedIntent);
        Assert.Equal("none", terminal.MandatoryOverride!.KindId);
        Assert.False(terminal.BlackKingRisk!.IsCaptured);
    }

    [Fact]
    public void BattleProjectionCarriesTheCanonicalPresentationSnapshot()
    {
        var session = CoreDuelBattleTestFixture.Start().Session;
        var state = session.State;
        var deck = state.CardTurnState.Deck;

        var preview = QueryBattle(session);

        Assert.True(preview.Accepted, preview.ReasonId);
        Assert.Equal(state.CardTurnState.Qi, preview.Qi);
        Assert.Equal(deck.DrawPile.Count, preview.DrawPileCount);
        Assert.Equal(deck.Resolving.Count, preview.ResolvingCardCount);
        Assert.Equal(deck.DiscardPile.Count, preview.DiscardPileCount);
        Assert.Equal(deck.ExhaustPile.Count, preview.ExhaustPileCount);
        Assert.Equal(
            deck.Hand.Select(card => (card.InstanceId, card.ContentId)),
            preview.HandCards.Select(card => (card.InstanceId, card.ContentId)));

        var expectedPoints = state.BattleState.Board.Geometry.CanonicalPoints;
        Assert.Equal(expectedPoints, preview.BoardPoints.Select(point => point.Point));
        Assert.Equal(BoardGeometry.AcceptedSize * BoardGeometry.AcceptedSize, preview.BoardPoints.Count);
        foreach (var projected in preview.BoardPoints)
        {
            var stone = state.RuntimeState.StoneRuntimeState.InstanceAt(projected.Point);
            if (stone is null)
            {
                Assert.Null(projected.Stone);
            }
            else
            {
                var projectedStone = Assert.IsType<CoreDuelStonePreview>(projected.Stone);
                Assert.Equal(stone.InstanceId, projectedStone.InstanceId);
                Assert.Equal(stone.KindId, projectedStone.KindId);
                Assert.Equal(ColorId(stone.Color), projectedStone.ColorId);
                Assert.Equal(stone.IsKing, projectedStone.IsKing);
            }

            Assert.Equal(
                TerritoryOwnerId(
                    state.BattleState.TerritoryAnalysis.RegionAt(projected.Point)?.Owner),
                projected.TerritoryOwnerId);
            Assert.Null(projected.Facility);
        }

        AssertAllGroupsMatch(state, preview.Groups);
    }

    [Fact]
    public void BattleProjectionSelectsTheStoredBonusIntentOnlyWhenItsActionBecomesActive()
    {
        var session = StartPendingCounterattackSession();
        var normalPlan = Assert.IsType<PlannedEnemyIntent>(session.State.NormalPlan);
        var bonusPlan = Assert.IsType<PlannedEnemyIntent>(session.State.BonusPlan);

        var player = QueryBattle(session);

        AssertIntentMatches(
            Assert.IsType<CoreDuelIntentPreview>(player.NormalIntent),
            normalPlan,
            isCounterattackAction: false);
        AssertIntentMatches(
            Assert.IsType<CoreDuelIntentPreview>(player.BonusIntent),
            bonusPlan,
            isCounterattackAction: true);
        Assert.Equal(normalPlan.Checksum, player.DisplayedIntent!.PlanChecksum);

        var endTurn = CoreDuelBattleStateMachine.Execute(
            session,
            new EndPlayerTurnCommand(
                session.State.Checksum,
                session.CommandLog.CurrentChecksum));
        Assert.True(endTurn.Accepted, endTurn.ReasonId);
        session = endTurn.SessionAfter;
        Assert.Equal(normalPlan.Checksum, QueryBattle(session).DisplayedIntent!.PlanChecksum);

        var normalAction = CoreDuelBattleStateMachine.Execute(
            session,
            new ResolveBanditEnemyActionCommand(
                session.State.Checksum,
                session.CommandLog.CurrentChecksum));
        Assert.True(normalAction.Accepted, normalAction.ReasonId);
        Assert.False(normalAction.SessionAfter.State.IsTerminal);
        session = normalAction.SessionAfter;

        var bonus = QueryBattle(session);

        Assert.Equal("enemy_action", bonus.PhaseId);
        AssertIntentMatches(
            Assert.IsType<CoreDuelIntentPreview>(bonus.DisplayedIntent),
            bonusPlan,
            isCounterattackAction: true);
    }

    [Fact]
    public void StandardStoneCandidatesReuseCommandReasonsAndCanonicalOrder()
    {
        var session = CoreDuelBattleTestFixture.Start().Session;
        var card = BasicCard(session);

        var preview = Query(session, card.InstanceId);

        Assert.True(preview.Accepted, preview.ReasonId);
        Assert.Equal("accepted", preview.ReasonId);
        Assert.Equal(card.ContentId, preview.CardContentId);
        Assert.Equal(49 * 3, preview.Candidates.Count);
        var expectedOrder = session.State.BattleState.Board.Geometry.CanonicalPoints
            .SelectMany(point => new[]
            {
                (point.X, point.Y, Mode: "frontline"),
                (point.X, point.Y, Mode: "contact"),
                (point.X, point.Y, Mode: "terminal_capture"),
            })
            .ToArray();
        Assert.Equal(
            expectedOrder,
            preview.Candidates
                .Select(candidate =>
                    (candidate.Target.X, candidate.Target.Y, Mode: candidate.PlacementModeId))
                .ToArray());

        var accepted = Candidate(preview, 1, 2, "frontline");
        var occupied = Candidate(preview, 2, 2, "frontline");
        var detached = Candidate(preview, 4, 4, "frontline");
        var unsupported = Candidate(preview, 1, 2, "contact");
        var nonCapturingTerminal = Candidate(preview, 4, 4, "terminal_capture");

        Assert.True(accepted.Accepted, accepted.ReasonId);
        Assert.Equal("accepted", accepted.ReasonId);
        Assert.False(occupied.Accepted);
        Assert.Equal("target_occupied", occupied.ReasonId);
        Assert.False(detached.Accepted);
        Assert.Equal("frontline_adjacency_required", detached.ReasonId);
        Assert.False(unsupported.Accepted);
        Assert.Equal("unsupported_placement_mode", unsupported.ReasonId);
        Assert.False(nonCapturingTerminal.Accepted);
        Assert.Equal("terminal_capture_required", nonCapturingTerminal.ReasonId);

        foreach (var candidate in new[]
                 {
                     accepted,
                     occupied,
                     detached,
                     unsupported,
                     nonCapturingTerminal,
                 })
        {
            var direct = CoreDuelBattleStateMachine.Execute(
                session,
                candidate.CommitCommand);
            Assert.Equal(candidate.Accepted, direct.Accepted);
            Assert.Equal(candidate.ReasonId, direct.ReasonId);
        }
    }

    [Fact]
    public void AcceptedPreviewMatchesCommittedChecksumsFactsAndProjection()
    {
        var session = CoreDuelBattleTestFixture.Start().Session;
        var preview = Query(session, BasicCard(session).InstanceId);
        var candidate = Candidate(preview, 1, 2, "frontline");

        var committed = CoreDuelBattleStateMachine.Execute(
            session,
            candidate.CommitCommand);

        Assert.True(committed.Accepted, committed.ReasonId);
        var accepted = Assert.IsType<CoreDuelAcceptedCardPlayPreview>(
            candidate.AcceptedResult);
        Assert.Equal(committed.StateChecksum, accepted.StateChecksum);
        Assert.Equal(committed.LogChecksum, accepted.LogChecksum);
        Assert.Equal(committed.SessionAfter.State.IsTerminal, accepted.IsTerminal);
        Assert.Equal(
            committed.SessionAfter.State.BattleState.OutcomeId,
            accepted.OutcomeId);
        Assert.Equal(
            committed.SessionAfter.State.BattleState.EndReasonId,
            accepted.EndReasonId);
        AssertCapturedGroupsMatch(committed, accepted);
        AssertTerritoryDeltaMatches(session, committed.SessionAfter, accepted);
        AssertResultingTargetGroupMatches(
            committed.SessionAfter.State,
            candidate.Target,
            accepted.ResultingTargetGroup);
        AssertAllGroupsMatch(
            committed.SessionAfter.State,
            accepted.ResultingGroups);

        var displayed = committed.SessionAfter.State.DisplayedPlan;
        Assert.NotNull(displayed);
        AssertIntentMatches(
            Assert.IsType<CoreDuelIntentPreview>(accepted.DisplayedIntent),
            displayed,
            isCounterattackAction: false);
        AssertMandatoryOverrideMatches(
            committed.SessionAfter,
            accepted.MandatoryOverride);
        Assert.False(accepted.BlackKingRisk.IsCaptured);
        Assert.NotNull(accepted.BlackKingRisk.Group);
    }

    [Fact]
    public void EvaluationPreservesEveryAuthoritativeReferenceRngLogAndFirstUseFlag()
    {
        var session = CoreDuelBattleTestFixture.Start().Session;
        var state = session.State;
        var battle = state.BattleState;
        var runtime = state.RuntimeState;
        var cardTurn = state.CardTurnState;
        var commandLog = session.CommandLog;
        var logEntries = commandLog.Entries;
        var closedWindow = runtime.ClosedWindowResources;
        var firstUseFlags = closedWindow.FirstUseFlags;
        var displayedPlan = state.DisplayedPlan;
        var canonicalState = state.CanonicalText;
        var stateChecksum = state.Checksum;
        var battleRngText = battle.RngState.ToCanonicalText();
        var cardRngText = cardTurn.RngState.ToCanonicalText();
        var logChecksum = commandLog.CurrentChecksum;
        var resourceText = closedWindow.ToCanonicalText();

        var battlePreview = QueryBattle(session);
        var preview = Query(session, BasicCard(session).InstanceId);

        Assert.True(battlePreview.Accepted, battlePreview.ReasonId);
        Assert.True(preview.Accepted, preview.ReasonId);
        Assert.Same(state, session.State);
        Assert.Same(battle, session.State.BattleState);
        Assert.Same(runtime, session.State.RuntimeState);
        Assert.Same(cardTurn, session.State.CardTurnState);
        Assert.Same(battle.RngState, session.State.BattleState.RngState);
        Assert.Same(cardTurn.RngState, session.State.CardTurnState.RngState);
        Assert.Same(commandLog, session.CommandLog);
        Assert.Same(logEntries, session.CommandLog.Entries);
        Assert.Same(closedWindow, session.State.RuntimeState.ClosedWindowResources);
        Assert.Same(firstUseFlags, session.State.RuntimeState.ClosedWindowResources.FirstUseFlags);
        Assert.Same(displayedPlan, session.State.DisplayedPlan);
        Assert.Equal(canonicalState, session.State.CanonicalText);
        Assert.Equal(stateChecksum, session.State.Checksum);
        Assert.Equal(battleRngText, session.State.BattleState.RngState.ToCanonicalText());
        Assert.Equal(cardRngText, session.State.CardTurnState.RngState.ToCanonicalText());
        Assert.Equal(logChecksum, session.CommandLog.CurrentChecksum);
        Assert.Equal(resourceText, session.State.RuntimeState.ClosedWindowResources.ToCanonicalText());
        Assert.Empty(session.CommandLog.Entries);
    }

    [Fact]
    public void AcceptedPreviewReportsNewEnemyAtariFromTheSharedEffectiveLibertyAnalysis()
    {
        var session = StartAtariSession();
        var preview = Query(session, BasicCard(session).InstanceId);
        var candidate = Candidate(preview, 3, 2, "frontline");

        Assert.True(candidate.Accepted, candidate.ReasonId);
        var accepted = Assert.IsType<CoreDuelAcceptedCardPlayPreview>(
            candidate.AcceptedResult);
        var newlyAtari = Assert.Single(accepted.NewEnemyAtariGroups);
        Assert.Equal("white", newlyAtari.ColorId);
        Assert.Equal(CoreDuelBattleTestFixture.Point(3, 3), newlyAtari.Anchor);
        Assert.Equal(
            CoreDuelBattleTestFixture.Point(4, 3),
            Assert.Single(newlyAtari.RealLibertyPoints));
        Assert.Equal(1, newlyAtari.EffectiveLibertyCount);
        Assert.True(newlyAtari.IsAtari);

        var committed = CoreDuelBattleStateMachine.Execute(
            session,
            candidate.CommitCommand);

        Assert.True(committed.Accepted, committed.ReasonId);
        Assert.Equal(accepted.StateChecksum, committed.StateChecksum);
        Assert.Equal(accepted.LogChecksum, committed.LogChecksum);
        AssertAllGroupsMatch(
            committed.SessionAfter.State,
            accepted.ResultingGroups);
        Assert.Contains(
            accepted.ResultingGroups,
            group => group.Anchor.Equals(newlyAtari.Anchor) && group.IsAtari);
    }

    [Fact]
    public void AcceptedPreviewDoesNotReportAnExistingEnemyAtariAsNew()
    {
        var session = StartExistingAtariSession();
        var before = QueryBattle(session).Groups.Single(group =>
            group.Anchor.Equals(CoreDuelBattleTestFixture.Point(3, 3)));
        var candidate = Candidate(
            Query(session, BasicCard(session).InstanceId),
            1,
            2,
            "frontline");

        Assert.True(before.IsAtari);
        Assert.True(candidate.Accepted, candidate.ReasonId);
        var accepted = Assert.IsType<CoreDuelAcceptedCardPlayPreview>(
            candidate.AcceptedResult);
        Assert.Empty(accepted.NewEnemyAtariGroups);
        Assert.Contains(
            accepted.ResultingGroups,
            group => group.Anchor.Equals(before.Anchor) && group.IsAtari);
    }

    [Fact]
    public void StoredIntentTargetOutlineTracksAGroupMergeWithoutReplanning()
    {
        var session = CoreDuelBattleTestFixture.Start().Session;
        var plan = Assert.IsType<PlannedEnemyIntent>(session.State.NormalPlan);
        var targetBefore = Assert.IsType<CoreDuelEnemyTargetPreview>(
            QueryBattle(session).DisplayedIntent!.Target);
        var candidate = Candidate(
            Query(session, BasicCard(session).InstanceId),
            1,
            2,
            "frontline");

        Assert.True(candidate.Accepted, candidate.ReasonId);
        var accepted = Assert.IsType<CoreDuelAcceptedCardPlayPreview>(
            candidate.AcceptedResult);
        var intentAfter = Assert.IsType<CoreDuelIntentPreview>(accepted.DisplayedIntent);
        var targetAfter = Assert.IsType<CoreDuelEnemyTargetPreview>(intentAfter.Target);

        Assert.Equal(plan.Checksum, intentAfter.PlanChecksum);
        Assert.Equal(targetBefore.Anchor, targetAfter.Anchor);
        Assert.True(targetAfter.AnchorResolvesToCurrentGroup);
        Assert.Equal(CoreDuelBattleTestFixture.Point(1, 2), targetAfter.CurrentGroupAnchor);
        Assert.Contains(CoreDuelBattleTestFixture.Point(1, 2), targetAfter.CurrentStonePoints);
        Assert.True(targetAfter.CurrentStonePoints.Count > targetBefore.CurrentStonePoints.Count);
    }

    [Fact]
    public void StaleRequestsAndAnOldBoundCommandFailClosedInTheSameOrder()
    {
        var session = CoreDuelBattleTestFixture.Start().Session;
        var sourceState = session.State;
        var card = BasicCard(session);
        var otherState = OtherChecksum(session.State.Checksum);
        var otherLog = OtherChecksum(session.CommandLog.CurrentChecksum);

        var staleState = CoreDuelBattlePreviewQuery.Evaluate(
            session,
            new CoreDuelCardPreviewRequest(
                otherState,
                session.CommandLog.CurrentChecksum,
                card.InstanceId));
        var staleLog = CoreDuelBattlePreviewQuery.Evaluate(
            session,
            new CoreDuelCardPreviewRequest(
                session.State.Checksum,
                otherLog,
                card.InstanceId));
        var staleBattleState = CoreDuelBattlePreviewQuery.Evaluate(
            session,
            new CoreDuelBattlePreviewRequest(
                otherState,
                session.CommandLog.CurrentChecksum));
        var staleBattleLog = CoreDuelBattlePreviewQuery.Evaluate(
            session,
            new CoreDuelBattlePreviewRequest(
                session.State.Checksum,
                otherLog));

        Assert.False(staleState.Accepted);
        Assert.Equal("stale_state", staleState.ReasonId);
        Assert.Empty(staleState.Candidates);
        Assert.False(staleLog.Accepted);
        Assert.Equal("stale_session", staleLog.ReasonId);
        Assert.Empty(staleLog.Candidates);
        Assert.False(staleBattleState.Accepted);
        Assert.Equal("stale_state", staleBattleState.ReasonId);
        Assert.Null(staleBattleState.PhaseId);
        Assert.Null(staleBattleState.Qi);
        Assert.Empty(staleBattleState.BoardPoints);
        Assert.Empty(staleBattleState.Groups);
        Assert.Empty(staleBattleState.HandCards);
        Assert.Null(staleBattleState.DrawPileCount);
        Assert.Null(staleBattleState.ResolvingCardCount);
        Assert.Null(staleBattleState.DiscardPileCount);
        Assert.Null(staleBattleState.ExhaustPileCount);
        Assert.Null(staleBattleState.DisplayedIntent);
        Assert.Null(staleBattleState.BlackKingRisk);
        Assert.False(staleBattleLog.Accepted);
        Assert.Equal("stale_session", staleBattleLog.ReasonId);
        Assert.Null(staleBattleLog.PhaseId);
        Assert.Null(staleBattleLog.Qi);
        Assert.Empty(staleBattleLog.BoardPoints);
        Assert.Empty(staleBattleLog.Groups);
        Assert.Empty(staleBattleLog.HandCards);
        Assert.Null(staleBattleLog.DrawPileCount);
        Assert.Null(staleBattleLog.ResolvingCardCount);
        Assert.Null(staleBattleLog.DiscardPileCount);
        Assert.Null(staleBattleLog.ExhaustPileCount);
        Assert.Null(staleBattleLog.DisplayedIntent);
        Assert.Null(staleBattleLog.BlackKingRisk);
        Assert.Same(sourceState, session.State);
        Assert.Empty(session.CommandLog.Entries);

        var currentRequest = new CoreDuelCardPreviewRequest(
            session.State.Checksum,
            session.CommandLog.CurrentChecksum,
            card.InstanceId);
        var current = CoreDuelBattlePreviewQuery.Evaluate(session, currentRequest);
        var oldCandidate = Candidate(current, 1, 2, "frontline");
        var committed = CoreDuelBattleStateMachine.Execute(
            session,
            oldCandidate.CommitCommand);
        Assert.True(committed.Accepted, committed.ReasonId);

        var oldQuery = CoreDuelBattlePreviewQuery.Evaluate(
            committed.SessionAfter,
            currentRequest);
        var oldCommand = CoreDuelBattleStateMachine.Execute(
            committed.SessionAfter,
            oldCandidate.CommitCommand);

        Assert.False(oldQuery.Accepted);
        Assert.Equal("stale_state", oldQuery.ReasonId);
        Assert.False(oldCommand.Accepted);
        Assert.Equal("stale_state", oldCommand.ReasonId);
        Assert.Same(committed.SessionAfter, oldCommand.SessionBefore);
        Assert.Same(committed.SessionAfter, oldCommand.SessionAfter);
        var rejection = Assert.Single(oldCommand.OrderedFacts);
        Assert.Equal("stale_state", Assert.IsType<CommandRejectedFact>(rejection).ReasonId);
    }

    [Fact]
    public void DisplayedIntentAndLethalRiskAreMappedFromTheAuthoritativeBanditKernel()
    {
        var session = CoreDuelBattleTestFixture.StartLethal().Session;
        var plan = Assert.IsType<PlannedEnemyIntent>(session.State.NormalPlan);

        var preview = Query(session, BasicCard(session).InstanceId);

        Assert.True(preview.Accepted, preview.ReasonId);
        AssertIntentMatches(
            Assert.IsType<CoreDuelIntentPreview>(preview.NormalIntent),
            plan,
            isCounterattackAction: false);
        AssertIntentMatches(
            Assert.IsType<CoreDuelIntentPreview>(preview.DisplayedIntent),
            plan,
            isCounterattackAction: false);
        Assert.Null(preview.BonusIntent);
        var mandatory = Assert.IsType<CoreDuelMandatoryOverridePreview>(
            preview.MandatoryOverride);
        Assert.Equal("lethal", mandatory.KindId);
        Assert.Equal("mandatory_lethal_override", mandatory.ReasonId);
        Assert.Equal(CoreDuelBattleTestFixture.Point(2, 3), mandatory.Point);
        var risk = Assert.IsType<CoreDuelBlackKingRiskPreview>(preview.BlackKingRisk);
        Assert.False(risk.IsCaptured);
        Assert.True(risk.HasMandatoryLethalOverride);
        Assert.Equal("mandatory_lethal_override", risk.ReasonId);
        Assert.Equal(CoreDuelBattleTestFixture.Point(2, 3), risk.ThreatPoint);

        var savingMove = Candidate(preview, 2, 3, "frontline");
        Assert.True(savingMove.Accepted, savingMove.ReasonId);
        var afterSavingMove = Assert.IsType<CoreDuelAcceptedCardPlayPreview>(
            savingMove.AcceptedResult);
        AssertIntentMatches(
            Assert.IsType<CoreDuelIntentPreview>(afterSavingMove.DisplayedIntent),
            plan,
            isCounterattackAction: false);
        Assert.False(afterSavingMove.BlackKingRisk.HasMandatoryLethalOverride);
        Assert.Equal("none", afterSavingMove.MandatoryOverride.KindId);
    }

    [Fact]
    public void TerminalCapturePreviewMatchesTheCommittedKingCaptureFacts()
    {
        var session = StartVictorySession();
        var card = BasicCard(session);
        var preview = Query(session, card.InstanceId);
        var terminal = Candidate(preview, 3, 3, "terminal_capture");

        Assert.True(terminal.Accepted, terminal.ReasonId);
        var accepted = Assert.IsType<CoreDuelAcceptedCardPlayPreview>(
            terminal.AcceptedResult);
        Assert.True(accepted.IsTerminal);
        Assert.Equal("win", accepted.OutcomeId);
        Assert.Equal("white_king_captured", accepted.EndReasonId);
        Assert.Equal("none", accepted.MandatoryOverride.KindId);
        Assert.False(accepted.BlackKingRisk.IsCaptured);

        var committed = CoreDuelBattleStateMachine.Execute(
            session,
            terminal.CommitCommand);

        Assert.True(committed.Accepted, committed.ReasonId);
        Assert.True(committed.SessionAfter.State.IsTerminal);
        Assert.Equal(accepted.StateChecksum, committed.StateChecksum);
        Assert.Equal(accepted.LogChecksum, committed.LogChecksum);
        AssertCapturedGroupsMatch(committed, accepted);
        AssertAllGroupsMatch(
            committed.SessionAfter.State,
            accepted.ResultingGroups);
        Assert.DoesNotContain(
            accepted.ResultingGroups,
            group => StringComparer.Ordinal.Equals(group.ColorId, "white") &&
                group.ContainsKing);
        Assert.Empty(accepted.NewEnemyAtariGroups);
        var capture = Assert.Single(accepted.CapturedGroups);
        Assert.Equal("white", capture.ColorId);
        Assert.True(capture.ContainsKing);
        Assert.Equal(CoreDuelBattleTestFixture.Point(3, 2), capture.Anchor);
        Assert.Equal(
            CoreDuelBattleTestFixture.Point(3, 2),
            Assert.Single(capture.StonePoints));
        Assert.Contains(
            committed.OrderedFacts,
            fact => fact is BattleEndedFact ended &&
                ended.Reason == BattleEndReason.WhiteKingCaptured);
    }

    private static CoreDuelCardPreviewResult Query(
        CoreDuelBattleSession session,
        string cardInstanceId) =>
        CoreDuelBattlePreviewQuery.Evaluate(
            session,
            new CoreDuelCardPreviewRequest(
                session.State.Checksum,
                session.CommandLog.CurrentChecksum,
                cardInstanceId));

    private static CoreDuelBattlePreviewResult QueryBattle(
        CoreDuelBattleSession session) =>
        CoreDuelBattlePreviewQuery.Evaluate(
            session,
            new CoreDuelBattlePreviewRequest(
                session.State.Checksum,
                session.CommandLog.CurrentChecksum));

    private static BattleCardInstance BasicCard(CoreDuelBattleSession session) =>
        session.State.CardTurnState.Deck.Hand.First(card =>
            StringComparer.Ordinal.Equals(card.ContentId, "card_basic_stone"));

    private static CoreDuelCardCandidatePreview Candidate(
        CoreDuelCardPreviewResult preview,
        int x,
        int y,
        string modeId) =>
        preview.Candidates.Single(candidate =>
            candidate.Target.X == x &&
            candidate.Target.Y == y &&
            StringComparer.Ordinal.Equals(candidate.PlacementModeId, modeId));

    private static void AssertCapturedGroupsMatch(
        CoreDuelBattleCommandResult committed,
        CoreDuelAcceptedCardPlayPreview preview)
    {
        var facts = committed.OrderedFacts.OfType<GroupCapturedFact>().ToArray();
        Assert.Equal(facts.Length, preview.CapturedGroups.Count);
        for (var index = 0; index < facts.Length; index++)
        {
            var group = facts[index].CapturedGroup;
            var projected = preview.CapturedGroups[index];
            Assert.Equal(ColorId(group.Color), projected.ColorId);
            Assert.Equal(group.Anchor, projected.Anchor);
            Assert.Equal(group.StonePoints, projected.StonePoints);
            Assert.Equal(group.Stones.Any(stone => stone.IsKing), projected.ContainsKing);
        }
    }

    private static void AssertTerritoryDeltaMatches(
        CoreDuelBattleSession before,
        CoreDuelBattleSession after,
        CoreDuelAcceptedCardPlayPreview preview)
    {
        var beforeTerritory = before.State.BattleState.TerritoryAnalysis;
        var afterTerritory = after.State.BattleState.TerritoryAnalysis;
        var expected = before.State.BattleState.Board.Geometry.CanonicalPoints
            .Select(point => new
            {
                Point = point,
                Before = TerritoryOwnerId(beforeTerritory.RegionAt(point)?.Owner),
                After = TerritoryOwnerId(afterTerritory.RegionAt(point)?.Owner),
            })
            .Where(delta => !StringComparer.Ordinal.Equals(delta.Before, delta.After))
            .ToArray();

        Assert.Equal(expected.Length, preview.TerritoryDeltas.Count);
        for (var index = 0; index < expected.Length; index++)
        {
            Assert.Equal(expected[index].Point, preview.TerritoryDeltas[index].Point);
            Assert.Equal(expected[index].Before, preview.TerritoryDeltas[index].OwnerBeforeId);
            Assert.Equal(expected[index].After, preview.TerritoryDeltas[index].OwnerAfterId);
        }

        Assert.Equal(
            OwnedPointCount(afterTerritory, TerritoryOwner.Black) -
                OwnedPointCount(beforeTerritory, TerritoryOwner.Black),
            preview.BlackTerritoryPointDelta);
        Assert.Equal(
            OwnedPointCount(afterTerritory, TerritoryOwner.White) -
                OwnedPointCount(beforeTerritory, TerritoryOwner.White),
            preview.WhiteTerritoryPointDelta);
    }

    private static void AssertResultingTargetGroupMatches(
        CoreDuelBattleState state,
        CanonicalPoint target,
        CoreDuelGroupPreview? preview)
    {
        var groups = StoneGroupAnalyzer.Analyze(state.BattleState.Board);
        var group = groups.GroupAt(target);
        if (group is null)
        {
            Assert.Null(preview);
            return;
        }

        var projected = Assert.IsType<CoreDuelGroupPreview>(preview);
        AssertGroupMatches(state, group, projected);
    }

    private static void AssertAllGroupsMatch(
        CoreDuelBattleState state,
        IReadOnlyList<CoreDuelGroupPreview> previews)
    {
        var groups = StoneGroupAnalyzer.Analyze(state.BattleState.Board);
        Assert.Equal(groups.Groups.Count, previews.Count);
        for (var index = 0; index < groups.Groups.Count; index++)
        {
            AssertGroupMatches(state, groups.Groups[index], previews[index]);
        }
    }

    private static void AssertGroupMatches(
        CoreDuelBattleState state,
        StoneGroup group,
        CoreDuelGroupPreview projected)
    {
        var groups = StoneGroupAnalyzer.Analyze(state.BattleState.Board);
        group = groups.GroupAt(group.Anchor)
            ?? throw new InvalidOperationException("Expected preview group is missing.");
        var effective = TemporaryLibertyEffectiveLibertyAnalyzer.Analyze(
            state.RuntimeState.StoneRuntimeState,
            state.RuntimeState.TemporaryLibertyState,
            state.RuntimeState.ContinuousLibertySnapshot,
            groups);
        var breakdown = effective.BreakdownFor(group);
        Assert.Equal(ColorId(group.Color), projected.ColorId);
        Assert.Equal(group.Anchor, projected.Anchor);
        Assert.Equal(group.StonePoints, projected.StonePoints);
        Assert.Equal(group.RealLiberties, projected.RealLibertyPoints);
        Assert.Equal(group.Stones.Any(stone => stone.IsKing), projected.ContainsKing);
        Assert.Equal(breakdown.TimedAmount, projected.TimedLibertyAmount);
        Assert.Equal(breakdown.ContinuousAmount, projected.ContinuousLibertyAmount);
        Assert.Equal(breakdown.EffectiveLibertyCount, projected.EffectiveLibertyCount);
        Assert.Equal(breakdown.EffectiveLibertyCount == 1, projected.IsAtari);
    }

    private static void AssertIntentMatches(
        CoreDuelIntentPreview actual,
        PlannedEnemyIntent expected,
        bool isCounterattackAction)
    {
        Assert.Equal(isCounterattackAction, actual.IsCounterattackAction);
        Assert.Equal(expected.IntentId, actual.IntentId);
        Assert.Equal(expected.PrimaryPoint, actual.PrimaryPoint);
        Assert.Equal(expected.AlternatePoints, actual.AlternatePoints);
        Assert.Equal(expected.Retargetable, actual.Retargetable);
        Assert.Equal(expected.PlannedFromStateChecksum, actual.PlannedFromStateChecksum);
        Assert.Equal(expected.Checksum, actual.PlanChecksum);
        if (expected.TargetReference is null)
        {
            Assert.Null(actual.Target);
            return;
        }

        var target = Assert.IsType<CoreDuelEnemyTargetPreview>(actual.Target);
        Assert.Equal("stone_group", target.KindId);
        Assert.Equal(ColorId(expected.TargetReference.Color), target.ColorId);
        Assert.Equal(expected.TargetReference.Anchor, target.Anchor);
    }

    private static void AssertCurrentTargetMatches(
        CoreDuelBattleState state,
        PlannedEnemyIntent expected,
        CoreDuelIntentPreview actual)
    {
        var targetReference = Assert.IsType<EnemyTargetReference>(
            expected.TargetReference);
        var target = Assert.IsType<CoreDuelEnemyTargetPreview>(actual.Target);
        var group = StoneGroupAnalyzer.Analyze(state.BattleState.Board)
            .GroupAt(targetReference.Anchor);
        if (group is null || group.Color != targetReference.Color)
        {
            Assert.False(target.AnchorResolvesToCurrentGroup);
            Assert.Null(target.CurrentGroupAnchor);
            Assert.Empty(target.CurrentStonePoints);
            return;
        }

        Assert.True(target.AnchorResolvesToCurrentGroup);
        Assert.Equal(group.Anchor, target.CurrentGroupAnchor);
        Assert.Equal(group.StonePoints, target.CurrentStonePoints);
    }

    private static void AssertMandatoryOverrideMatches(
        CoreDuelBattleSession session,
        CoreDuelMandatoryOverridePreview actual)
    {
        var expected = CoreDuelBattleStateMachine.PreviewMandatoryOverride(session);
        var expectedKind = expected.Kind switch
        {
            BanditMandatoryOverrideKind.None => "none",
            BanditMandatoryOverrideKind.Lethal => "lethal",
            BanditMandatoryOverrideKind.Defense => "defense",
            _ => throw new InvalidOperationException("Unknown Bandit override kind."),
        };
        Assert.Equal(expectedKind, actual.KindId);
        Assert.Equal(expected.Point, actual.Point);
        Assert.Equal(expected.Decision?.ReasonId ?? "none", actual.ReasonId);
        Assert.Equal(expected.Decision?.ExecutedIntentId ?? "none", actual.ExecutedIntentId);
    }

    private static CoreDuelBattleSession StartVictorySession()
    {
        var catalog = CoreDuelBattleTestFixture.LoadCatalog();
        var metadata = ReplayMetadata.Create(
            CoreDuelBattleTestFixture.GameVersion,
            catalog.ContentHash,
            CoreDuelBattleTestFixture.Seed);
        return CoreDuelBattleStateMachine.Start(
            VictoryInitialSnapshot(),
            catalog,
            metadata).Session;
    }

    private static CoreDuelBattleSession StartAtariSession()
    {
        var catalog = CoreDuelBattleTestFixture.LoadCatalog();
        var metadata = ReplayMetadata.Create(
            CoreDuelBattleTestFixture.GameVersion,
            catalog.ContentHash,
            CoreDuelBattleTestFixture.Seed);
        return CoreDuelBattleStateMachine.Start(
            AtariInitialSnapshot(),
            catalog,
            metadata).Session;
    }

    private static CoreDuelBattleSession StartExistingAtariSession()
    {
        var catalog = CoreDuelBattleTestFixture.LoadCatalog();
        var metadata = ReplayMetadata.Create(
            CoreDuelBattleTestFixture.GameVersion,
            catalog.ContentHash,
            CoreDuelBattleTestFixture.Seed);
        return CoreDuelBattleStateMachine.Start(
            ExistingAtariInitialSnapshot(),
            catalog,
            metadata).Session;
    }

    private static CoreDuelBattleSession StartPendingCounterattackSession()
    {
        var initial = CoreDuelBattleTestFixture.InitialSnapshot();
        var pending = CounterattackBoundaryState.Create(
            initial.CounterattackState.GaugeUnits,
            pending: true,
            initial.CounterattackState.SacrificeStoneRemainder,
            initial.CounterattackPolicy);
        var pendingInitial = BattleAuthoritativeInitialSnapshot.Create(
            initial.StoneRuntimeState,
            initial.TemporaryLibertyState,
            initial.ContinuousLibertySnapshot,
            initial.RepetitionHistory,
            initial.FacilityState,
            initial.ClosedWindowResources,
            initial.CaptureBenefitTriggerPlan,
            pending,
            initial.CounterattackPolicy,
            initial.RuntimePolicy,
            initial.PlayerTurnIndex);
        var catalog = CoreDuelBattleTestFixture.LoadCatalog();
        return CoreDuelBattleStateMachine.Start(
            pendingInitial,
            catalog,
            ReplayMetadata.Create(
                CoreDuelBattleTestFixture.GameVersion,
                catalog.ContentHash,
                CoreDuelBattleTestFixture.Seed)).Session;
    }

    private static CoreDuelBattleSession StartWithCardInHand(string contentId)
    {
        for (var seed = 0L; seed < 64L; seed++)
        {
            var session = CoreDuelBattleTestFixture.Start(seed).Session;
            if (session.State.CardTurnState.Deck.Hand.Any(card =>
                    StringComparer.Ordinal.Equals(card.ContentId, contentId)))
            {
                return session;
            }
        }

        throw new InvalidOperationException(
            $"No deterministic seed in the bounded fixture search drew {contentId}.");
    }

    private static BattleAuthoritativeInitialSnapshot VictoryInitialSnapshot()
    {
        var geometry = BoardGeometry.Create(BoardGeometry.AcceptedSize);
        var board = BoardState.Create(
            geometry,
            [
                Stone(geometry, StoneColor.Black, isKing: false, 3, 1),
                Stone(geometry, StoneColor.Black, isKing: false, 2, 2),
                Stone(geometry, StoneColor.White, isKing: true, 3, 2),
                Stone(geometry, StoneColor.Black, isKing: false, 4, 2),
                Stone(geometry, StoneColor.Black, isKing: true, 7, 7),
            ]);
        var instances = board.OccupiedStones
            .Select((stone, index) => new StoneRuntimeInstance(
                "preview.victory.stone." +
                index.ToString("D2", CultureInfo.InvariantCulture),
                stone,
                stone.IsKing ? "king" : "standard",
                index + 1L,
                []))
            .ToArray();
        var runtime = StoneRuntimeState.Create(
            board,
            instances,
            instances.Length + 1L);
        var standard = CoreDuelBattleTestFixture.InitialSnapshot();
        return BattleAuthoritativeInitialSnapshot.Create(
            runtime,
            TemporaryLibertyState.Create(runtime, [], nextCreatedSequence: 1),
            ContinuousLibertySnapshot.Empty(runtime),
            BattleRepetitionHistory.Start(board),
            FacilityState.Create(board, [], nextBuildSequence: 1),
            ClosedWindowResourceState.Empty([]),
            CaptureBenefitTriggerPlan.Create([]),
            standard.CounterattackState,
            standard.CounterattackPolicy,
            standard.RuntimePolicy,
            playerTurnIndex: 1);
    }

    private static BattleAuthoritativeInitialSnapshot AtariInitialSnapshot()
    {
        var geometry = BoardGeometry.Create(BoardGeometry.AcceptedSize);
        var board = BoardState.Create(
            geometry,
            [
                Stone(geometry, StoneColor.Black, isKing: true, 2, 2),
                Stone(geometry, StoneColor.Black, isKing: false, 2, 3),
                Stone(geometry, StoneColor.White, isKing: false, 3, 3),
                Stone(geometry, StoneColor.Black, isKing: false, 3, 4),
                Stone(geometry, StoneColor.White, isKing: true, 7, 7),
            ]);
        var instances = board.OccupiedStones
            .Select((stone, index) => new StoneRuntimeInstance(
                "preview.atari.stone." +
                index.ToString("D2", CultureInfo.InvariantCulture),
                stone,
                stone.IsKing ? "king" : "standard",
                index + 1L,
                []))
            .ToArray();
        var runtime = StoneRuntimeState.Create(
            board,
            instances,
            instances.Length + 1L);
        var standard = CoreDuelBattleTestFixture.InitialSnapshot();
        return BattleAuthoritativeInitialSnapshot.Create(
            runtime,
            TemporaryLibertyState.Create(runtime, [], nextCreatedSequence: 1),
            ContinuousLibertySnapshot.Empty(runtime),
            BattleRepetitionHistory.Start(board),
            FacilityState.Create(board, [], nextBuildSequence: 1),
            ClosedWindowResourceState.Empty([]),
            CaptureBenefitTriggerPlan.Create([]),
            standard.CounterattackState,
            standard.CounterattackPolicy,
            standard.RuntimePolicy,
            playerTurnIndex: 1);
    }

    private static BattleAuthoritativeInitialSnapshot ExistingAtariInitialSnapshot()
    {
        var geometry = BoardGeometry.Create(BoardGeometry.AcceptedSize);
        var board = BoardState.Create(
            geometry,
            [
                Stone(geometry, StoneColor.Black, isKing: true, 2, 2),
                Stone(geometry, StoneColor.Black, isKing: false, 3, 2),
                Stone(geometry, StoneColor.Black, isKing: false, 2, 3),
                Stone(geometry, StoneColor.White, isKing: false, 3, 3),
                Stone(geometry, StoneColor.Black, isKing: false, 3, 4),
                Stone(geometry, StoneColor.White, isKing: true, 7, 7),
            ]);
        var instances = board.OccupiedStones
            .Select((stone, index) => new StoneRuntimeInstance(
                "preview.existing_atari.stone." +
                index.ToString("D2", CultureInfo.InvariantCulture),
                stone,
                stone.IsKing ? "king" : "standard",
                index + 1L,
                []))
            .ToArray();
        var runtime = StoneRuntimeState.Create(
            board,
            instances,
            instances.Length + 1L);
        var standard = CoreDuelBattleTestFixture.InitialSnapshot();
        return BattleAuthoritativeInitialSnapshot.Create(
            runtime,
            TemporaryLibertyState.Create(runtime, [], nextCreatedSequence: 1),
            ContinuousLibertySnapshot.Empty(runtime),
            BattleRepetitionHistory.Start(board),
            FacilityState.Create(board, [], nextBuildSequence: 1),
            ClosedWindowResourceState.Empty([]),
            CaptureBenefitTriggerPlan.Create([]),
            standard.CounterattackState,
            standard.CounterattackPolicy,
            standard.RuntimePolicy,
            playerTurnIndex: 1);
    }

    private static BoardStone Stone(
        BoardGeometry geometry,
        StoneColor color,
        bool isKing,
        int x,
        int y) =>
        new(color, isKing, geometry.CreateCanonicalPoint(x, y));

    private static int OwnedPointCount(
        TerritoryAnalysis analysis,
        TerritoryOwner owner) =>
        analysis.Regions
            .Where(region => region.Owner == owner)
            .Sum(region => region.Size);

    private static string TerritoryOwnerId(TerritoryOwner? owner) => owner switch
    {
        null => "none",
        TerritoryOwner.Neutral => "neutral",
        TerritoryOwner.Black => "black",
        TerritoryOwner.White => "white",
        _ => throw new InvalidOperationException("Unknown territory owner."),
    };

    private static string ColorId(StoneColor color) => color switch
    {
        StoneColor.Black => "black",
        StoneColor.White => "white",
        _ => throw new InvalidOperationException("Unknown stone color."),
    };

    private static string OtherChecksum(string checksum) =>
        (checksum[0] == '0' ? '1' : '0') + checksum[1..];
}
