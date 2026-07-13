using System.Collections.ObjectModel;

using Igorogue.Domain.Board;
using Igorogue.Domain.Cards;
using Igorogue.Domain.Enemies;
using Igorogue.Domain.Facilities;

namespace Igorogue.Application.Battle;

public sealed class CoreDuelBattlePreviewRequest
{
    public CoreDuelBattlePreviewRequest(
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

    public string ExpectedStateChecksum { get; }

    public string ExpectedLogChecksum { get; }
}

public sealed class CoreDuelCardPreviewRequest
{
    public CoreDuelCardPreviewRequest(
        string expectedStateChecksum,
        string expectedLogChecksum,
        string cardInstanceId)
    {
        ExpectedStateChecksum = BattleCommandValidation.CanonicalChecksum(
            expectedStateChecksum,
            nameof(expectedStateChecksum));
        ExpectedLogChecksum = BattleCommandValidation.CanonicalChecksum(
            expectedLogChecksum,
            nameof(expectedLogChecksum));
        CardInstanceId = BattleCommandValidation.StableId(
            cardInstanceId,
            nameof(cardInstanceId));
    }

    public string ExpectedStateChecksum { get; }

    public string ExpectedLogChecksum { get; }

    public string CardInstanceId { get; }
}

public sealed class CoreDuelEnemyTargetPreview
{
    private readonly ReadOnlyCollection<CanonicalPoint> currentStonePointView;

    internal CoreDuelEnemyTargetPreview(
        string kindId,
        string colorId,
        CanonicalPoint anchor,
        bool anchorResolvesToCurrentGroup,
        CanonicalPoint? currentGroupAnchor,
        IEnumerable<CanonicalPoint> currentStonePoints)
    {
        KindId = kindId;
        ColorId = colorId;
        Anchor = anchor;
        AnchorResolvesToCurrentGroup = anchorResolvesToCurrentGroup;
        CurrentGroupAnchor = currentGroupAnchor;
        currentStonePointView = Array.AsReadOnly(currentStonePoints.ToArray());
    }

    public string KindId { get; }

    public string ColorId { get; }

    public CanonicalPoint Anchor { get; }

    public bool AnchorResolvesToCurrentGroup { get; }

    public CanonicalPoint? CurrentGroupAnchor { get; }

    public IReadOnlyList<CanonicalPoint> CurrentStonePoints => currentStonePointView;
}

public sealed class CoreDuelIntentPreview
{
    private readonly ReadOnlyCollection<CanonicalPoint> alternatePointView;

    internal CoreDuelIntentPreview(
        bool isCounterattackAction,
        string intentId,
        CoreDuelEnemyTargetPreview? target,
        CanonicalPoint? primaryPoint,
        IEnumerable<CanonicalPoint> alternatePoints,
        bool retargetable,
        string plannedFromStateChecksum,
        string planChecksum)
    {
        IsCounterattackAction = isCounterattackAction;
        IntentId = intentId;
        Target = target;
        PrimaryPoint = primaryPoint;
        alternatePointView = Array.AsReadOnly(alternatePoints.ToArray());
        Retargetable = retargetable;
        PlannedFromStateChecksum = plannedFromStateChecksum;
        PlanChecksum = planChecksum;
    }

    public bool IsCounterattackAction { get; }

    public bool IsPass => Target is null;

    public string IntentId { get; }

    public CoreDuelEnemyTargetPreview? Target { get; }

    public CanonicalPoint? PrimaryPoint { get; }

    public IReadOnlyList<CanonicalPoint> AlternatePoints => alternatePointView;

    public bool Retargetable { get; }

    public string PlannedFromStateChecksum { get; }

    public string PlanChecksum { get; }
}

public sealed class CoreDuelMandatoryOverridePreview
{
    internal CoreDuelMandatoryOverridePreview(
        string kindId,
        string reasonId,
        string executedIntentId,
        CanonicalPoint? point,
        CoreDuelEnemyTargetPreview? target)
    {
        KindId = kindId;
        ReasonId = reasonId;
        ExecutedIntentId = executedIntentId;
        Point = point;
        Target = target;
    }

    public string KindId { get; }

    public bool HasOverride => !StringComparer.Ordinal.Equals(KindId, "none");

    public string ReasonId { get; }

    public string ExecutedIntentId { get; }

    public CanonicalPoint? Point { get; }

    public CoreDuelEnemyTargetPreview? Target { get; }
}

public sealed class CoreDuelGroupPreview
{
    private readonly ReadOnlyCollection<CanonicalPoint> stonePointView;
    private readonly ReadOnlyCollection<CanonicalPoint> realLibertyPointView;

    internal CoreDuelGroupPreview(
        string colorId,
        CanonicalPoint anchor,
        IEnumerable<CanonicalPoint> stonePoints,
        IEnumerable<CanonicalPoint> realLibertyPoints,
        bool containsKing,
        int timedLibertyAmount,
        int continuousLibertyAmount,
        int effectiveLibertyCount)
    {
        ColorId = colorId;
        Anchor = anchor;
        stonePointView = Array.AsReadOnly(stonePoints.ToArray());
        realLibertyPointView = Array.AsReadOnly(realLibertyPoints.ToArray());
        ContainsKing = containsKing;
        TimedLibertyAmount = timedLibertyAmount;
        ContinuousLibertyAmount = continuousLibertyAmount;
        EffectiveLibertyCount = effectiveLibertyCount;
    }

