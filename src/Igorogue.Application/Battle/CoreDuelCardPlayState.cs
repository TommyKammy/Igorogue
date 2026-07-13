using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

using Igorogue.Application.Replay;
using Igorogue.Domain.Board;
using Igorogue.Domain.Cards;
using Igorogue.Domain.Combat;
using Igorogue.Domain.Determinism;
using Igorogue.Domain.Facilities;

namespace Igorogue.Application.Battle;

public sealed class PlayCardCommand : IBattleCommand
{
    public PlayCardCommand(
        string expectedStateChecksum,
        string expectedLogChecksum,
        string cardInstanceId,
        CanonicalPoint target,
        BasicStoneCardPlacementMode placementMode)
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
                "Unknown basic-stone placement mode.");
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

    public BasicStoneCardPlacementMode PlacementMode { get; }

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

    private static string PlacementModeId(BasicStoneCardPlacementMode mode) => mode switch
    {
        BasicStoneCardPlacementMode.Frontline => "frontline",
        BasicStoneCardPlacementMode.TerminalCapture => "terminal_capture",
        _ => throw new InvalidOperationException("Command contains an unknown placement mode."),
    };
}

public sealed class CoreDuelCardPlayState
{
    public const string EncodingVersion = "core-duel-card-play-state-v1";

    private CoreDuelCardPlayState(
        string contentHash,
        BattleState battleState,
        CoreDuelCardTurnState cardTurnState,
        BasicStoneCardPlayDefinition definition)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentHash);
        ArgumentNullException.ThrowIfNull(battleState);
        ArgumentNullException.ThrowIfNull(cardTurnState);
        ArgumentNullException.ThrowIfNull(definition);
        if (battleState.AuthoritativeRuntime is not null)
        {
            throw new ArgumentException(
                "TASK-0034 card play is a standalone legacy-placement proof; authoritative composition belongs to TASK-0039.",
                nameof(battleState));
        }

        if (!battleState.RngState.Equals(cardTurnState.RngState))
        {
            throw new ArgumentException(
                "Battle and card-turn state must share the same authoritative RNG snapshot.",
                nameof(cardTurnState));
        }

        ContentHash = contentHash;
        BattleState = battleState;
        CardTurnState = cardTurnState;
        Definition = definition;
        CanonicalText = CreateCanonicalText();
        Checksum = DeterministicChecksum.Sha256Hex(CanonicalText);
    }

    public string ContentHash { get; }

    public BattleState BattleState { get; }

    public CoreDuelCardTurnState CardTurnState { get; }

    public BasicStoneCardPlayDefinition Definition { get; }

    public string CanonicalText { get; }

    public string Checksum { get; }

    public bool IsTerminal => BattleState.IsTerminal;

    public string ToCanonicalText() => CanonicalText;

    internal static CoreDuelCardPlayState Create(
        string contentHash,
        BattleState battleState,
        CoreDuelCardTurnState cardTurnState,
        BasicStoneCardPlayDefinition definition) =>
        new(contentHash, battleState, cardTurnState, definition);

    private string CreateCanonicalText() => string.Join(
        '\n',
        EncodingVersion,
        $"content_hash={ContentHash}",
        $"basic_stone_definition={EncodeStableText(Definition.ToCanonicalText())}",
        $"battle_state={EncodeStableText(BattleState.ToCanonicalText())}",
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
        BoardState initialBoard,
        BattleRepetitionHistory repetitionHistory,
        FacilityState initialFacilities,
        BattleRuntimePolicy battlePolicy,
        CoreDuelCardTurnState cardTurnState,
        BasicStoneCardPlayDefinition definition,
        ReplayMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(initialBoard);
        ArgumentNullException.ThrowIfNull(repetitionHistory);
        ArgumentNullException.ThrowIfNull(initialFacilities);
        ArgumentNullException.ThrowIfNull(battlePolicy);
        ArgumentNullException.ThrowIfNull(cardTurnState);
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(metadata);
        if (metadata.InitialSeed != cardTurnState.RngState.InitialSeed)
        {
            throw new ArgumentException(
                "Replay metadata seed must match the card-turn RNG seed.",
                nameof(metadata));
        }

        var battleState = BattleState.Start(
            initialBoard,
            repetitionHistory,
            initialFacilities,
            cardTurnState.RngState,
            battlePolicy);
        var state = CoreDuelCardPlayState.Create(
            metadata.ContentHash,
            battleState,
            cardTurnState,
            definition);
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
        var evaluation = BasicStoneCardPlayEvaluator.Evaluate(
            source.Definition,
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
                "Authorized basic-stone play must bind a placement access mode.");
        if (!evaluation.IsBoundTo(
                source.Definition,
                source.CardTurnState.Deck,
                source.CardTurnState.Qi,
                command.CardInstanceId,
                source.BattleState.Board,
                command.Target,
                command.PlacementMode,
                accessMode))
        {
            throw new InvalidOperationException(
                "Basic-stone evaluation lost its exact source binding.");
        }

        var placement = AuthorizedStonePlacementPipeline.Resolve(
            source.BattleState,
            StoneColor.Black,
            command.Target,
            accessMode);
        if (!placement.Accepted)
        {
            return Reject(session, command, placement.ReasonId);
        }

        var cardTransition = CoreDuelCardTurnKernel.CommitBasicStoneCardPlay(
            source.CardTurnState,
            evaluation);
        if (cardTransition.IsExactNoOp)
        {
            throw new InvalidOperationException(
                $"Accepted placement could not commit its card: {cardTransition.ReasonId}.");
        }

        var stateAfter = CoreDuelCardPlayState.Create(
            source.ContentHash,
            placement.StateAfter,
            cardTransition.StateAfter,
            source.Definition);
        var nextLog = session.CommandLog.Append(command, stateAfter.Checksum);
        var nextSession = new CoreDuelCardPlaySession(stateAfter, nextLog);
        var facts = new List<IBattleFact>
        {
            QiChangedFact.SpendCardCost(
                source.CardTurnState.Qi,
                cardTransition.StateAfter.Qi,
                command.CardInstanceId),
        };
        facts.AddRange(placement.OrderedFacts);
        return new CoreDuelCardPlayResult(
            session,
            nextSession,
            command,
            true,
            "accepted",
            facts);
    }

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
