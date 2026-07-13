using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

using Igorogue.Application.Replay;
using Igorogue.Domain.Board;
using Igorogue.Domain.Cards;
using Igorogue.Domain.Combat;
using Igorogue.Domain.Content;
using Igorogue.Domain.Determinism;

namespace Igorogue.Application.Battle;

public sealed class PlayCardCommand : IBattleCommand
{
    public PlayCardCommand(
        string expectedStateChecksum,
        string expectedLogChecksum,
        string cardInstanceId,
        CanonicalPoint target,
        StoneCardPlacementMode placementMode)
    {
        ExpectedStateChecksum = BattleCommandValidation.CanonicalChecksum(
            expectedStateChecksum,
            nameof(expectedStateChecksum));
        ExpectedLogChecksum = BattleCommandValidation.CanonicalChecksum(
            expectedLogChecksum,
            nameof(expectedLogChecksum));
        ArgumentNullException.ThrowIfNull(target);
        if (!Enum.IsDefined(placementMode))
        {
            throw new ArgumentOutOfRangeException(
                nameof(placementMode),
                placementMode,
                "Unknown starter-stone placement mode.");
        }

        CardInstanceId = ValidateStableId(cardInstanceId, nameof(cardInstanceId));
        Target = target;
        PlacementMode = placementMode;
    }

    public string CommandType => "battle.play_card";

    public int CommandSchemaVersion => 1;

    public string ExpectedStateChecksum { get; }

    public string ExpectedLogChecksum { get; }

    public string CardInstanceId { get; }

    public CanonicalPoint Target { get; }

    public StoneCardPlacementMode PlacementMode { get; }

    public string ToCanonicalPayload() =>
        "play-card-v1\n" +
        $"expected_state_checksum={ExpectedStateChecksum}\n" +
        $"expected_log_checksum={ExpectedLogChecksum}\n" +
        $"card_instance_id={CardInstanceId}\n" +
        $"target={Target.X.ToString(CultureInfo.InvariantCulture)},{Target.Y.ToString(CultureInfo.InvariantCulture)}\n" +
        $"placement_mode={PlacementModeId(PlacementMode)}\n";

    private static string ValidateStableId(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        if (value.Any(character =>
                !char.IsAsciiLetterOrDigit(character) &&
                character is not '.' and not '_' and not '-'))
        {
            throw new ArgumentException(
                "Stable IDs may contain only ASCII letters, digits, '.', '_', or '-'.",
                parameterName);
        }

        return value;
    }

    private static string PlacementModeId(StoneCardPlacementMode mode) => mode switch
    {
        StoneCardPlacementMode.Frontline => "frontline",
        StoneCardPlacementMode.Contact => "contact",
        StoneCardPlacementMode.TerminalCapture => "terminal_capture",
        _ => throw new InvalidOperationException("Command contains an unknown placement mode."),
    };
}

public sealed class CoreDuelCardPlayState
{
    public const string EncodingVersion = "core-duel-card-play-state-v2";

    private CoreDuelCardPlayState(
        string contentHash,
        BattleState battleState,
        BattleAuthoritativeRuntimeState runtimeState,
        CoreDuelCardTurnState cardTurnState,
        StarterStoneCardPlayCatalog definitions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentHash);
        ArgumentNullException.ThrowIfNull(battleState);
        ArgumentNullException.ThrowIfNull(runtimeState);
        ArgumentNullException.ThrowIfNull(cardTurnState);
        ArgumentNullException.ThrowIfNull(definitions);
        if (battleState.AuthoritativeRuntime is not null)
        {
            throw new ArgumentException(
                "Starter-card play keeps its authoritative runtime as a detached sidecar until TASK-0039.",
                nameof(battleState));
        }

        if (!ReferenceEquals(runtimeState.StoneRuntimeState.SourceBoard, battleState.Board))
        {
            throw new ArgumentException(
                "Card-play runtime must belong to the exact legacy battle board snapshot.",
                nameof(runtimeState));
        }