    public string ColorId { get; }

    public CanonicalPoint Anchor { get; }

    public IReadOnlyList<CanonicalPoint> StonePoints => stonePointView;

    public IReadOnlyList<CanonicalPoint> RealLibertyPoints => realLibertyPointView;

    public bool ContainsKing { get; }

    public int RealLibertyCount => realLibertyPointView.Count;

    public int TimedLibertyAmount { get; }

    public int ContinuousLibertyAmount { get; }

    public int EffectiveLibertyCount { get; }

    public bool IsAtari => EffectiveLibertyCount == 1;
}

public sealed class CoreDuelCapturedGroupPreview
{
    private readonly ReadOnlyCollection<CanonicalPoint> stonePointView;

    internal CoreDuelCapturedGroupPreview(
        string colorId,
        CanonicalPoint anchor,
        IEnumerable<CanonicalPoint> stonePoints,
        bool containsKing)
    {
        ColorId = colorId;
        Anchor = anchor;
        stonePointView = Array.AsReadOnly(stonePoints.ToArray());
        ContainsKing = containsKing;
    }

    public string ColorId { get; }

    public CanonicalPoint Anchor { get; }

    public IReadOnlyList<CanonicalPoint> StonePoints => stonePointView;

    public int StoneCount => stonePointView.Count;

    public bool ContainsKing { get; }
}

public sealed class CoreDuelTerritoryPointDeltaPreview
{
    internal CoreDuelTerritoryPointDeltaPreview(
        CanonicalPoint point,
        string ownerBeforeId,
        string ownerAfterId)
    {
        Point = point;
        OwnerBeforeId = ownerBeforeId;
        OwnerAfterId = ownerAfterId;
    }

    public CanonicalPoint Point { get; }

    public string OwnerBeforeId { get; }

    public string OwnerAfterId { get; }
}

public sealed class CoreDuelFacilityPreview
{
    private readonly ReadOnlyCollection<string> explicitDisableSourceView;

    internal CoreDuelFacilityPreview(
        string instanceId,
        string contentId,
        string ownerId,
        CanonicalPoint point,
        long buildSequence,
        IEnumerable<string> explicitDisableSources,
        bool isActive,
        string operatingReasonId)
    {
        InstanceId = instanceId;
        ContentId = contentId;
        OwnerId = ownerId;
        Point = point;
        BuildSequence = buildSequence;
        explicitDisableSourceView = Array.AsReadOnly(explicitDisableSources.ToArray());
        IsActive = isActive;
        OperatingReasonId = operatingReasonId;
    }

    public string InstanceId { get; }

    public string ContentId { get; }

    public string OwnerId { get; }

    public CanonicalPoint Point { get; }

    public long BuildSequence { get; }

    public IReadOnlyList<string> ExplicitDisableSources => explicitDisableSourceView;

    public bool IsActive { get; }

    public string OperatingReasonId { get; }
}

public sealed class CoreDuelFacilityDeltaPreview
{
    internal CoreDuelFacilityDeltaPreview(
        string instanceId,
        CoreDuelFacilityPreview? before,
        CoreDuelFacilityPreview? after)
    {
        InstanceId = instanceId;
        Before = before;
        After = after;
    }

    public string InstanceId { get; }

    public CoreDuelFacilityPreview? Before { get; }

    public CoreDuelFacilityPreview? After { get; }

    public bool WasBuilt => Before is null && After is not null;

    public bool WasDestroyed => Before is not null && After is null;
}

public sealed class CoreDuelBlackKingRiskPreview
{
    internal CoreDuelBlackKingRiskPreview(
        bool isCaptured,
        CoreDuelGroupPreview? group,
        bool hasMandatoryLethalOverride,
        string reasonId,
        CanonicalPoint? threatPoint)
    {
        IsCaptured = isCaptured;
        Group = group;
        HasMandatoryLethalOverride = hasMandatoryLethalOverride;
        ReasonId = reasonId;
        ThreatPoint = threatPoint;
    }

    public bool IsCaptured { get; }

    public CoreDuelGroupPreview? Group { get; }

    public bool HasMandatoryLethalOverride { get; }

    public string ReasonId { get; }

    public CanonicalPoint? ThreatPoint { get; }
}

public sealed class CoreDuelAcceptedCardPlayPreview
{
    private readonly ReadOnlyCollection<CoreDuelCapturedGroupPreview> capturedGroupView;
    private readonly ReadOnlyCollection<CoreDuelGroupPreview> resultingGroupView;
    private readonly ReadOnlyCollection<CoreDuelGroupPreview> newEnemyAtariGroupView;
    private readonly ReadOnlyCollection<CoreDuelTerritoryPointDeltaPreview> territoryDeltaView;
    private readonly ReadOnlyCollection<CoreDuelFacilityDeltaPreview> facilityDeltaView;

