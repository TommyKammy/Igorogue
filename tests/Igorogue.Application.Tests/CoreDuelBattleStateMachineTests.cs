using System.Globalization;
using System.Text.Json;

using Igorogue.Application.Battle;
using Igorogue.Application.Replay;
using Igorogue.Content;
using Igorogue.Domain.Board;
using Igorogue.Domain.Cards;
using Igorogue.Domain.Combat;
using Igorogue.Domain.Enemies;

namespace Igorogue.Application.Tests;

public sealed class CoreDuelBattleStateMachineTests
{
    private const string GameVersion = "v0.2.10";
    private const long Seed = 39039;

    [Fact]
    public void BootstrapBindsTheGeneratedCatalogAndExpandsTheExactPhysicalRecipe()
    {
        var catalog = CoreDuelBattleTestFixture.LoadCatalog();
        var initial = CoreDuelBattleTestFixture.InitialSnapshot();
        var metadata = ReplayMetadata.Create(GameVersion, catalog.ContentHash, Seed);

        var bootstrap = CoreDuelBattleBootstrap.Create(initial, catalog, metadata);

        Assert.Same(initial, bootstrap.InitialSnapshot);
        Assert.Same(catalog, bootstrap.Catalog);
        Assert.Same(metadata, bootstrap.Metadata);
        Assert.Equal(catalog.ContentHash, bootstrap.Metadata.ContentHash);
        Assert.Equal(Seed, bootstrap.Metadata.InitialSeed);
        Assert.Equal(
            [
                "deck.core_duel.card_basic_stone.0001",
                "deck.core_duel.card_basic_stone.0002",
                "deck.core_duel.card_basic_stone.0003",
                "deck.core_duel.card_basic_stone.0004",
                "deck.core_duel.card_basic_stone.0005",
                "deck.core_duel.card_contact.0001",
                "deck.core_duel.card_contact.0002",
                "deck.core_duel.card_development.0001",
                "deck.core_duel.card_extend.0001",
                "deck.core_duel.card_extend.0002",
                "deck.core_duel.card_lure_stone.0001",
                "deck.core_duel.card_reinforce.0001",
            ],
            bootstrap.PhysicalDeckRecipe.Select(card => card.InstanceId));
        Assert.Equal(12, bootstrap.PhysicalDeckRecipe.Count);
        Assert.Equal(
            12,
            bootstrap.PhysicalDeckRecipe
                .Select(card => card.InstanceId)
                .Distinct(StringComparer.Ordinal)
                .Count());
        Assert.Equal(
            catalog.StartingDeck.Entries
                .ToDictionary(entry => entry.CardId, entry => entry.Count, StringComparer.Ordinal),
            bootstrap.PhysicalDeckRecipe
                .GroupBy(card => card.ContentId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal));
    }

    [Fact]
    public void StartBindsSeedContentAndInitialRuntimeBeforePublishingTheFirstIntent()
    {
        var start = StartBattle();
        var state = start.Session.State;

        Assert.Equal(BattlePhase.PlayerAction, state.BattleState.Phase);
        Assert.False(state.IsTerminal);
        Assert.Equal(0, state.RestartCount);
        Assert.Equal(1, state.BattleState.PlayerTurnIndex);
        Assert.Equal(Seed, state.BattleState.RngState.InitialSeed);
        Assert.Equal(Seed, state.CardTurnState.RngState.InitialSeed);
        Assert.Equal(Seed, start.Session.CommandLog.Metadata.InitialSeed);
        Assert.Equal(
            state.Bootstrap.Catalog.ContentHash,
            start.Session.CommandLog.Metadata.ContentHash);
        Assert.Same(
            state.Bootstrap.InitialSnapshot.StoneRuntimeState,
            state.RuntimeState.StoneRuntimeState);
        Assert.Same(
            state.Bootstrap.InitialSnapshot.FacilityState,
            state.BattleState.FacilityState);
        Assert.Same(
            state.Bootstrap.Catalog.SystemPolicy,
            state.CardTurnState.SystemPolicy);
        Assert.Equal(3, state.CardTurnState.Qi);
        Assert.Equal(5, state.CardTurnState.Deck.Hand.Count);
        Assert.Equal(12, AllCards(state.CardTurnState.Deck).Count);
        Assert.Equal(
            state.Bootstrap.PhysicalDeckRecipe
                .Select(card => card.InstanceId)
                .Order(StringComparer.Ordinal),
            AllCards(state.CardTurnState.Deck)
                .Select(card => card.InstanceId)
                .Order(StringComparer.Ordinal));

        var displayed = state.DisplayedPlan;
        Assert.NotNull(displayed);
        var initialIntentFact = Assert.Single(
            start.OrderedFacts.OfType<EnemyIntentPlannedFact>(),
            fact => !fact.IsCounterattackAction);
        Assert.Same(displayed, initialIntentFact.PlannedIntent);
        Assert.Empty(start.Session.CommandLog.Entries);
    }