        if (!ReferenceEquals(
                runtimeState.ClosedWindowResources,
                cardTurnState.ClosedWindowResources))
        {
            throw new ArgumentException(
                "Card-turn and runtime sidecar must share the exact resource snapshot.",
                nameof(cardTurnState));
        }

        if (!battleState.RngState.Equals(cardTurnState.RngState))
        {
            throw new ArgumentException(
                "Battle and card-turn state must share the same authoritative RNG snapshot.",
                nameof(cardTurnState));
        }

        if (runtimeState.EnemyActionStage is not null ||
            runtimeState.PendingAtEnemyTurnStart is not null)
        {
            throw new ArgumentException(
                "Standalone player card play cannot retain an active enemy boundary.",
                nameof(runtimeState));
        }

        ContentHash = contentHash;
        BattleState = battleState;
        RuntimeState = runtimeState;
        CardTurnState = cardTurnState;
        Definitions = definitions;
        CanonicalText = CreateCanonicalText();
        Checksum = DeterministicChecksum.Sha256Hex(CanonicalText);
    }

    public string ContentHash { get; }

    public BattleState BattleState { get; }

    public BattleAuthoritativeRuntimeState RuntimeState { get; }

    public CoreDuelCardTurnState CardTurnState { get; }

    public StarterStoneCardPlayCatalog Definitions { get; }

    public string CanonicalText { get; }

    public string Checksum { get; }

    public bool IsTerminal => BattleState.IsTerminal;

    public string ToCanonicalText() => CanonicalText;

    internal static CoreDuelCardPlayState Create(
        string contentHash,
        BattleState battleState,
        BattleAuthoritativeRuntimeState runtimeState,
        CoreDuelCardTurnState cardTurnState,
        StarterStoneCardPlayCatalog definitions) =>
        new(contentHash, battleState, runtimeState, cardTurnState, definitions);

    private string CreateCanonicalText() => string.Join(
        '\n',
        EncodingVersion,
        $"content_hash={ContentHash}",
        $"starter_stone_definitions={EncodeStableText(Definitions.ToCanonicalText())}",
        $"battle_state={EncodeStableText(BattleState.ToCanonicalText())}",
        $"runtime_sidecar={EncodeStableText(RuntimeState.ToCanonicalText())}",
        $"card_turn_state={EncodeStableText(CardTurnState.ToCanonicalText())}");

    private static string EncodeStableText(string value) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
}

public sealed class CoreDuelCardPlaySession
{
    internal CoreDuelCardPlaySession(
        CoreDuelCardPlayState state,
        OrderedCommandLog commandLog)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(commandLog);
        if (commandLog.Metadata.InitialSeed != state.CardTurnState.RngState.InitialSeed ||
            !StringComparer.Ordinal.Equals(commandLog.Metadata.ContentHash, state.ContentHash))
        {
            throw new ArgumentException(
                "Command-log identity must match the card-play state seed and content hash.",
                nameof(commandLog));
        }

        if (commandLog.Entries.Count > 0 &&
            !StringComparer.Ordinal.Equals(
                commandLog.Entries[^1].ResultChecksum,
                state.Checksum))
        {
            throw new ArgumentException(
                "The last command-log checksum must match the card-play state.",
                nameof(commandLog));
        }

        State = state;
        CommandLog = commandLog;
    }

    public CoreDuelCardPlayState State { get; }

    public OrderedCommandLog CommandLog { get; }
}

public sealed class CoreDuelCardPlayResult
{
    private readonly ReadOnlyCollection<IBattleFact> orderedFactView;

    internal CoreDuelCardPlayResult(
        CoreDuelCardPlaySession sessionBefore,
        CoreDuelCardPlaySession sessionAfter,
        PlayCardCommand command,
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
                "Rejected PlayCard results must retain the exact session and one rejection fact.");
        }

        SessionBefore = sessionBefore;
        SessionAfter = sessionAfter;
        Command = command;
        Accepted = accepted;
        ReasonId = reasonId;
        orderedFactView = Array.AsReadOnly(facts);
    }

    public CoreDuelCardPlaySession SessionBefore { get; }

    public CoreDuelCardPlaySession SessionAfter { get; }

    public PlayCardCommand Command { get; }

    public bool Accepted { get; }

    public string ReasonId { get; }

    public IReadOnlyList<IBattleFact> OrderedFacts => orderedFactView;

    public string StateChecksum => SessionAfter.State.Checksum;

    public string LogChecksum => SessionAfter.CommandLog.CurrentChecksum;
}