    internal CoreDuelAcceptedCardPlayPreview(
        string stateChecksum,
        string logChecksum,
        bool isTerminal,
        string outcomeId,
        string endReasonId,
        IEnumerable<CoreDuelCapturedGroupPreview> capturedGroups,
        CoreDuelGroupPreview? resultingTargetGroup,
        IEnumerable<CoreDuelGroupPreview> resultingGroups,
        IEnumerable<CoreDuelGroupPreview> newEnemyAtariGroups,
        IEnumerable<CoreDuelTerritoryPointDeltaPreview> territoryDeltas,
        int blackTerritoryPointDelta,
        int whiteTerritoryPointDelta,
        IEnumerable<CoreDuelFacilityDeltaPreview> facilityDeltas,
        CoreDuelIntentPreview? displayedIntent,
        CoreDuelMandatoryOverridePreview mandatoryOverride,
        CoreDuelBlackKingRiskPreview blackKingRisk)
    {
        StateChecksum = stateChecksum;
        LogChecksum = logChecksum;
        IsTerminal = isTerminal;
        OutcomeId = outcomeId;
        EndReasonId = endReasonId;
        capturedGroupView = Array.AsReadOnly(capturedGroups.ToArray());
        ResultingTargetGroup = resultingTargetGroup;
        resultingGroupView = Array.AsReadOnly(resultingGroups.ToArray());
        newEnemyAtariGroupView = Array.AsReadOnly(newEnemyAtariGroups.ToArray());
        territoryDeltaView = Array.AsReadOnly(territoryDeltas.ToArray());
        BlackTerritoryPointDelta = blackTerritoryPointDelta;
        WhiteTerritoryPointDelta = whiteTerritoryPointDelta;
        facilityDeltaView = Array.AsReadOnly(facilityDeltas.ToArray());
        DisplayedIntent = displayedIntent;
        MandatoryOverride = mandatoryOverride;
        BlackKingRisk = blackKingRisk;
    }

    public string StateChecksum { get; }

    public string LogChecksum { get; }

    public bool IsTerminal { get; }

    public string OutcomeId { get; }

    public string EndReasonId { get; }

    public IReadOnlyList<CoreDuelCapturedGroupPreview> CapturedGroups => capturedGroupView;

    public CoreDuelGroupPreview? ResultingTargetGroup { get; }

    public IReadOnlyList<CoreDuelGroupPreview> ResultingGroups => resultingGroupView;

    public IReadOnlyList<CoreDuelGroupPreview> NewEnemyAtariGroups =>
        newEnemyAtariGroupView;

    public IReadOnlyList<CoreDuelTerritoryPointDeltaPreview> TerritoryDeltas =>
        territoryDeltaView;

    public int BlackTerritoryPointDelta { get; }

    public int WhiteTerritoryPointDelta { get; }

    public IReadOnlyList<CoreDuelFacilityDeltaPreview> FacilityDeltas => facilityDeltaView;

    public CoreDuelIntentPreview? DisplayedIntent { get; }

    public CoreDuelMandatoryOverridePreview MandatoryOverride { get; }

    public CoreDuelBlackKingRiskPreview BlackKingRisk { get; }
}

public sealed class CoreDuelCardCandidatePreview
{
    internal CoreDuelCardCandidatePreview(
        PlayCardCommand commitCommand,
        string placementModeId,
        bool accepted,
        string reasonId,
        CoreDuelAcceptedCardPlayPreview? acceptedResult)
    {
        CommitCommand = commitCommand;
        PlacementModeId = placementModeId;
        Accepted = accepted;
        ReasonId = reasonId;
        AcceptedResult = acceptedResult;
    }

    public PlayCardCommand CommitCommand { get; }

    public CanonicalPoint Target => CommitCommand.Target;

    public string PlacementModeId { get; }

    public bool Accepted { get; }

    public string ReasonId { get; }

    public CoreDuelAcceptedCardPlayPreview? AcceptedResult { get; }
}

public sealed class CoreDuelCardPreviewResult
{
    private readonly ReadOnlyCollection<CoreDuelCardCandidatePreview> candidateView;
    private readonly ReadOnlyCollection<CoreDuelCardCandidatePreview> legalCandidateView;

    internal CoreDuelCardPreviewResult(
        bool accepted,
        string reasonId,
        string sourceStateChecksum,
        string sourceLogChecksum,
        string cardInstanceId,
        string? cardContentId,
        IEnumerable<CoreDuelCardCandidatePreview> candidates,
        CoreDuelIntentPreview? normalIntent,
        CoreDuelIntentPreview? bonusIntent,
        CoreDuelIntentPreview? displayedIntent,
        CoreDuelMandatoryOverridePreview? mandatoryOverride,
        CoreDuelBlackKingRiskPreview? blackKingRisk)
    {
        Accepted = accepted;
        ReasonId = reasonId;
        SourceStateChecksum = sourceStateChecksum;
        SourceLogChecksum = sourceLogChecksum;
        CardInstanceId = cardInstanceId;
        CardContentId = cardContentId;
        var materialized = candidates.ToArray();
        candidateView = Array.AsReadOnly(materialized);
        legalCandidateView = Array.AsReadOnly(materialized.Where(candidate => candidate.Accepted).ToArray());
        NormalIntent = normalIntent;
        BonusIntent = bonusIntent;
        DisplayedIntent = displayedIntent;
        MandatoryOverride = mandatoryOverride;
        BlackKingRisk = blackKingRisk;
    }

    public bool Accepted { get; }

    public string ReasonId { get; }

    public string SourceStateChecksum { get; }

    public string SourceLogChecksum { get; }

    public string CardInstanceId { get; }

    public string? CardContentId { get; }

    public IReadOnlyList<CoreDuelCardCandidatePreview> Candidates => candidateView;

