using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

using Igorogue.Domain.Board;
using Igorogue.Domain.Cards;
using Igorogue.Domain.Combat;
using Igorogue.Domain.Content;
using Igorogue.Domain.Determinism;
using Igorogue.Domain.Facilities;

namespace Igorogue.Application.Battle;

public sealed class AuthorizedDeferredChoiceOutcome
{
    public AuthorizedDeferredChoiceOutcome(
        string sourceId,
        string choiceId,
        long createdSequence,
        int qiDelta,
        int drawDelta)
    {
        var identity = new DeferredPlayerChoice(sourceId, choiceId, createdSequence);
        if (qiDelta < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(qiDelta),
                qiDelta,
                "Authorized deferred-choice qi cannot be negative.");
        }

        if (drawDelta < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(drawDelta),
                drawDelta,
                "Authorized deferred-choice draw cannot be negative.");
        }

        SourceId = identity.SourceId;
        ChoiceId = identity.ChoiceId;
        CreatedSequence = identity.CreatedSequence;
        QiDelta = qiDelta;
        DrawDelta = drawDelta;
    }

    public string SourceId { get; }

    public string ChoiceId { get; }

    public long CreatedSequence { get; }

    public int QiDelta { get; }

    public int DrawDelta { get; }
}

public enum CoreDuelCardTurnStage : byte
{
    ResetTurnScopedFlags = 1,
    ResolveDeferredPlayerChoices = 2,
    RecalculateTerritory = 3,
    ApplyQi = 4,
    DrawCards = 5,
}

public sealed class CoreDuelCardTurnState
{
    public const string EncodingVersion = "core-duel-card-turn-state-v1";

    private readonly ReadOnlyDictionary<string, bool> turnScopedFlagView;

    private CoreDuelCardTurnState(
        BattleDeckState deck,
        AuthoritativeRngState rngState,
        CoreDuelSystemPolicy systemPolicy,
        ClosedWindowResourceState closedWindowResources,
        int qi,
        SortedDictionary<string, bool> turnScopedFlags)
    {
        ArgumentNullException.ThrowIfNull(deck);
        ArgumentNullException.ThrowIfNull(rngState);
        ArgumentNullException.ThrowIfNull(systemPolicy);
        ArgumentNullException.ThrowIfNull(closedWindowResources);
        if (qi < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(qi), qi, "Qi cannot be negative.");
        }

        Deck = deck;
        RngState = rngState;
        SystemPolicy = systemPolicy;
        ClosedWindowResources = closedWindowResources;
        Qi = qi;
        turnScopedFlagView = new ReadOnlyDictionary<string, bool>(
            new SortedDictionary<string, bool>(turnScopedFlags, StringComparer.Ordinal));
        CanonicalText = CreateCanonicalText();
        Checksum = DeterministicChecksum.Sha256Hex(CanonicalText);
    }

    public BattleDeckState Deck { get; }

    public AuthoritativeRngState RngState { get; }

    public CoreDuelSystemPolicy SystemPolicy { get; }

    public ClosedWindowResourceState ClosedWindowResources { get; }

    public int Qi { get; }

    public IReadOnlyDictionary<string, bool> TurnScopedFlags => turnScopedFlagView;

    public string CanonicalText { get; }

    public string Checksum { get; }

    public string ToCanonicalText() => CanonicalText;

    internal static CoreDuelCardTurnState Create(
        BattleDeckState deck,
        AuthoritativeRngState rngState,
        CoreDuelSystemPolicy systemPolicy,
        ClosedWindowResourceState closedWindowResources,
        int qi,
        IEnumerable<KeyValuePair<string, bool>> turnScopedFlags)
    {
        ArgumentNullException.ThrowIfNull(turnScopedFlags);
        var canonicalFlags = new SortedDictionary<string, bool>(StringComparer.Ordinal);
        foreach (var pair in turnScopedFlags)
        {
            var flagId = ValidateStableId(pair.Key, nameof(turnScopedFlags));
            if (!canonicalFlags.TryAdd(flagId, pair.Value))
            {
                throw new ArgumentException(
                    $"Card-turn state contains duplicate turn-scoped flag {flagId}.",
                    nameof(turnScopedFlags));
            }
        }

        return new CoreDuelCardTurnState(
            deck,
            rngState,
            systemPolicy,
            closedWindowResources,
            qi,
            canonicalFlags);
    }

    private string CreateCanonicalText()
    {
        var lines = new List<string>
        {
            EncodingVersion,
            $"qi={Qi.ToString(CultureInfo.InvariantCulture)}",
            $"base_qi={SystemPolicy.BaseQi.ToString(CultureInfo.InvariantCulture)}",
            $"base_draw={SystemPolicy.BaseDraw.ToString(CultureInfo.InvariantCulture)}",
            $"rng={EncodeStableText(RngState.ToCanonicalText())}",
            $"deck={EncodeStableText(Deck.ToCanonicalText())}",
            $"closed_window_resources={EncodeStableText(ClosedWindowResources.ToCanonicalText())}",
            $"turn_scoped_flag_count={turnScopedFlagView.Count.ToString(CultureInfo.InvariantCulture)}",
        };

        foreach (var pair in turnScopedFlagView)
        {
            lines.Add(
                $"turn_scoped_flag={EncodeStableText(pair.Key)}:{(pair.Value ? "1" : "0")}");
        }

        return string.Join('\n', lines);
    }

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

    private static string EncodeStableText(string value) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
}