public static class CoreDuelCardPlayStateMachine
{
    public static CoreDuelCardPlaySession Start(
        BattleAuthoritativeInitialSnapshot initial,
        CoreDuelCardTurnState cardTurnState,
        StarterStoneCardPlayCatalog definitions,
        ReplayMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(initial);
        ArgumentNullException.ThrowIfNull(cardTurnState);
        ArgumentNullException.ThrowIfNull(definitions);
        ArgumentNullException.ThrowIfNull(metadata);
        if (metadata.InitialSeed != cardTurnState.RngState.InitialSeed)
        {
            throw new ArgumentException(
                "Replay metadata seed must match the card-turn RNG seed.",
                nameof(metadata));
        }

        if (!ReferenceEquals(
                initial.ClosedWindowResources,
                cardTurnState.ClosedWindowResources))
        {
            throw new ArgumentException(
                "Initial runtime and card turn must share the exact resource snapshot.",
                nameof(cardTurnState));
        }

        var battleState = BattleState.Start(
            initial.Board,
            initial.RepetitionHistory,
            initial.FacilityState,
            cardTurnState.RngState,
            initial.RuntimePolicy);
        var state = CoreDuelCardPlayState.Create(
            metadata.ContentHash,
            battleState,
            BattleAuthoritativeRuntimeState.FromInitial(initial),
            cardTurnState,
            definitions);
        return new CoreDuelCardPlaySession(
            state,
            OrderedCommandLog.Create(metadata));
    }

    public static CoreDuelCardPlayResult Execute(
        CoreDuelCardPlaySession session,
        PlayCardCommand command)
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

        if (session.State.BattleState.Phase != BattlePhase.PlayerAction)
        {
            return Reject(session, command, "wrong_phase");
        }

        var source = session.State;
        var card = source.CardTurnState.Deck.Hand.FirstOrDefault(candidate =>
            StringComparer.Ordinal.Equals(candidate.InstanceId, command.CardInstanceId));
        if (card is null)
        {
            return Reject(session, command, "card_not_in_hand");
        }

        if (!source.Definitions.TryDefinition(card.ContentId, out var definition) ||
            definition is null)
        {
            return Reject(session, command, "card_content_mismatch");
        }

        var evaluation = StarterStoneCardPlayEvaluator.Evaluate(
            source.Definitions,
            definition,
            source.CardTurnState.Deck,
            source.CardTurnState.Qi,
            command.CardInstanceId,
            source.BattleState.Board,
            command.Target,
            command.PlacementMode);
        if (!evaluation.IsAuthorized)
        {
            return Reject(session, command, evaluation.ReasonId);
        }

        var accessMode = evaluation.AccessMode
            ?? throw new InvalidOperationException(
                "Authorized starter-stone play must bind a placement access mode.");
        if (!evaluation.IsBoundTo(
                source.Definitions,
                definition,
                source.CardTurnState.Deck,
                source.CardTurnState.Qi,
                command.CardInstanceId,
                source.BattleState.Board,
                command.Target,
                command.PlacementMode,
                accessMode))
        {
            throw new InvalidOperationException(
                "Starter-stone evaluation lost its exact source binding.");
        }

        var stoneInstanceId = StoneInstanceId(
            command.CardInstanceId,
            source.RuntimeState.StoneRuntimeState.NextCreatedSequence);
        var placementDescriptor = new StoneRuntimePlacementDescriptor(
            stoneInstanceId,
            StoneKindId(definition.StoneKind),
            definition.OnCaptured.Count == 0 ? [] : ["captured_stone_self"]);
        var runtimePlacement = AuthorizedRuntimeStonePlacementPipeline.Resolve(
            source.BattleState,
            source.RuntimeState,
            StoneColor.Black,
            command.Target,
            accessMode,
            placementDescriptor);
        if (!runtimePlacement.Accepted)
        {
            return Reject(session, command, runtimePlacement.ReasonId);
        }