    public IReadOnlyList<CoreDuelCardCandidatePreview> LegalCandidates => legalCandidateView;

    public CoreDuelIntentPreview? NormalIntent { get; }

    public CoreDuelIntentPreview? BonusIntent { get; }

    public CoreDuelIntentPreview? DisplayedIntent { get; }

    public CoreDuelMandatoryOverridePreview? MandatoryOverride { get; }

    public CoreDuelBlackKingRiskPreview? BlackKingRisk { get; }
}

public sealed class CoreDuelBattlePreviewResult
{
    private readonly ReadOnlyCollection<CoreDuelBoardPointPreview> boardPointView;
    private readonly ReadOnlyCollection<CoreDuelGroupPreview> groupView;
    private readonly ReadOnlyCollection<CoreDuelCardInstancePreview> handCardView;

    internal CoreDuelBattlePreviewResult(
        bool accepted,
        string reasonId,
        string sourceStateChecksum,
        string sourceLogChecksum,
        string? phaseId,
        int? playerTurnIndex,
        int? restartCount,
        string? outcomeId,
        string? endReasonId,
        int? qi,
        IEnumerable<CoreDuelBoardPointPreview> boardPoints,
        IEnumerable<CoreDuelGroupPreview> groups,
        IEnumerable<CoreDuelCardInstancePreview> handCards,
        int? drawPileCount,
        int? resolvingCardCount,
        int? discardPileCount,
        int? exhaustPileCount,
        CoreDuelIntentPreview? normalIntent,
        CoreDuelIntentPreview? bonusIntent,
        CoreDuelIntentPreview? displayedIntent,
        CoreDuelMandatoryOverridePreview? mandatoryOverride,
        CoreDuelBlackKingRiskPreview? blackKingRisk)
    {
        Accepted = accepted;
        ReasonId = reasonId;
        SourceStateChecksum = sourceStateChecksum;
        SourceLogChecksum = sourceLogChecksum;
        PhaseId = phaseId;
        PlayerTurnIndex = playerTurnIndex;
        RestartCount = restartCount;
        OutcomeId = outcomeId;
        EndReasonId = endReasonId;
        Qi = qi;
        boardPointView = Array.AsReadOnly(boardPoints.ToArray());
        groupView = Array.AsReadOnly(groups.ToArray());
        handCardView = Array.AsReadOnly(handCards.ToArray());
        DrawPileCount = drawPileCount;
        ResolvingCardCount = resolvingCardCount;
        DiscardPileCount = discardPileCount;
        ExhaustPileCount = exhaustPileCount;
        NormalIntent = normalIntent;
        BonusIntent = bonusIntent;
        DisplayedIntent = displayedIntent;
        MandatoryOverride = mandatoryOverride;
        BlackKingRisk = blackKingRisk;
    }

    public bool Accepted { get; }

    public string ReasonId { get; }

    public string SourceStateChecksum { get; }

    public string SourceLogChecksum { get; }

    public string? PhaseId { get; }

    public int? PlayerTurnIndex { get; }

    public int? RestartCount { get; }

    public string? OutcomeId { get; }

    public string? EndReasonId { get; }

    public int? Qi { get; }

    public IReadOnlyList<CoreDuelBoardPointPreview> BoardPoints => boardPointView;

    public IReadOnlyList<CoreDuelGroupPreview> Groups => groupView;

    public IReadOnlyList<CoreDuelCardInstancePreview> HandCards => handCardView;

    public int? DrawPileCount { get; }

    public int? ResolvingCardCount { get; }

    public int? DiscardPileCount { get; }

    public int? ExhaustPileCount { get; }

    public bool IsTerminal => StringComparer.Ordinal.Equals(PhaseId, "ended");

    public CoreDuelIntentPreview? NormalIntent { get; }

    public CoreDuelIntentPreview? BonusIntent { get; }

    public CoreDuelIntentPreview? DisplayedIntent { get; }

    public CoreDuelMandatoryOverridePreview? MandatoryOverride { get; }

    public CoreDuelBlackKingRiskPreview? BlackKingRisk { get; }
}

public static class CoreDuelBattlePreviewQuery
{
    private static readonly PlacementModeCandidate[] StonePlacementModes =
    [
        new(StoneCardPlacementMode.Frontline, "frontline"),
        new(StoneCardPlacementMode.Contact, "contact"),
        new(StoneCardPlacementMode.TerminalCapture, "terminal_capture"),
    ];

    private static readonly PlacementModeCandidate[] TargetOnlyMode =
    [
        new(null, "none"),
    ];

    public static CoreDuelBattlePreviewResult Evaluate(
        CoreDuelBattleSession session,
        CoreDuelBattlePreviewRequest request)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(request);
        if (!StringComparer.Ordinal.Equals(
                request.ExpectedStateChecksum,
                session.State.Checksum))
        {
            return RejectBattle(session, "stale_state");
        }

        if (!StringComparer.Ordinal.Equals(
                request.ExpectedLogChecksum,
                session.CommandLog.CurrentChecksum))
        {
            return RejectBattle(session, "stale_session");
        }