public sealed class CoreDuelCardTurnTransition
{
    private readonly ReadOnlyCollection<CoreDuelCardTurnStage> stageView;

    private CoreDuelCardTurnTransition(
        CoreDuelCardTurnState stateBefore,
        CoreDuelCardTurnState stateAfter,
        bool isExactNoOp,
        string? reasonId,
        CoreDuelCardTurnStage[] stages,
        int territoryIncome,
        int appliedQi,
        int requestedDrawCount)
    {
        StateBefore = stateBefore;
        StateAfter = stateAfter;
        IsExactNoOp = isExactNoOp;
        ReasonId = reasonId;
        stageView = Array.AsReadOnly((CoreDuelCardTurnStage[])stages.Clone());
        TerritoryIncome = territoryIncome;
        AppliedQi = appliedQi;
        RequestedDrawCount = requestedDrawCount;
    }

    public CoreDuelCardTurnState StateBefore { get; }

    public CoreDuelCardTurnState StateAfter { get; }

    public bool Accepted => !IsExactNoOp;

    public bool IsExactNoOp { get; }

    public string? ReasonId { get; }

    public IReadOnlyList<CoreDuelCardTurnStage> Stages => stageView;

    public int TerritoryIncome { get; }

    public int AppliedQi { get; }

    public int RequestedDrawCount { get; }

    internal static CoreDuelCardTurnTransition Applied(
        CoreDuelCardTurnState stateBefore,
        CoreDuelCardTurnState stateAfter,
        IEnumerable<CoreDuelCardTurnStage>? stages = null,
        int territoryIncome = 0,
        int appliedQi = 0,
        int requestedDrawCount = 0) =>
        new(
            stateBefore,
            stateAfter,
            false,
            null,
            stages?.ToArray() ?? [],
            territoryIncome,
            appliedQi,
            requestedDrawCount);

    internal static CoreDuelCardTurnTransition NoOp(
        CoreDuelCardTurnState source,
        string reasonId) =>
        new(source, source, true, reasonId, [], 0, 0, 0);
}

public static class CoreDuelCardTurnKernel
{
    private static readonly CoreDuelCardTurnStage[] TurnStartStages =
    [
        CoreDuelCardTurnStage.ResetTurnScopedFlags,
        CoreDuelCardTurnStage.ResolveDeferredPlayerChoices,
        CoreDuelCardTurnStage.RecalculateTerritory,
        CoreDuelCardTurnStage.ApplyQi,
        CoreDuelCardTurnStage.DrawCards,
    ];

    public static CoreDuelCardTurnState StartBattle(
        IEnumerable<BattleCardInstance> recipe,
        AuthoritativeRngState rngState,
        CoreDuelSystemPolicy systemPolicy,
        ClosedWindowResourceState closedWindowResources,
        IEnumerable<KeyValuePair<string, bool>> turnScopedFlags)
    {
        ArgumentNullException.ThrowIfNull(recipe);
        ArgumentNullException.ThrowIfNull(rngState);
        ArgumentNullException.ThrowIfNull(systemPolicy);
        ArgumentNullException.ThrowIfNull(closedWindowResources);
        ArgumentNullException.ThrowIfNull(turnScopedFlags);

        var initialization = BattleDeckState.CreateShuffled(recipe, rngState);
        return CoreDuelCardTurnState.Create(
            initialization.Deck,
            initialization.RngAfter,
            systemPolicy,
            closedWindowResources,
            qi: 0,
            turnScopedFlags);
    }