        var legalPlacement = runtimePlacement.LegalPlacementCommit
            ?? throw new InvalidOperationException(
                "Accepted runtime placement is missing its legal commit.");
        var runtimeCommit = runtimePlacement.RuntimePlacementCommit
            ?? throw new InvalidOperationException(
                "Accepted runtime placement is missing its runtime commit.");
        var postCapture = runtimePlacement.PostCaptureEffectiveLiberties
            ?? throw new InvalidOperationException(
                "Accepted runtime placement is missing post-capture liberties.");
        var placement = AuthorizedStonePlacementPipeline.Commit(
            source.BattleState,
            StoneColor.Black,
            legalPlacement);
        if (!placement.Accepted)
        {
            throw new InvalidOperationException(
                $"A runtime-authorized placement failed commit: {placement.ReasonId}.");
        }

        var cardTransition = CoreDuelCardTurnKernel.CommitStarterStoneCardPlay(
            source.CardTurnState,
            evaluation);
        if (cardTransition.IsExactNoOp)
        {
            throw new InvalidOperationException(
                $"Accepted placement could not commit its card: {cardTransition.ReasonId}.");
        }

        var facts = new List<IBattleFact>
        {
            QiChangedFact.SpendCardCost(
                source.CardTurnState.Qi,
                cardTransition.StateAfter.Qi,
                command.CardInstanceId),
        };
        facts.AddRange(placement.OrderedFacts);

        var cardStateAfter = cardTransition.StateAfter;
        if (!placement.StateAfter.IsTerminal)
        {
            var effectAnalysis = StarterStonePlacementEffectAnalysis.Create(
                legalPlacement,
                postCapture);
            cardStateAfter = ApplyFollowUpEffects(
                cardStateAfter,
                definition,
                effectAnalysis,
                command.CardInstanceId,
                facts);
        }

        var triggerPlan = source.RuntimeState.CaptureBenefitTriggerPlan;
        if (!placement.StateAfter.IsTerminal && definition.OnCaptured.Count > 0)
        {
            triggerPlan = triggerPlan.AppendConditional(
                LureCaptureTrigger(definition, stoneInstanceId));
        }