        var analysis = Analyze(session.State);
        var normalIntent = ProjectIntent(
            session.State,
            analysis.Groups,
            session.State.NormalPlan,
            isCounterattackAction: false);
        var bonusIntent = ProjectIntent(
            session.State,
            analysis.Groups,
            session.State.BonusPlan,
            isCounterattackAction: true);
        var mandatoryOverride = ProjectMandatoryOverride(session, analysis.Groups);
        var deck = session.State.CardTurnState.Deck;
        return new CoreDuelBattlePreviewResult(
            true,
            "accepted",
            session.State.Checksum,
            session.CommandLog.CurrentChecksum,
            session.State.BattleState.PhaseId,
            session.State.BattleState.PlayerTurnIndex,
            session.State.RestartCount,
            session.State.BattleState.OutcomeId,
            session.State.BattleState.EndReasonId,
            session.State.CardTurnState.Qi,
            ProjectBoardPoints(session.State),
            analysis.Groups.Groups.Select(group =>
                ProjectGroup(group, analysis.EffectiveLiberties)),
            deck.Hand.Select(ProjectCard),
            deck.DrawPile.Count,
            deck.Resolving.Count,
            deck.DiscardPile.Count,
            deck.ExhaustPile.Count,
            normalIntent,
            bonusIntent,
            DisplayedIntent(session.State, normalIntent, bonusIntent),
            mandatoryOverride,
            ProjectBlackKingRisk(session.State, analysis, mandatoryOverride));
    }

    public static CoreDuelCardPreviewResult Evaluate(
        CoreDuelBattleSession session,
        CoreDuelCardPreviewRequest request)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(request);
        if (!StringComparer.Ordinal.Equals(
                request.ExpectedStateChecksum,
                session.State.Checksum))
        {
            return Reject(session, request, "stale_state");
        }

        if (!StringComparer.Ordinal.Equals(
                request.ExpectedLogChecksum,
                session.CommandLog.CurrentChecksum))
        {
            return Reject(session, request, "stale_session");
        }

        if (session.State.IsTerminal)
        {
            return Reject(session, request, "battle_terminal");
        }

        if (session.State.BattleState.Phase != BattlePhase.PlayerAction)
        {
            return Reject(session, request, "wrong_phase");
        }

        var card = session.State.CardTurnState.Deck.Hand.FirstOrDefault(candidate =>
            StringComparer.Ordinal.Equals(candidate.InstanceId, request.CardInstanceId));
        if (card is null)
        {
            return Reject(session, request, "card_not_in_hand");
        }

        var modes = ModesFor(session.State, card.ContentId);
        if (modes is null)
        {
            return Reject(session, request, "card_content_mismatch");
        }

        var sourceAnalysis = Analyze(session.State);
        var normalIntent = ProjectIntent(
            session.State,
            sourceAnalysis.Groups,
            session.State.NormalPlan,
            isCounterattackAction: false);
        var bonusIntent = ProjectIntent(
            session.State,
            sourceAnalysis.Groups,
            session.State.BonusPlan,
            isCounterattackAction: true);
        var displayedIntent = DisplayedIntent(session.State, normalIntent, bonusIntent);
        var mandatoryOverride = ProjectMandatoryOverride(session, sourceAnalysis.Groups);
        var blackKingRisk = ProjectBlackKingRisk(
            session.State,
            sourceAnalysis,
            mandatoryOverride);
        var candidates = new List<CoreDuelCardCandidatePreview>(
            session.State.BattleState.Board.Geometry.PointCount * modes.Length);

        foreach (var point in session.State.BattleState.Board.Geometry.CanonicalPoints)
        {
            foreach (var mode in modes)
            {
                var command = mode.Mode is StoneCardPlacementMode placementMode
                    ? new PlayCardCommand(
                        session.State.Checksum,
                        session.CommandLog.CurrentChecksum,
                        card.InstanceId,
                        point,
                        placementMode)
                    : new PlayCardCommand(
                        session.State.Checksum,
                        session.CommandLog.CurrentChecksum,
                        card.InstanceId,
                        point);
                var result = CoreDuelBattleStateMachine.Execute(session, command);
                candidates.Add(new CoreDuelCardCandidatePreview(
                    command,
                    mode.Id,
                    result.Accepted,
                    result.ReasonId,
                    result.Accepted
                        ? ProjectAccepted(
                            session,
                            result,
                            command,
                            sourceAnalysis)
                        : null));
            }
        }