    [Fact]
    public void PlayerPlayKeepsTheDisplayedIntentFixedUntilBanditResolutionPlansTheNextTurn()
    {
        var start = StartBattle();
        var session = start.Session;
        var displayedBefore = session.State.DisplayedPlan;
        Assert.NotNull(displayedBefore);
        var previewBefore = CoreDuelBattleStateMachine.PreviewMandatoryOverride(session);
        var stateChecksumBeforePreview = session.State.Checksum;
        var logChecksumBeforePreview = session.CommandLog.CurrentChecksum;

        Assert.True(Enum.IsDefined(previewBefore.Kind));
        Assert.Equal(stateChecksumBeforePreview, session.State.Checksum);
        Assert.Equal(logChecksumBeforePreview, session.CommandLog.CurrentChecksum);

        var play = CoreDuelBattleStateMachine.Execute(
            session,
            FirstPlayableCardCommand(session));

        Assert.True(play.Accepted, play.ReasonId);
        session = play.SessionAfter;
        Assert.Same(displayedBefore, session.State.DisplayedPlan);
        Assert.Equal(displayedBefore.Checksum, session.State.DisplayedPlan!.Checksum);

        var previewAfterPlay = CoreDuelBattleStateMachine.PreviewMandatoryOverride(session);
        Assert.True(Enum.IsDefined(previewAfterPlay.Kind));
        Assert.Equal(session.State.Checksum, play.StateChecksum);
        Assert.Equal(session.CommandLog.CurrentChecksum, play.LogChecksum);

        var endTurn = ExecuteAccepted(
            session,
            new EndPlayerTurnCommand(
                session.State.Checksum,
                session.CommandLog.CurrentChecksum));
        session = endTurn.SessionAfter;
        Assert.Equal(BattlePhase.EnemyAction, session.State.BattleState.Phase);
        Assert.Same(displayedBefore, session.State.DisplayedPlan);

        var enemy = ExecuteAccepted(
            session,
            new ResolveBanditEnemyActionCommand(
                session.State.Checksum,
                session.CommandLog.CurrentChecksum));
        session = enemy.SessionAfter;

        Assert.Equal(BattlePhase.PlayerAction, session.State.BattleState.Phase);
        Assert.Equal(2, session.State.BattleState.PlayerTurnIndex);
        Assert.Equal(3, session.State.CardTurnState.Qi);
        Assert.Equal(5, session.State.CardTurnState.Deck.Hand.Count);
        Assert.Contains(enemy.OrderedFacts, fact => fact is EnemyActionStartedFact);
        Assert.Contains(enemy.OrderedFacts, fact => fact is EnemyActionResolvedFact);
        Assert.Contains(
            enemy.OrderedFacts,
            fact => fact is EnemyIntentPlannedFact planned &&
                !planned.IsCounterattackAction);
        var displayedAfter = session.State.DisplayedPlan;
        Assert.NotNull(displayedAfter);
        Assert.NotSame(displayedBefore, displayedAfter);
        Assert.NotEqual(displayedBefore.Checksum, displayedAfter.Checksum);
    }