        var continuousAfter = source.RuntimeState.ContinuousLibertySnapshot.Rebind(
            runtimeCommit.StonesAfterCommit);
        var runtimeAfter = source.RuntimeState.Transition(
            stoneRuntimeState: runtimeCommit.StonesAfterCommit,
            temporaryLibertyState: runtimeCommit.TemporaryLibertiesAfterCommit,
            continuousLibertySnapshot: continuousAfter,
            closedWindowResources: cardStateAfter.ClosedWindowResources,
            captureBenefitTriggerPlan: triggerPlan,
            registeredStoneInstanceId: stoneInstanceId);
        var battleAfter = BattleState.RebindRng(
            placement.StateAfter,
            cardStateAfter.RngState);
        var stateAfter = CoreDuelCardPlayState.Create(
            source.ContentHash,
            battleAfter,
            runtimeAfter,
            cardStateAfter,
            source.Definitions);
        var nextLog = session.CommandLog.Append(command, stateAfter.Checksum);
        var nextSession = new CoreDuelCardPlaySession(stateAfter, nextLog);
        return new CoreDuelCardPlayResult(
            session,
            nextSession,
            command,
            true,
            "accepted",
            facts);
    }

    private static CoreDuelCardTurnState ApplyFollowUpEffects(
        CoreDuelCardTurnState source,
        StarterStoneCardPlayDefinition definition,
        StarterStonePlacementEffectAnalysis placement,
        string cardInstanceId,
        ICollection<IBattleFact> facts)
    {
        var state = source;
        foreach (var operation in definition.Effects.Skip(1))
        {
            switch (operation)
            {
                case DrawIfRealLibertiesAtLeastOperationDefinition draw
                    when placement.PlacedGroupRealLibertyCount >=
                         draw.MinimumRealLiberties:
                {
                    var handCountBefore = state.Deck.Hand.Count;
                    var deckTransition = state.Deck.Draw(draw.Cards, state.RngState);
                    if (!deckTransition.IsExactNoOp)
                    {
                        var drawnCards = deckTransition.DeckAfter.Hand
                            .Skip(handCountBefore)
                            .ToArray();
                        state = CoreDuelCardTurnState.Create(
                            deckTransition.DeckAfter,
                            deckTransition.RngAfter,
                            state.SystemPolicy,
                            state.ClosedWindowResources,
                            state.Qi,
                            state.TurnScopedFlags);
                        foreach (var drawn in drawnCards)
                        {
                            facts.Add(CardDrawnFact.FromCardEffect(
                                drawn,
                                cardInstanceId));
                        }
                    }

                    break;
                }
                case DrawIfRealLibertiesAtLeastOperationDefinition:
                    break;
                case GainQiIfEnemyAtariOperationDefinition gain
                    when placement.EstablishedEnemyAtari:
                {
                    var fact = QiChangedFact.GainFromCardEffect(
                        state.Qi,
                        gain.Amount,
                        cardInstanceId);
                    state = CoreDuelCardTurnState.Create(
                        state.Deck,
                        state.RngState,
                        state.SystemPolicy,
                        state.ClosedWindowResources,
                        fact.NewAmount,
                        state.TurnScopedFlags);
                    facts.Add(fact);
                    break;
                }
                case GainQiIfEnemyAtariOperationDefinition:
                    break;
                case ReserveDrawOperationDefinition reserve:
                {
                    var resolution = ReservedDrawCardEffectResolver.Apply(
                        state.ClosedWindowResources,
                        cardInstanceId,
                        reserve.Cards);
                    state = CoreDuelCardTurnState.Create(
                        state.Deck,
                        state.RngState,
                        state.SystemPolicy,
                        resolution.StateAfter,
                        state.Qi,
                        state.TurnScopedFlags);
                    facts.Add(resolution.Fact);
                    break;
                }
                default:
                    throw new InvalidOperationException(
                        $"Unsupported starter-stone follow-up operation {operation.GetType().FullName}.");
            }
        }

        return state;
    }

    private static CaptureBenefitTriggerPlanEntry LureCaptureTrigger(
        StarterStoneCardPlayDefinition definition,
        string stoneInstanceId)
    {
        if (definition.OnCaptured.Count != 1 ||
            definition.OnCaptured[0] is not ReserveDrawOperationDefinition reserve)
        {
            throw new InvalidOperationException(
                "A registered starter-stone capture trigger must be one reserved-draw operation.");
        }

        return new CaptureBenefitTriggerPlanEntry(
            new CaptureBenefitTrigger(
                CaptureBenefitSource.CapturedStoneSelf(stoneInstanceId),
                $"lure.{stoneInstanceId}",
                ["lure", stoneInstanceId],
                [new ReserveDrawCaptureBenefitOperation(reserve.Cards)],
                firstUseFlagId: null),
            CaptureBenefitTriggerCondition.CapturedSourceStone);
    }

    private static string StoneInstanceId(string cardInstanceId, long sequence) =>
        $"stone.card.{cardInstanceId}.{sequence.ToString(CultureInfo.InvariantCulture)}";

    private static string StoneKindId(StoneContentKind kind) => kind switch
    {
        StoneContentKind.Basic => "basic",
        StoneContentKind.Lure => "lure",
        _ => throw new InvalidOperationException("Unsupported starter-stone runtime kind."),
    };

    private static CoreDuelCardPlayResult Reject(
        CoreDuelCardPlaySession session,
        PlayCardCommand command,
        string reasonId) =>
        new(
            session,
            session,
            command,
            false,
            reasonId,
            [new CommandRejectedFact(reasonId)]);
}