        return new CoreDuelCardPreviewResult(
            true,
            "accepted",
            session.State.Checksum,
            session.CommandLog.CurrentChecksum,
            card.InstanceId,
            card.ContentId,
            candidates,
            normalIntent,
            bonusIntent,
            displayedIntent,
            mandatoryOverride,
            blackKingRisk);
    }

    private static PlacementModeCandidate[]? ModesFor(
        CoreDuelBattleState state,
        string contentId)
    {
        if (state.Bootstrap.StoneDefinitions.TryDefinition(contentId, out _))
        {
            return StonePlacementModes;
        }

        if (StringComparer.Ordinal.Equals(
                contentId,
                state.Bootstrap.ReinforceDefinition.ContentId) ||
            StringComparer.Ordinal.Equals(
                contentId,
                state.Bootstrap.DevelopmentDefinition.ContentId))
        {
            return TargetOnlyMode;
        }

        return null;
    }

    private static CoreDuelAcceptedCardPlayPreview ProjectAccepted(
        CoreDuelBattleSession source,
        CoreDuelBattleCommandResult result,
        PlayCardCommand command,
        StateAnalysis analysisBefore)
    {
        var after = result.SessionAfter;
        var stateAfter = after.State;
        var analysisAfter = Analyze(stateAfter);
        var resultingGroup = analysisAfter.Groups.GroupAt(command.Target);
        var mandatoryOverride = ProjectMandatoryOverride(after, analysisAfter.Groups);
        var blackKingRisk = ProjectBlackKingRisk(
            stateAfter,
            analysisAfter,
            mandatoryOverride);
        var territoryDeltas = TerritoryDeltas(
            source.State.BattleState.TerritoryAnalysis,
            stateAfter.BattleState.TerritoryAnalysis);

        return new CoreDuelAcceptedCardPlayPreview(
            result.StateChecksum,
            result.LogChecksum,
            stateAfter.IsTerminal,
            stateAfter.BattleState.OutcomeId,
            stateAfter.BattleState.EndReasonId,
            result.OrderedFacts
                .OfType<GroupCapturedFact>()
                .Select(fact => ProjectCapturedGroup(fact.CapturedGroup)),
            resultingGroup is null
                ? null
                : ProjectGroup(resultingGroup, analysisAfter.EffectiveLiberties),
            analysisAfter.Groups.Groups.Select(group =>
                ProjectGroup(group, analysisAfter.EffectiveLiberties)),
            NewEnemyAtariGroups(analysisBefore, analysisAfter),
            territoryDeltas,
            OwnedPointCount(stateAfter.BattleState.TerritoryAnalysis, TerritoryOwner.Black) -
                OwnedPointCount(source.State.BattleState.TerritoryAnalysis, TerritoryOwner.Black),
            OwnedPointCount(stateAfter.BattleState.TerritoryAnalysis, TerritoryOwner.White) -
                OwnedPointCount(source.State.BattleState.TerritoryAnalysis, TerritoryOwner.White),
            FacilityDeltas(source.State, stateAfter),
            DisplayedIntent(
                stateAfter,
                ProjectIntent(
                    stateAfter,
                    analysisAfter.Groups,
                    stateAfter.NormalPlan,
                    isCounterattackAction: false),
                ProjectIntent(
                    stateAfter,
                    analysisAfter.Groups,
                    stateAfter.BonusPlan,
                    isCounterattackAction: true)),
            mandatoryOverride,
            blackKingRisk);
    }

    private static CoreDuelCardPreviewResult Reject(
        CoreDuelBattleSession session,
        CoreDuelCardPreviewRequest request,
        string reasonId) =>
        new(
            false,
            reasonId,
            session.State.Checksum,
            session.CommandLog.CurrentChecksum,
            request.CardInstanceId,
            null,
            [],
            null,
            null,
            null,
            null,
            null);

    private static CoreDuelBattlePreviewResult RejectBattle(
        CoreDuelBattleSession session,
        string reasonId) =>
        new(
            accepted: false,
            reasonId,
            sourceStateChecksum: session.State.Checksum,
            sourceLogChecksum: session.CommandLog.CurrentChecksum,
            phaseId: null,
            playerTurnIndex: null,
            restartCount: null,
            outcomeId: null,
            endReasonId: null,
            qi: null,
            boardPoints: [],
            groups: [],
            handCards: [],
            drawPileCount: null,
            resolvingCardCount: null,
            discardPileCount: null,
            exhaustPileCount: null,
            normalIntent: null,
            bonusIntent: null,
            displayedIntent: null,
            mandatoryOverride: null,
            blackKingRisk: null);

    private static StateAnalysis Analyze(CoreDuelBattleState state)
    {
        var groups = StoneGroupAnalyzer.Analyze(state.BattleState.Board);
        var effective = TemporaryLibertyEffectiveLibertyAnalyzer.Analyze(
            state.RuntimeState.StoneRuntimeState,
            state.RuntimeState.TemporaryLibertyState,
            state.RuntimeState.ContinuousLibertySnapshot,
            groups);
        return new StateAnalysis(groups, effective);
    }

    private static CoreDuelGroupPreview ProjectGroup(
        StoneGroup group,
        TemporaryLibertyEffectiveLibertyAnalysis analysis)
    {
        var breakdown = analysis.BreakdownFor(group);
        return new CoreDuelGroupPreview(
            ColorId(group.Color),
            group.Anchor,
            group.StonePoints,
            group.RealLiberties,
            group.Stones.Any(stone => stone.IsKing),
            breakdown.TimedAmount,
            breakdown.ContinuousAmount,
            breakdown.EffectiveLibertyCount);
    }

    private static CoreDuelCapturedGroupPreview ProjectCapturedGroup(StoneGroup group) =>
        new(
            ColorId(group.Color),
            group.Anchor,
            group.StonePoints,
            group.Stones.Any(stone => stone.IsKing));

    private static IReadOnlyList<CoreDuelGroupPreview> NewEnemyAtariGroups(
        StateAnalysis before,
        StateAnalysis after)
    {
        var newlyAtari = new List<CoreDuelGroupPreview>();
        foreach (var group in after.Groups.Groups)
        {
            if (group.Color != StoneColor.White ||
                after.EffectiveLiberties.BreakdownFor(group).EffectiveLibertyCount != 1)
            {
                continue;
            }

            var beforeGroup = before.Groups.GroupAt(group.Anchor);
            var wasAtari = beforeGroup?.Color == StoneColor.White &&
                before.EffectiveLiberties
                    .BreakdownFor(beforeGroup)
                    .EffectiveLibertyCount == 1;
            if (!wasAtari)
            {
                newlyAtari.Add(ProjectGroup(group, after.EffectiveLiberties));
            }
        }

        return newlyAtari;
    }

    private static IReadOnlyList<CoreDuelBoardPointPreview> ProjectBoardPoints(
        CoreDuelBattleState state)
    {
        var points = new List<CoreDuelBoardPointPreview>(
            state.BattleState.Board.Geometry.PointCount);
        foreach (var point in state.BattleState.Board.Geometry.CanonicalPoints)
        {
            var stone = state.RuntimeState.StoneRuntimeState.InstanceAt(point);
            var facility = state.BattleState.FacilityRuntimeAnalysis.FacilityAt(point);
            points.Add(new CoreDuelBoardPointPreview(
                point,
                stone is null
                    ? null
                    : new CoreDuelStonePreview(
                        stone.InstanceId,
                        stone.KindId,
                        ColorId(stone.Color),
                        stone.IsKing),
                TerritoryOwnerId(
                    state.BattleState.TerritoryAnalysis.RegionAt(point)?.Owner),
                facility is null
                    ? null
                    : ProjectFacility(
                        state.BattleState.FacilityRuntimeAnalysis,
                        facility.Facility)));
        }

        return points;
    }

    private static CoreDuelCardInstancePreview ProjectCard(BattleCardInstance card) =>
        new(card.InstanceId, card.ContentId);

    private static IReadOnlyList<CoreDuelTerritoryPointDeltaPreview> TerritoryDeltas(
        TerritoryAnalysis before,
        TerritoryAnalysis after)
    {
        var deltas = new List<CoreDuelTerritoryPointDeltaPreview>();
        foreach (var point in before.SourceBoard.Geometry.CanonicalPoints)
        {
            var ownerBeforeId = TerritoryOwnerId(before.RegionAt(point)?.Owner);
            var ownerAfterId = TerritoryOwnerId(after.RegionAt(point)?.Owner);
            if (!StringComparer.Ordinal.Equals(ownerBeforeId, ownerAfterId))
            {
                deltas.Add(new CoreDuelTerritoryPointDeltaPreview(
                    point,
                    ownerBeforeId,
                    ownerAfterId));
            }
        }

        return deltas;
    }

    private static int OwnedPointCount(TerritoryAnalysis analysis, TerritoryOwner owner) =>
        analysis.Regions
            .Where(region => region.Owner == owner)
            .Sum(region => region.Size);

    private static IReadOnlyList<CoreDuelFacilityDeltaPreview> FacilityDeltas(
        CoreDuelBattleState before,
        CoreDuelBattleState after)
    {
        var beforeById = before.BattleState.FacilityState.InstalledFacilities
            .ToDictionary(facility => facility.InstanceId, StringComparer.Ordinal);
        var afterById = after.BattleState.FacilityState.InstalledFacilities
            .ToDictionary(facility => facility.InstanceId, StringComparer.Ordinal);
        var ids = beforeById.Keys
            .Concat(afterById.Keys)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(
                id => before.BattleState.Board.Geometry.ToCanonicalIndex(
                    afterById.GetValueOrDefault(id)?.Point ?? beforeById[id].Point))
            .ThenBy(id => id, StringComparer.Ordinal);
        var deltas = new List<CoreDuelFacilityDeltaPreview>();
        foreach (var id in ids)
        {
            var beforeProjection = beforeById.TryGetValue(id, out var beforeFacility)
                ? ProjectFacility(before.BattleState.FacilityRuntimeAnalysis, beforeFacility)
                : null;
            var afterProjection = afterById.TryGetValue(id, out var afterFacility)
                ? ProjectFacility(after.BattleState.FacilityRuntimeAnalysis, afterFacility)
                : null;
            if (!FacilityEquivalent(beforeProjection, afterProjection))
            {
                deltas.Add(new CoreDuelFacilityDeltaPreview(
                    id,
                    beforeProjection,
                    afterProjection));
            }
        }

        return deltas;
    }

    private static CoreDuelFacilityPreview ProjectFacility(
        FacilityRuntimeAnalysis analysis,
        FacilityInstance facility)
    {
        var operating = analysis.OperatingStateFor(facility);
        return new CoreDuelFacilityPreview(
            facility.InstanceId,
            facility.ContentId,
            ColorId(facility.Owner),
            facility.Point,
            facility.BuildSequence,
            facility.ExplicitDisableSources,
            operating.IsActive,
            operating.ReasonId);
    }

    private static bool FacilityEquivalent(
        CoreDuelFacilityPreview? left,
        CoreDuelFacilityPreview? right) =>
        left is null
            ? right is null
            : right is not null &&
              StringComparer.Ordinal.Equals(left.InstanceId, right.InstanceId) &&
              StringComparer.Ordinal.Equals(left.ContentId, right.ContentId) &&
              StringComparer.Ordinal.Equals(left.OwnerId, right.OwnerId) &&
              left.Point.Equals(right.Point) &&
              left.BuildSequence == right.BuildSequence &&
              left.ExplicitDisableSources.SequenceEqual(
                  right.ExplicitDisableSources,
                  StringComparer.Ordinal) &&
              left.IsActive == right.IsActive &&
              StringComparer.Ordinal.Equals(
                  left.OperatingReasonId,
                  right.OperatingReasonId);

    private static CoreDuelIntentPreview? ProjectIntent(
        CoreDuelBattleState state,
        StoneGroupAnalysis groups,
        PlannedEnemyIntent? intent,
        bool isCounterattackAction) =>
        intent is null
            ? null
            : new CoreDuelIntentPreview(
                isCounterattackAction,
                intent.IntentId,
                ProjectTarget(state, groups, intent.TargetReference),
                intent.PrimaryPoint,
                intent.AlternatePoints,
                intent.Retargetable,
                intent.PlannedFromStateChecksum,
                intent.Checksum);

    private static CoreDuelIntentPreview? DisplayedIntent(
        CoreDuelBattleState state,
        CoreDuelIntentPreview? normal,
        CoreDuelIntentPreview? bonus)
    {
        if (ReferenceEquals(state.DisplayedPlan, state.NormalPlan))
        {
            return normal;
        }

        return ReferenceEquals(state.DisplayedPlan, state.BonusPlan)
            ? bonus
            : null;
    }

    private static CoreDuelMandatoryOverridePreview ProjectMandatoryOverride(
        CoreDuelBattleSession session,
        StoneGroupAnalysis groups)
    {
        if (session.State.IsTerminal ||
            session.State.BattleState.Phase != BattlePhase.PlayerAction)
        {
            return NoMandatoryOverride();
        }

        var preview = CoreDuelBattleStateMachine.PreviewMandatoryOverride(session);
        if (!preview.HasOverride)
        {
            return NoMandatoryOverride();
        }

        var decision = preview.Decision
            ?? throw new InvalidOperationException(
                "A mandatory Bandit override is missing its execution decision.");
        return new CoreDuelMandatoryOverridePreview(
            preview.Kind switch
            {
                BanditMandatoryOverrideKind.Lethal => "lethal",
                BanditMandatoryOverrideKind.Defense => "defense",
                _ => throw new InvalidOperationException(
                    "Mandatory override contains an unsupported kind."),
            },
            decision.ReasonId,
            decision.ExecutedIntentId,
            preview.Point,
            ProjectTarget(session.State, groups, decision.TargetAfter));
    }

    private static CoreDuelMandatoryOverridePreview NoMandatoryOverride() =>
        new("none", "none", "none", null, null);

    private static CoreDuelBlackKingRiskPreview ProjectBlackKingRisk(
        CoreDuelBattleState state,
        StateAnalysis analysis,
        CoreDuelMandatoryOverridePreview mandatoryOverride)
    {
        var king = state.RuntimeState.StoneRuntimeState.Instances.SingleOrDefault(instance =>
            instance.Color == StoneColor.Black && instance.IsKing);
        if (king is null)
        {
            return new CoreDuelBlackKingRiskPreview(
                true,
                null,
                false,
                "black_king_captured",
                null);
        }

        var group = analysis.Groups.GroupAt(king.Point)
            ?? throw new InvalidOperationException("The live black king has no stone group.");
        var lethal = StringComparer.Ordinal.Equals(mandatoryOverride.KindId, "lethal");
        return new CoreDuelBlackKingRiskPreview(
            false,
            ProjectGroup(group, analysis.EffectiveLiberties),
            lethal,
            lethal ? mandatoryOverride.ReasonId : "none",
            lethal ? mandatoryOverride.Point : null);
    }

    private static CoreDuelEnemyTargetPreview? ProjectTarget(
        CoreDuelBattleState state,
        StoneGroupAnalysis groups,
        EnemyTargetReference? target) =>
        target is null
            ? null
            : ProjectStoneGroupTarget(state, groups, target);

    private static CoreDuelEnemyTargetPreview ProjectStoneGroupTarget(
        CoreDuelBattleState state,
        StoneGroupAnalysis groups,
        EnemyTargetReference target)
    {
        if (target.Kind != EnemyTargetReferenceKind.StoneGroup)
        {
            throw new InvalidOperationException(
                "Bandit plan contains an unsupported target kind.");
        }

        state.BattleState.Board.Geometry.ToCanonicalIndex(target.Anchor);
        var current = groups.GroupAt(target.Anchor);
        if (current?.Color != target.Color)
        {
            current = null;
        }

        return new CoreDuelEnemyTargetPreview(
            "stone_group",
            ColorId(target.Color),
            target.Anchor,
            current is not null,
            current?.Anchor,
            current?.StonePoints ?? []);
    }

    private static string ColorId(StoneColor color) => color switch
    {
        StoneColor.Black => "black",
        StoneColor.White => "white",
        _ => throw new InvalidOperationException("Preview contains an unknown stone color."),
    };

    private static string TerritoryOwnerId(TerritoryOwner? owner) => owner switch
    {
        null => "none",
        TerritoryOwner.Neutral => "neutral",
        TerritoryOwner.Black => "black",
        TerritoryOwner.White => "white",
        _ => throw new InvalidOperationException("Preview contains an unknown territory owner."),
    };

    private sealed record PlacementModeCandidate(
        StoneCardPlacementMode? Mode,
        string Id);

    private sealed record StateAnalysis(
        StoneGroupAnalysis Groups,
        TemporaryLibertyEffectiveLibertyAnalysis EffectiveLiberties);
}