    [Fact]
    public void MandatoryLethalPreviewIsAnExactNoOpAndResolutionPublishesTheOverrideReason()
    {
        var session = CoreDuelBattleTestFixture.StartLethal().Session;
        var displayed = session.State.DisplayedPlan;
        Assert.NotNull(displayed);
        var stateChecksumBefore = session.State.Checksum;
        var canonicalStateBefore = session.State.CanonicalText;
        var logChecksumBefore = session.CommandLog.CurrentChecksum;

        var preview = CoreDuelBattleStateMachine.PreviewMandatoryOverride(session);

        Assert.True(preview.HasOverride);
        Assert.Equal(BanditMandatoryOverrideKind.Lethal, preview.Kind);
        Assert.Equal(CoreDuelBattleTestFixture.Point(2, 3), preview.Point);
        Assert.Equal(
            BanditExecutionReason.MandatoryLethalOverride,
            preview.Decision!.Reason);
        Assert.Same(displayed, session.State.DisplayedPlan);
        Assert.Equal(stateChecksumBefore, session.State.Checksum);
        Assert.Equal(canonicalStateBefore, session.State.CanonicalText);
        Assert.Equal(logChecksumBefore, session.CommandLog.CurrentChecksum);
        Assert.Empty(session.CommandLog.Entries);

        var endTurn = ExecuteAccepted(
            session,
            new EndPlayerTurnCommand(
                session.State.Checksum,
                session.CommandLog.CurrentChecksum));
        session = endTurn.SessionAfter;
        Assert.Equal(BattlePhase.EnemyAction, session.State.BattleState.Phase);
        Assert.Same(displayed, session.State.DisplayedPlan);

        var enemy = ExecuteAccepted(
            session,
            new ResolveBanditEnemyActionCommand(
                session.State.Checksum,
                session.CommandLog.CurrentChecksum));
        var resolved = Assert.Single(
            enemy.OrderedFacts.OfType<EnemyActionResolvedFact>());

        Assert.Equal("mandatory_lethal_override", resolved.ReasonId);
        Assert.Equal(preview.Decision.ReasonId, resolved.ReasonId);
        Assert.Equal(preview.Point, resolved.ExecutedPoint);
        Assert.True(enemy.SessionAfter.State.IsTerminal);
        Assert.Equal(
            BattleEndReason.BlackKingCaptured,
            enemy.SessionAfter.State.BattleState.EndReason);
    }

    [Fact]
    public void FixedFullBattleAndRestartAreByteForByteDeterministic()
    {
        var first = RunFixedBattle();
        var second = RunFixedBattle();

        Assert.Equal("loss", first.TerminalOutcomeId);
        Assert.Equal(first.TerminalOutcomeId, second.TerminalOutcomeId);
        Assert.Equal(first.TerminalEndReasonId, second.TerminalEndReasonId);
        Assert.Equal(first.TerminalStateChecksum, second.TerminalStateChecksum);
        Assert.Equal(first.TerminalStateBytes, second.TerminalStateBytes);
        Assert.Equal(first.FactBytes, second.FactBytes);
        Assert.Equal(first.CommandLogProjection, second.CommandLogProjection);
        Assert.Equal(first.TerminalLogChecksum, second.TerminalLogChecksum);
        Assert.Equal(first.RestartStateChecksum, second.RestartStateChecksum);
        Assert.Equal(first.RestartStateBytes, second.RestartStateBytes);
        Assert.Equal(first.RestartLogChecksum, second.RestartLogChecksum);
        Assert.Equal(1, first.RestartCount);

        Assert.Equal(first.InitialBattleState, first.RestartedBattleState);
        Assert.Equal(first.InitialCardTurnState, first.RestartedCardTurnState);
        Assert.Equal(first.InitialNormalPlan, first.RestartedNormalPlan);
        Assert.Equal(first.InitialBonusPlan, first.RestartedBonusPlan);
    }

    private static CoreDuelBattleStartResult StartBattle()
        => CoreDuelBattleTestFixture.Start();

    private static FixedBattleResult RunFixedBattle()
    {
        var start = CoreDuelBattleTestFixture.Start(playerTurnLimit: 1);
        var initialState = start.Session.State;
        var session = start.Session;
        var facts = start.OrderedFacts.Select(FactFingerprint).ToList();
        var play = ExecuteAccepted(session, FirstPlayableCardCommand(session));
        session = play.SessionAfter;
        facts.AddRange(play.OrderedFacts.Select(FactFingerprint));

        var commandCount = 1;
        while (!session.State.IsTerminal)
        {
            Assert.True(commandCount < 128, "Fixed Core Duel exceeded its command safety bound.");
            IBattleCommand command = session.State.BattleState.Phase switch
            {
                BattlePhase.PlayerAction => new EndPlayerTurnCommand(
                    session.State.Checksum,
                    session.CommandLog.CurrentChecksum),
                BattlePhase.EnemyAction => new ResolveBanditEnemyActionCommand(
                    session.State.Checksum,
                    session.CommandLog.CurrentChecksum),
                _ => throw new InvalidOperationException("Ongoing Core Duel entered an unknown phase."),
            };
            var result = ExecuteAccepted(session, command);
            session = result.SessionAfter;
            facts.AddRange(result.OrderedFacts.Select(FactFingerprint));
            commandCount++;
        }

        var terminal = session;
        Assert.Equal(BattleOutcome.PlayerDefeat, terminal.State.BattleState.Outcome);
        var restart = ExecuteAccepted(
            terminal,
            new RestartBattleCommand(
                terminal.State.Checksum,
                terminal.CommandLog.CurrentChecksum));
        facts.AddRange(restart.OrderedFacts.Select(FactFingerprint));
        var restarted = restart.SessionAfter;
        Assert.False(restarted.State.IsTerminal);
        Assert.Equal(BattlePhase.PlayerAction, restarted.State.BattleState.Phase);
        Assert.Equal(terminal.State.RestartCount + 1, restarted.State.RestartCount);

        return new FixedBattleResult(
            terminal.State.BattleState.OutcomeId,
            terminal.State.BattleState.EndReasonId,
            terminal.State.Checksum,
            Utf8(terminal.State.CanonicalText),
            terminal.CommandLog.CurrentChecksum,
            facts.SelectMany(Utf8).ToArray(),
            terminal.CommandLog.Entries.Select(ProjectLogEntry).ToArray(),
            restarted.State.Checksum,
            Utf8(restarted.State.CanonicalText),
            restarted.CommandLog.CurrentChecksum,
            restarted.State.RestartCount,
            initialState.BattleState.CanonicalText,
            restarted.State.BattleState.CanonicalText,
            initialState.CardTurnState.CanonicalText,
            restarted.State.CardTurnState.CanonicalText,
            PlanText(initialState.NormalPlan),
            PlanText(restarted.State.NormalPlan),
            PlanText(initialState.BonusPlan),
            PlanText(restarted.State.BonusPlan));
    }

