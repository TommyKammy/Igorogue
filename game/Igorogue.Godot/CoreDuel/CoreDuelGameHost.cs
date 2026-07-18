using Igorogue.Application.Battle;
using Igorogue.Application.Replay;
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
    private readonly CoreDuelReplayEvidenceRecorder replayRecorder;

    private CoreDuelGameHost(
        CoreDuelContentCatalog catalog,
        CoreDuelBattleStartResult startResult,
        string? replayOutputPath)
    {
        this.catalog = catalog;
        Session = startResult.Session;
        replayRecorder = new CoreDuelReplayEvidenceRecorder(
            catalog,
            startResult.Session,
            replayOutputPath);
        Battle = QueryBattle();
    }

    private CoreDuelBattleSession Session { get; set; }

    public CoreDuelBattlePreviewResult Battle { get; private set; }

    public CoreDuelCardPreviewResult? SelectedCard { get; private set; }

    public string? SelectedCardInstanceId => SelectedCard?.CardInstanceId;

    public string LastActionReasonId { get; private set; } = "ready";

    public bool ReplayEvidenceEnabled => replayRecorder.Enabled;

    public CoreDuelReplayEvidence? ReplayEvidence => replayRecorder.Evidence;

    public string ReplayEvidenceStatus => !ReplayEvidenceEnabled
        ? "OFF"
        : ReplayEvidence is null
            ? "RECORDING"
            : ReplayEvidence.Verified
                ? "VERIFIED"
                : "FAILED";

    public static CoreDuelGameHost Create(
        string manifestPath,
        string gameVersion,
        long seed,
        string? replayOutputPath = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(manifestPath);
        var catalog = new CoreDuelContentCatalogLoader().Load(manifestPath);
        return Create(catalog, gameVersion, seed, replayOutputPath);
    }

    public static CoreDuelGameHost Create(
        CoreDuelContentCatalog catalog,
        string gameVersion,
        long seed,
        string? replayOutputPath = null)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        var startResult = CoreDuelBattleStartup.Start(catalog, gameVersion, seed);
        return new CoreDuelGameHost(catalog, startResult, replayOutputPath);
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

        var result = ExecuteAndObserve(candidate.CommitCommand);
        LastActionReasonId = result.ReasonId;
        if (!result.Accepted)
        {
            RefreshBattleAndSelection();
            return false;
        }

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

        var endResult = ExecuteAndObserve(
            new EndPlayerTurnCommand(
                Session.State.Checksum,
                Session.CommandLog.CurrentChecksum));
        LastActionReasonId = endResult.ReasonId;
        if (!endResult.Accepted)
        {
            RefreshBattleAndSelection();
            return false;
        }

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

            var enemyResult = ExecuteAndObserve(
                new ResolveBanditEnemyActionCommand(
                    Session.State.Checksum,
                    Session.CommandLog.CurrentChecksum));
            LastActionReasonId = enemyResult.ReasonId;
            if (!enemyResult.Accepted)
            {
                RefreshBattle();
                return false;
            }

            resolvedActions++;
            RefreshBattle();
        }

        return true;
    }

    public bool Restart()
    {
        var result = ExecuteAndObserve(
            new RestartBattleCommand(
                Session.State.Checksum,
                Session.CommandLog.CurrentChecksum));
        LastActionReasonId = result.ReasonId;
        if (!result.Accepted)
        {
            RefreshBattleAndSelection();
            return false;
        }

        SelectedCard = null;
        RefreshBattle();
        return true;
    }

    public static CoreDuelHeadlessSmokeResult RunHeadlessSmoke(
        string manifestPath,
        string gameVersion,
        long seed,
        string? replayOutputPath = null,
        string replayScenario = "loss")
    {
        if (replayOutputPath is not null &&
            replayScenario is not "loss" and not "win" and
                not "existing-target-race")
        {
            throw new ArgumentException(
                "Headless replay scenario must be 'loss', 'win', or 'existing-target-race'.",
                nameof(replayScenario));
        }

        var first = Create(manifestPath, gameVersion, seed, replayOutputPath);
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

        if (replayOutputPath is null)
        {
            RunDefaultHeadlessBattle(first);
            VerifyRestart(first);
            return new CoreDuelHeadlessSmokeResult(
                first.Session.State.Checksum,
                ReplayEvidence: null);
        }

        if (first.Restart() ||
            !StringComparer.Ordinal.Equals(
                first.LastActionReasonId,
                "battle_not_terminal"))
        {
            throw new InvalidOperationException(
                "Pre-terminal restart was not preserved as a rejected Application result.");
        }

        if (StringComparer.Ordinal.Equals(
                replayScenario,
                "existing-target-race"))
        {
            using (var sentinel = new FileStream(
                       replayOutputPath,
                       FileMode.CreateNew,
                       FileAccess.Write,
                       FileShare.None))
            {
                sentinel.Write("do-not-overwrite\n"u8);
                sentinel.Flush(flushToDisk: true);
            }
        }

        if (StringComparer.Ordinal.Equals(replayScenario, "win"))
        {
            RunHumanShapedVictory(first);
            RequireTerminal(first, "win", "white_king_captured");
        }
        else
        {
            RunTurnLimitPath(first);
            RequireTerminal(first, "loss", "black_king_captured");
        }

        var evidence = first.ReplayEvidence
            ?? throw new InvalidOperationException(
                "Replay evidence was not sealed at the first terminal transition.");
        if (!evidence.Verified)
        {
            if (StringComparer.Ordinal.Equals(
                    replayScenario,
                    "existing-target-race"))
            {
                var directory = Path.GetDirectoryName(replayOutputPath)
                    ?? throw new InvalidOperationException(
                        "Replay race smoke path has no parent directory.");
                if (!StringComparer.Ordinal.Equals(
                        File.ReadAllText(replayOutputPath),
                        "do-not-overwrite\n") ||
                    Directory.EnumerateFiles(
                            directory,
                            ".igorogue-replay-*.tmp",
                            SearchOption.TopDirectoryOnly)
                        .Any() ||
                    !StringComparer.Ordinal.Equals(
                        evidence.ReasonId,
                        "artifact_io_failure"))
                {
                    throw new InvalidOperationException(
                        "Replay race smoke changed the target or leaked its owned temp file.");
                }
            }

            return new CoreDuelHeadlessSmokeResult(
                first.Session.State.Checksum,
                evidence);
        }

        RequireFixedSeedReplayEvidence(evidence, replayScenario);

        using (var source = new FileStream(
                   evidence.Path,
                   FileMode.Open,
                   FileAccess.Read,
                   FileShare.Read))
        {
            var document = BattleReplaySerializerV3.Load(source);
            if (document.Attempts.Count == 0 ||
                document.Attempts[0].Accepted ||
                !StringComparer.Ordinal.Equals(
                    document.Attempts[0].ReasonId,
                    "battle_not_terminal") ||
                !document.Attempts.Any(attempt =>
                    StringComparer.Ordinal.Equals(
                        attempt.CommandType,
                        "battle.end_player_turn")) ||
                !document.Attempts.Any(attempt =>
                    StringComparer.Ordinal.Equals(
                        attempt.CommandType,
                        "battle.resolve_bandit_enemy_action")))
            {
                throw new InvalidOperationException(
                    "Replay evidence omitted a rejected attempt, End Turn, or Bandit result.");
            }
        }

        var sealedBytes = File.ReadAllBytes(evidence.Path);
        VerifyRestart(first);
        var restartedChecksum = first.Session.State.Checksum;
        if (!ReferenceEquals(evidence, first.ReplayEvidence))
        {
            throw new InvalidOperationException(
                "Restart replaced the launch-scoped sealed replay evidence.");
        }

        RunTurnLimitPath(first);
        if (!ReferenceEquals(evidence, first.ReplayEvidence) ||
            !sealedBytes.AsSpan().SequenceEqual(File.ReadAllBytes(evidence.Path)))
        {
            throw new InvalidOperationException(
                "A post-restart terminal changed or regenerated the sealed replay artifact.");
        }

        return new CoreDuelHeadlessSmokeResult(restartedChecksum, evidence);
    }

    private static void RunDefaultHeadlessBattle(CoreDuelGameHost host)
    {
        var playedCard = false;
        foreach (var card in host.Battle.HandCards)
        {
            if (host.SelectCard(card.InstanceId) &&
                host.SelectedCard?.LegalCandidates.FirstOrDefault() is { } candidate)
            {
                if (!host.TryPlaySelectedCard(candidate.Target))
                {
                    throw new InvalidOperationException(
                        $"Graybox legal preview command was rejected: {host.LastActionReasonId}.");
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

        if (!host.EndTurnAndResolveEnemy())
        {
            throw new InvalidOperationException(
                $"Graybox turn smoke was rejected: {host.LastActionReasonId}.");
        }

        if (!host.Battle.IsTerminal &&
            !StringComparer.Ordinal.Equals(host.Battle.PhaseId, "player_action"))
        {
            throw new InvalidOperationException(
                $"Graybox turn smoke ended in unexpected phase: {host.Battle.PhaseId}.");
        }

        for (var turn = 1;
             !host.Battle.IsTerminal && turn < host.catalog.BattleSetup.PlayerTurnLimit;
             turn++)
        {
            if (!host.EndTurnAndResolveEnemy())
            {
                throw new InvalidOperationException(
                    $"Graybox terminal smoke was rejected: {host.LastActionReasonId}.");
            }
        }

        if (!host.Battle.IsTerminal)
        {
            throw new InvalidOperationException(
                "Graybox battle did not reach its content-owned player-turn limit.");
        }
    }

    private static void RunHumanShapedVictory(CoreDuelGameHost host)
    {
        Play(host, "card_extend", 2, 4);
        Play(host, "card_extend", 2, 5);
        Play(host, "card_basic_stone", 2, 6);
        EndTurn(host);

        Play(host, "card_lure_stone", 3, 6);
        Play(host, "card_basic_stone", 3, 7);
        Play(host, "card_contact", 4, 7);
        Play(host, "card_contact", 5, 7);
        EndTurn(host);

        Play(host, "card_extend", 2, 7);
        Play(host, "card_extend", 3, 5);
        Play(host, "card_contact", 4, 5);
        EndTurn(host);

        Play(host, "card_basic_stone", 4, 4);
        Play(host, "card_lure_stone", 5, 4);
        Play(host, "card_contact", 6, 4);
        Play(host, "card_basic_stone", 7, 4);
        EndTurn(host);

        Play(host, "card_extend", 7, 3);
        Play(host, "card_extend", 7, 2);
        Play(host, "card_basic_stone", 6, 3);
        Play(host, "card_basic_stone", 1, 7);
        Play(host, "card_basic_stone", 6, 2);
        EndTurn(host);

        Play(host, "card_basic_stone", 7, 7);
    }

    private static void RunTurnLimitPath(CoreDuelGameHost host)
    {
        for (var turn = 0;
             !host.Battle.IsTerminal &&
             turn <= host.catalog.BattleSetup.PlayerTurnLimit;
             turn++)
        {
            EndTurn(host);
        }

        if (!host.Battle.IsTerminal)
        {
            throw new InvalidOperationException(
                "Headless loss path did not reach a terminal boundary.");
        }
    }

    private static void Play(
        CoreDuelGameHost host,
        string contentId,
        int x,
        int y)
    {
        var card = host.Battle.HandCards.FirstOrDefault(candidate =>
            StringComparer.Ordinal.Equals(candidate.ContentId, contentId))
            ?? throw new InvalidOperationException(
                $"Headless victory hand does not contain {contentId}.");
        if (!host.SelectCard(card.InstanceId))
        {
            throw new InvalidOperationException(
                $"Headless victory could not select {contentId}: {host.LastActionReasonId}.");
        }

        var point = host.Battle.BoardPoints
            .Select(candidate => candidate.Point)
            .Single(candidate => candidate.X == x && candidate.Y == y);
        if (host.CandidateAt(point) is null || !host.TryPlaySelectedCard(point))
        {
            throw new InvalidOperationException(
                $"Headless victory rejected {contentId} at ({x},{y}): {host.LastActionReasonId}.");
        }
    }

    private static void EndTurn(CoreDuelGameHost host)
    {
        if (!host.EndTurnAndResolveEnemy())
        {
            throw new InvalidOperationException(
                $"Headless End Turn was rejected: {host.LastActionReasonId}.");
        }
    }

    private static void RequireTerminal(
        CoreDuelGameHost host,
        string outcomeId,
        string endReasonId)
    {
        if (!host.Battle.IsTerminal ||
            !StringComparer.Ordinal.Equals(host.Battle.OutcomeId, outcomeId) ||
            !StringComparer.Ordinal.Equals(host.Battle.EndReasonId, endReasonId))
        {
            throw new InvalidOperationException(
                $"Headless terminal mismatch: {host.Battle.OutcomeId}/{host.Battle.EndReasonId}.");
        }
    }

    private static void RequireFixedSeedReplayEvidence(
        CoreDuelReplayEvidence evidence,
        string replayScenario)
    {
        const string contentHash =
            "sha256:aa26362f6c4b1cdc9c8dc9336654bd20fe5379f622eef3fa992257db62d86832";
        if (evidence.Seed != 39039L ||
            !StringComparer.Ordinal.Equals(evidence.GameVersion, "v0.2.10") ||
            !StringComparer.Ordinal.Equals(evidence.ContentHash, contentHash))
        {
            return;
        }

        const string initialState =
            "f1f718f000f63cf284c38284295b0d69c5a8ff3b512e3c0a50569ef1e9ab3be8";
        const string initialLog =
            "9cbd4fb1c03a5ffa33f24c9f7aea52d62e9c46e3c222667b4d0c0aabb52f1efa";
        var expected = StringComparer.Ordinal.Equals(replayScenario, "win")
            ? new
            {
                FinalState = "1fc97bb91f9be10b71d5370053580f051a499fef459741b674c427b85a743706",
                FinalLog = "17a22ad2716a70f105d3bc0d4be4d4f5ef5e780aa7c009d98eff071cf7a18622",
                Attempts = "91d7c0c375d5e757ea5a27078b647d301fcdce8afc97efc3ae0b0b7c81a786b3",
                Document = "253f4dd78436f5d6f67e2d16b74a86c521b14aec4224093d24f6361049d65028",
                Artifact = "6ff9aae41202c49ff136e7962af9b4243889d3e9a036b0b8a74fb4d1a8b6c6be",
                Bytes = 30140L,
            }
            : new
            {
                FinalState = "008f3d0865cc83ebad869706ba0885d7231cf422d1dc95b940b1ece3d93f4711",
                FinalLog = "f77e02965374c6266b9f92e96184a58ff88956806b56eb6cff00c0310bd34339",
                Attempts = "10dda0caff92aed9879578635f9fae253788b1c31a10dc3ae5ea9dcd2f684c21",
                Document = "3955c87d149baf1ed388eec247ddca127cea93c5607f94a69895d371496ab0ca",
                Artifact = "2f81068272beaa4b6f4f6c2e7b77884f16f24606d35e9c6961da22fe27c304b7",
                Bytes = 28814L,
            };
        if (evidence.AttemptCount != 31 ||
            evidence.AcceptedCount != 30 ||
            !StringComparer.Ordinal.Equals(evidence.InitialStateChecksum, initialState) ||
            !StringComparer.Ordinal.Equals(evidence.InitialLogChecksum, initialLog) ||
            !StringComparer.Ordinal.Equals(evidence.FinalStateChecksum, expected.FinalState) ||
            !StringComparer.Ordinal.Equals(evidence.FinalLogChecksum, expected.FinalLog) ||
            !StringComparer.Ordinal.Equals(evidence.AttemptsChecksum, expected.Attempts) ||
            !StringComparer.Ordinal.Equals(evidence.DocumentChecksum, expected.Document) ||
            !StringComparer.Ordinal.Equals(evidence.ArtifactSha256, expected.Artifact) ||
            evidence.ArtifactBytes != expected.Bytes ||
            !StringComparer.Ordinal.Equals(
                evidence.ReplayedStateChecksum,
                expected.FinalState) ||
            !StringComparer.Ordinal.Equals(
                evidence.ReplayedLogChecksum,
                expected.FinalLog))
        {
            throw new InvalidOperationException(
                $"Seed 39039 {replayScenario} Replay V3 evidence changed.");
        }
    }

    private static void VerifyRestart(CoreDuelGameHost host)
    {
        if (!host.Restart() ||
            host.Battle.IsTerminal ||
            host.Battle.RestartCount is null or < 1 ||
            host.Battle.PlayerTurnIndex != 1 ||
            !StringComparer.Ordinal.Equals(host.Battle.PhaseId, "player_action") ||
            host.Battle.Qi != host.catalog.SystemPolicy.BaseQi ||
            host.Battle.HandCards.Count != host.catalog.SystemPolicy.BaseDraw ||
            !RestartedBoardMatchesTypedSetup(host))
        {
            throw new InvalidOperationException(
                $"Graybox restart smoke failed: {host.LastActionReasonId}.");
        }
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

    private CoreDuelBattleCommandResult ExecuteAndObserve(IBattleCommand command)
    {
        var result = CoreDuelBattleStateMachine.Execute(Session, command);
        replayRecorder.Observe(result);
        Session = result.SessionAfter;
        return result;
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

public sealed record CoreDuelHeadlessSmokeResult(
    string Checksum,
    CoreDuelReplayEvidence? ReplayEvidence);