    public static CoreDuelCardTurnTransition StartPlayerTurn(
        CoreDuelCardTurnState state,
        BoardState board,
        FacilityState facilities,
        FacilityRuntimePolicy facilityPolicy,
        IEnumerable<AuthorizedDeferredChoiceOutcome> authorizedOutcomes)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(facilities);
        ArgumentNullException.ThrowIfNull(facilityPolicy);
        ArgumentNullException.ThrowIfNull(authorizedOutcomes);

        var outcomes = authorizedOutcomes.ToArray();
        foreach (var outcome in outcomes)
        {
            ArgumentNullException.ThrowIfNull(outcome);
        }

        if (!OutcomesMatch(state.ClosedWindowResources.DeferredPlayerChoices, outcomes))
        {
            return CoreDuelCardTurnTransition.NoOp(
                state,
                "deferred_choice_outcomes_mismatch");
        }

        if (!ReferenceEquals(facilities.SourceBoard, board))
        {
            throw new ArgumentException(
                "Facility state must belong to the exact turn-start board snapshot.",
                nameof(facilities));
        }

        try
        {
            var resetTurnFlags = state.TurnScopedFlags
                .Select(pair => new KeyValuePair<string, bool>(pair.Key, false))
                .ToArray();
            var authorizedQi = 0;
            var authorizedDraw = 0;
            foreach (var outcome in outcomes)
            {
                authorizedQi = checked(authorizedQi + outcome.QiDelta);
                authorizedDraw = checked(authorizedDraw + outcome.DrawDelta);
            }

            var resourcesAfterChoices = RecreateResources(
                state.ClosedWindowResources,
                turnReservedDraw: checked(
                    state.ClosedWindowResources.TurnReservedDraw + authorizedDraw),
                turnReservedQi: checked(
                    state.ClosedWindowResources.TurnReservedQi + authorizedQi));

            var territory = TerritoryAnalyzer.Analyze(board);
            var facilityRuntime = FacilityRuntimeAnalyzer.Analyze(
                facilities,
                territory,
                facilityPolicy);
            var territoryIncome = 0;
            foreach (var region in facilityRuntime.Regions)
            {
                if (region.Region.Owner == TerritoryOwner.Black)
                {
                    territoryIncome = checked(territoryIncome + region.BasicIncome);
                }
            }

            var appliedQi = checked(
                checked(state.SystemPolicy.BaseQi + territoryIncome) +
                resourcesAfterChoices.TurnReservedQi);
            var resourcesAfterQi = RecreateResources(
                resourcesAfterChoices,
                resourcesAfterChoices.TurnReservedDraw,
                turnReservedQi: 0);

            var requestedDraw = checked(
                state.SystemPolicy.BaseDraw +
                resourcesAfterQi.TurnReservedDraw);
            var draw = state.Deck.Draw(requestedDraw, state.RngState);
            var resourcesAfterDraw = RecreateResources(
                resourcesAfterQi,
                turnReservedDraw: 0,
                turnReservedQi: 0);
            var stateAfter = CoreDuelCardTurnState.Create(
                draw.DeckAfter,
                draw.RngAfter,
                state.SystemPolicy,
                resourcesAfterDraw,
                appliedQi,
                resetTurnFlags);
            return CoreDuelCardTurnTransition.Applied(
                state,
                stateAfter,
                TurnStartStages,
                territoryIncome,
                appliedQi,
                requestedDraw);
        }
        catch (OverflowException)
        {
            return CoreDuelCardTurnTransition.NoOp(state, "turn_start_overflow");
        }
    }

    public static CoreDuelCardTurnTransition BeginResolution(
        CoreDuelCardTurnState state,
        string? instanceId)
    {
        ArgumentNullException.ThrowIfNull(state);
        return ApplyDeckTransition(
            state,
            state.Deck.BeginResolution(instanceId, state.RngState));
    }

    public static CoreDuelCardTurnTransition CompleteResolution(
        CoreDuelCardTurnState state,
        string? instanceId)
    {
        ArgumentNullException.ThrowIfNull(state);
        return ApplyDeckTransition(
            state,
            state.Deck.CompleteResolution(instanceId, state.RngState));
    }

    public static CoreDuelCardTurnTransition Exhaust(
        CoreDuelCardTurnState state,
        string? instanceId)
    {
        ArgumentNullException.ThrowIfNull(state);
        return ApplyDeckTransition(
            state,
            state.Deck.Exhaust(instanceId, state.RngState));
    }

    internal static CoreDuelCardTurnTransition CommitBasicStoneCardPlay(
        CoreDuelCardTurnState state,
        BasicStoneCardPlayEvaluation evaluation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(evaluation);
        if (!evaluation.IsAuthorized)
        {
            return CoreDuelCardTurnTransition.NoOp(state, evaluation.ReasonId);
        }

        if (!ReferenceEquals(evaluation.SourceDeck, state.Deck) ||
            evaluation.SourceQi != state.Qi ||
            evaluation.Card is null)
        {
            return CoreDuelCardTurnTransition.NoOp(
                state,
                "stale_card_play_evaluation");
        }

        var begun = state.Deck.BeginResolution(
            evaluation.Card.InstanceId,
            state.RngState);
        if (begun.IsExactNoOp)
        {
            return CoreDuelCardTurnTransition.NoOp(
                state,
                begun.NoOpReason ?? "card_resolution_rejected");
        }

        var completed = begun.DeckAfter.CompleteResolution(
            evaluation.Card.InstanceId,
            begun.RngAfter);
        if (completed.IsExactNoOp)
        {
            throw new InvalidOperationException(
                "A begun basic-stone card resolution must be completable.");
        }

        var qiAfter = checked(state.Qi - evaluation.SourceDefinition.Cost);
        var stateAfter = CoreDuelCardTurnState.Create(
            completed.DeckAfter,
            completed.RngAfter,
            state.SystemPolicy,
            state.ClosedWindowResources,
            qiAfter,
            state.TurnScopedFlags);
        return CoreDuelCardTurnTransition.Applied(state, stateAfter);
    }

    public static CoreDuelCardTurnTransition EndPlayerTurn(CoreDuelCardTurnState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var deckTransition = state.Deck.EndTurn(state.RngState);
        if (deckTransition.IsExactNoOp && deckTransition.NoOpReason == "active_resolution_exists")
        {
            return CoreDuelCardTurnTransition.NoOp(state, deckTransition.NoOpReason);
        }

        if (deckTransition.IsExactNoOp && state.Qi == 0)
        {
            return CoreDuelCardTurnTransition.NoOp(
                state,
                deckTransition.NoOpReason ?? "nothing_to_end");
        }

        var stateAfter = CoreDuelCardTurnState.Create(
            deckTransition.DeckAfter,
            deckTransition.RngAfter,
            state.SystemPolicy,
            state.ClosedWindowResources,
            qi: 0,
            state.TurnScopedFlags);
        return CoreDuelCardTurnTransition.Applied(state, stateAfter);
    }

    private static CoreDuelCardTurnTransition ApplyDeckTransition(
        CoreDuelCardTurnState state,
        BattleDeckTransition deckTransition)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(deckTransition);
        if (deckTransition.IsExactNoOp)
        {
            return CoreDuelCardTurnTransition.NoOp(
                state,
                deckTransition.NoOpReason ?? "deck_transition_rejected");
        }

        var stateAfter = CoreDuelCardTurnState.Create(
            deckTransition.DeckAfter,
            deckTransition.RngAfter,
            state.SystemPolicy,
            state.ClosedWindowResources,
            state.Qi,
            state.TurnScopedFlags);
        return CoreDuelCardTurnTransition.Applied(state, stateAfter);
    }

    private static bool OutcomesMatch(
        IReadOnlyList<DeferredPlayerChoice> choices,
        IReadOnlyList<AuthorizedDeferredChoiceOutcome> outcomes)
    {
        if (choices.Count != outcomes.Count)
        {
            return false;
        }

        for (var index = 0; index < choices.Count; index++)
        {
            var choice = choices[index];
            var outcome = outcomes[index];
            if (choice.CreatedSequence != outcome.CreatedSequence ||
                !StringComparer.Ordinal.Equals(choice.SourceId, outcome.SourceId) ||
                !StringComparer.Ordinal.Equals(choice.ChoiceId, outcome.ChoiceId))
            {
                return false;
            }
        }

        return true;
    }

    private static ClosedWindowResourceState RecreateResources(
        ClosedWindowResourceState source,
        int turnReservedDraw,
        int turnReservedQi) =>
        ClosedWindowResourceState.Create(
            turnReservedDraw,
            turnReservedQi,
            source.Soul,
            source.StandardCaptureRewardsClaimed,
            deferredChoices: [],
            source.FirstUseFlags,
            source.NextDeferredChoiceSequence);
}
