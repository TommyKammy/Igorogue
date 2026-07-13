using Igorogue.Application.Battle;
using Igorogue.Content;
using Igorogue.Domain.Board;
using Igorogue.Domain.Content;

namespace Igorogue.Godot.CoreDuel;

/// <summary>
/// Presentation-facing owner for one immutable Core Duel session. The Godot Node
/// tree never mutates gameplay state; every transition goes through an
/// Application command and every visible fact comes from an Application query.
/// </summary>
public sealed class CoreDuelGameHost
{
    private readonly CoreDuelContentCatalog catalog;

    private CoreDuelGameHost(
        CoreDuelContentCatalog catalog,
        CoreDuelBattleStartResult startResult)
    {
        this.catalog = catalog;
        Session = startResult.Session;
        Battle = QueryBattle();
    }

    private CoreDuelBattleSession Session { get; set; }

    public CoreDuelBattlePreviewResult Battle { get; private set; }

    public CoreDuelCardPreviewResult? SelectedCard { get; private set; }

    public string? SelectedCardInstanceId => SelectedCard?.CardInstanceId;

    public string LastActionReasonId { get; private set; } = "ready";

    public static CoreDuelGameHost Create(
        string manifestPath,
        string gameVersion,
        long seed)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(manifestPath);
        var catalog = new CoreDuelContentCatalogLoader().Load(manifestPath);
        return Create(catalog, gameVersion, seed);
    }

    public static CoreDuelGameHost Create(
        CoreDuelContentCatalog catalog,
        string gameVersion,
        long seed)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        var startResult = CoreDuelBattleStartup.Start(catalog, gameVersion, seed);
        return new CoreDuelGameHost(catalog, startResult);
    }

    public CardContentDefinition CardDefinition(string contentId) =>
        catalog.StarterCard(contentId);

    public bool SelectCard(string cardInstanceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cardInstanceId);
        var preview = CoreDuelBattlePreviewQuery.Evaluate(
            Session,
            new CoreDuelCardPreviewRequest(
                Session.State.Checksum,
                Session.CommandLog.CurrentChecksum,
                cardInstanceId));
        SelectedCard = preview;
        LastActionReasonId = preview.ReasonId;
        return preview.Accepted;
    }

    public void ClearCardSelection()
    {
        SelectedCard = null;
        LastActionReasonId = "selection_cleared";
    }

    public CoreDuelCardCandidatePreview? CandidateAt(CanonicalPoint point) =>
        SelectedCard?.LegalCandidates.FirstOrDefault(candidate =>
            candidate.Target.Equals(point));

    public bool TryPlaySelectedCard(CanonicalPoint point)
    {
        var candidate = CandidateAt(point);
        if (candidate is null)
        {
            LastActionReasonId = "no_legal_candidate";
            return false;
        }

        var result = CoreDuelBattleStateMachine.Execute(
            Session,
            candidate.CommitCommand);
        LastActionReasonId = result.ReasonId;
        if (!result.Accepted)
        {
            RefreshBattleAndSelection();
            return false;
        }

        Session = result.SessionAfter;
        SelectedCard = null;
        RefreshBattle();
        return true;
    }

    public bool EndTurnAndResolveEnemy()
    {
        if (Battle.IsTerminal)
        {
            LastActionReasonId = "battle_terminal";
            return false;
        }

        var endResult = CoreDuelBattleStateMachine.Execute(
            Session,
            new EndPlayerTurnCommand(
                Session.State.Checksum,
                Session.CommandLog.CurrentChecksum));
        LastActionReasonId = endResult.ReasonId;
        if (!endResult.Accepted)
        {
            RefreshBattleAndSelection();
            return false;
        }

        Session = endResult.SessionAfter;
        SelectedCard = null;
        RefreshBattle();

        var resolvedActions = 0;
        while (!Battle.IsTerminal &&
               StringComparer.Ordinal.Equals(Battle.PhaseId, "enemy_action"))
        {
            if (resolvedActions >= catalog.Bandit.ActionBudget.MaxActionsPerEnemyTurn)
            {
                throw new InvalidOperationException(
                    "Core Duel retained an enemy action after the normal and counterattack windows.");
            }

            var enemyResult = CoreDuelBattleStateMachine.Execute(
                Session,
                new ResolveBanditEnemyActionCommand(
                    Session.State.Checksum,
                    Session.CommandLog.CurrentChecksum));
            LastActionReasonId = enemyResult.ReasonId;
            if (!enemyResult.Accepted)
            {
                RefreshBattle();
                return false;
            }

            Session = enemyResult.SessionAfter;
            resolvedActions++;
            RefreshBattle();
        }

        return true;
    }

    public bool Restart()
    {
        var result = CoreDuelBattleStateMachine.Execute(
            Session,
            new RestartBattleCommand(
                Session.State.Checksum,
                Session.CommandLog.CurrentChecksum));
        LastActionReasonId = result.ReasonId;
        if (!result.Accepted)
        {
            RefreshBattleAndSelection();
            return false;
        }

        Session = result.SessionAfter;
        SelectedCard = null;
        RefreshBattle();
        return true;
    }

    public static string RunHeadlessSmoke(
        string manifestPath,
        string gameVersion,
        long seed)
    {
        var first = Create(manifestPath, gameVersion, seed);
        var second = Create(manifestPath, gameVersion, seed);
        if (!StringComparer.Ordinal.Equals(
                first.Session.State.Checksum,
                second.Session.State.Checksum) ||
            !StringComparer.Ordinal.Equals(
                first.Session.CommandLog.CurrentChecksum,
                second.Session.CommandLog.CurrentChecksum))
        {
            throw new InvalidOperationException(
                "Graybox startup was not deterministic for the same content and seed.");
        }

        if (!first.Battle.Accepted || first.Battle.BoardPoints.Count != 49)
        {
            throw new InvalidOperationException(
                "Graybox battle query did not return the accepted 7x7 projection.");
        }

        var playedCard = false;
        foreach (var card in first.Battle.HandCards)
        {
            if (first.SelectCard(card.InstanceId) &&
                first.SelectedCard?.LegalCandidates.FirstOrDefault() is { } candidate)
            {
                if (!first.TryPlaySelectedCard(candidate.Target))
                {
                    throw new InvalidOperationException(
                        $"Graybox legal preview command was rejected: {first.LastActionReasonId}.");
                }

                playedCard = true;
                break;
            }
        }

        if (!playedCard)
        {
            throw new InvalidOperationException(
                "Graybox startup hand did not contain a playable card candidate.");
        }

        if (!first.EndTurnAndResolveEnemy())
        {
            throw new InvalidOperationException(
                $"Graybox turn smoke was rejected: {first.LastActionReasonId}.");
        }

        if (!first.Battle.IsTerminal &&
            !StringComparer.Ordinal.Equals(first.Battle.PhaseId, "player_action"))
        {
            throw new InvalidOperationException(
                $"Graybox turn smoke ended in unexpected phase: {first.Battle.PhaseId}.");
        }

        for (var turn = 1;
             !first.Battle.IsTerminal && turn < first.catalog.BattleSetup.PlayerTurnLimit;
             turn++)
        {
            if (!first.EndTurnAndResolveEnemy())
            {
                throw new InvalidOperationException(
                    $"Graybox terminal smoke was rejected: {first.LastActionReasonId}.");
            }
        }

        if (!first.Battle.IsTerminal)
        {
            throw new InvalidOperationException(
                "Graybox battle did not reach its content-owned player-turn limit.");
        }

        if (!first.Restart() ||
            first.Battle.IsTerminal ||
            first.Battle.RestartCount != 1 ||
            first.Battle.PlayerTurnIndex != 1 ||
            !StringComparer.Ordinal.Equals(first.Battle.PhaseId, "player_action") ||
            first.Battle.Qi != first.catalog.SystemPolicy.BaseQi ||
            first.Battle.HandCards.Count != first.catalog.SystemPolicy.BaseDraw ||
            !RestartedBoardMatchesTypedSetup(first))
        {
            throw new InvalidOperationException(
                $"Graybox restart smoke failed: {first.LastActionReasonId}.");
        }

        return first.Session.State.Checksum;
    }

    private static bool RestartedBoardMatchesTypedSetup(CoreDuelGameHost host)
    {
        var expected = host.catalog.BattleSetup.InitialPosition.Stones
            .Select(stone => (
                stone.Point.X,
                stone.Point.Y,
                ColorId: stone.Color.ToString().ToLowerInvariant(),
                IsKing: stone.Role == InitialStoneRole.King));
        var actual = host.Battle.BoardPoints
            .Where(point => point.Stone is not null)
            .Select(point => (
                point.Point.X,
                point.Point.Y,
                ColorId: point.Stone!.ColorId,
                point.Stone.IsKing));
        return expected.SequenceEqual(actual);
    }

    private void RefreshBattleAndSelection()
    {
        var selectedId = SelectedCardInstanceId;
        RefreshBattle();
        if (selectedId is not null &&
            Battle.HandCards.Any(card =>
                StringComparer.Ordinal.Equals(card.InstanceId, selectedId)))
        {
            SelectCard(selectedId);
        }
        else
        {
            SelectedCard = null;
        }
    }

    private void RefreshBattle()
    {
        Battle = QueryBattle();
    }

    private CoreDuelBattlePreviewResult QueryBattle()
    {
        var preview = CoreDuelBattlePreviewQuery.Evaluate(
            Session,
            new CoreDuelBattlePreviewRequest(
                Session.State.Checksum,
                Session.CommandLog.CurrentChecksum));
        if (!preview.Accepted)
        {
            throw new InvalidOperationException(
                $"Authoritative graybox battle query was rejected: {preview.ReasonId}.");
        }

        return preview;
    }
}