    private static PlayCardCommand FirstPlayableCardCommand(CoreDuelBattleSession session)
    {
        var hand = session.State.CardTurnState.Deck.Hand;
        var stoneCard = hand.FirstOrDefault(card =>
            StringComparer.Ordinal.Equals(card.ContentId, "card_basic_stone")) ??
            hand.FirstOrDefault(card =>
                StringComparer.Ordinal.Equals(card.ContentId, "card_extend"));
        if (stoneCard is not null)
        {
            return new PlayCardCommand(
                session.State.Checksum,
                session.CommandLog.CurrentChecksum,
                stoneCard.InstanceId,
                CoreDuelBattleTestFixture.Point(1, 2),
                StoneCardPlacementMode.Frontline);
        }

        var reinforce = hand.SingleOrDefault(card =>
            StringComparer.Ordinal.Equals(card.ContentId, "card_reinforce"))
            ?? throw new InvalidOperationException(
                "Every five-card starter hand must contain a legal basic, Extend, or Reinforce play.");
        return new PlayCardCommand(
            session.State.Checksum,
            session.CommandLog.CurrentChecksum,
            reinforce.InstanceId,
            CoreDuelBattleTestFixture.Point(2, 2));
    }

    private static CoreDuelBattleCommandResult ExecuteAccepted(
        CoreDuelBattleSession session,
        IBattleCommand command)
    {
        var result = CoreDuelBattleStateMachine.Execute(session, command);
        Assert.True(result.Accepted, result.ReasonId);
        return result;
    }

    private static IReadOnlyList<BattleCardInstance> AllCards(BattleDeckState deck) =>
    [
        .. deck.DrawPile,
        .. deck.Hand,
        .. deck.Resolving.Select(entry => entry.Card),
        .. deck.DiscardPile,
        .. deck.ExhaustPile,
    ];

    private static string FactFingerprint(IBattleFact fact) =>
        $"{fact.GetType().FullName}|" + JsonSerializer.Serialize(fact, fact.GetType());

    private static string ProjectLogEntry(CommandLogEntry entry) => string.Join(
        '|',
        entry.Sequence.ToString(CultureInfo.InvariantCulture),
        entry.CommandType,
        entry.CommandSchemaVersion.ToString(CultureInfo.InvariantCulture),
        Convert.ToBase64String(Utf8(entry.CanonicalPayload)),
        entry.ResultChecksum,
        entry.LogChecksum);

    private static string PlanText(PlannedEnemyIntent? plan) =>
        plan?.CanonicalText ?? "none";

    private static byte[] Utf8(string value) =>
        System.Text.Encoding.UTF8.GetBytes(value);

    private sealed record FixedBattleResult(
        string TerminalOutcomeId,
        string TerminalEndReasonId,
        string TerminalStateChecksum,
        byte[] TerminalStateBytes,
        string TerminalLogChecksum,
        byte[] FactBytes,
        IReadOnlyList<string> CommandLogProjection,
        string RestartStateChecksum,
        byte[] RestartStateBytes,
        string RestartLogChecksum,
        int RestartCount,
        string InitialBattleState,
        string RestartedBattleState,
        string InitialCardTurnState,
        string RestartedCardTurnState,
        string InitialNormalPlan,
        string RestartedNormalPlan,
        string InitialBonusPlan,
        string RestartedBonusPlan);
}
