using System.Collections.ObjectModel;
using System.Globalization;

namespace Igorogue.Domain.Board;

public sealed class BattleRepetitionHistory
{
    public const string EncodingVersion = "battle-repetition-history-v1";

    private readonly ReadOnlyCollection<StoneTopologyKey> observationView;
    private readonly HashSet<StoneTopologyKey> seenKeys;

    private BattleRepetitionHistory(
        StoneTopologyKey[] orderedObservations,
        HashSet<StoneTopologyKey> seenKeys)
    {
        observationView = Array.AsReadOnly(
            (StoneTopologyKey[])orderedObservations.Clone());
        this.seenKeys = new HashSet<StoneTopologyKey>(seenKeys);
    }

    public IReadOnlyList<StoneTopologyKey> OrderedObservations => observationView;

    public int ObservationCount => observationView.Count;

    public int UniqueKeyCount => seenKeys.Count;

    public StoneTopologyKey Current => observationView[^1];

    public static BattleRepetitionHistory Start(BoardState initialBoard)
    {
        ArgumentNullException.ThrowIfNull(initialBoard);
        return FromObservedBoards([initialBoard]);
    }

    public static BattleRepetitionHistory FromObservedBoards(
        IEnumerable<BoardState> observedBoards)
    {
        ArgumentNullException.ThrowIfNull(observedBoards);

        var observations = observedBoards
            .Select(board => StoneTopologyKey.FromBoard(
                board ?? throw new ArgumentNullException(nameof(observedBoards))))
            .ToArray();
        if (observations.Length == 0)
        {
            throw new ArgumentException(
                "Battle repetition history must contain the initial board at index 0.",
                nameof(observedBoards));
        }

        return new BattleRepetitionHistory(
            observations,
            new HashSet<StoneTopologyKey>(observations));
    }

    public bool HasSeen(StoneTopologyKey key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return seenKeys.Contains(key);
    }

    public LegalPlacementCommit CommitLegalPlacement(
        PlacementLegalityEvaluation legalEvaluation)
    {
        ArgumentNullException.ThrowIfNull(legalEvaluation);
        if (!legalEvaluation.IsLegal ||
            legalEvaluation.AcceptedCandidate is null ||
            legalEvaluation.CandidateTopologyKey is null)
        {
            throw new InvalidOperationException(
                "Only a legal placement evaluation can register a topology observation.");
        }

        if (!ReferenceEquals(this, legalEvaluation.EvaluatedHistory))
        {
            throw new ArgumentException(
                "The legal placement evaluation belongs to a different battle history.",
                nameof(legalEvaluation));
        }

        var candidate = legalEvaluation.AcceptedCandidate
            ?? throw new InvalidOperationException(
                "A legal placement evaluation must retain its accepted candidate.");
        var sourceKey = StoneTopologyKey.FromBoard(candidate.SourceBoard);
        if (!Current.Equals(sourceKey))
        {
            throw new InvalidOperationException(
                "The battle history no longer ends at the evaluated source board.");
        }

        var unseenKey = StoneTopologyKey.FromBoard(candidate.BoardAfterCapture);
        if (!unseenKey.Equals(legalEvaluation.CandidateTopologyKey))
        {
            throw new InvalidOperationException(
                "The accepted candidate no longer matches its evaluated topology key.");
        }

        if (seenKeys.Contains(unseenKey))
        {
            throw new InvalidOperationException(
                "A legal placement cannot register an already observed stone topology.");
        }

        var observations = new StoneTopologyKey[observationView.Count + 1];
        observationView.CopyTo(observations, 0);
        observations[^1] = unseenKey;
        var nextSeenKeys = new HashSet<StoneTopologyKey>(seenKeys)
        {
            unseenKey,
        };
        var historyAfterCommit = new BattleRepetitionHistory(observations, nextSeenKeys);
        return new LegalPlacementCommit(
            candidate,
            unseenKey,
            legalEvaluation.EvaluatedEffectiveLiberties,
            historyAfterCommit);
    }

    internal MandatoryTopologyCommit CommitMandatoryMutation(
        BoardState sourceBoard,
        BoardState resultBoard,
        string sourceReasonId)
    {
        ArgumentNullException.ThrowIfNull(sourceBoard);
        ArgumentNullException.ThrowIfNull(resultBoard);
        var reasonId = StableDomainId.Validate(sourceReasonId, nameof(sourceReasonId));
        if (!ReferenceEquals(sourceBoard.Geometry, resultBoard.Geometry))
        {
            throw new ArgumentException(
                "Mandatory mutation boards must use the exact same geometry.",
                nameof(resultBoard));
        }

        var sourceKey = StoneTopologyKey.FromBoard(sourceBoard);
        if (!Current.Equals(sourceKey))
        {
            throw new InvalidOperationException(
                "The battle history no longer ends at the mandatory mutation source board.");
        }

        var resultKey = StoneTopologyKey.FromBoard(resultBoard);
        if (resultKey.Equals(sourceKey))
        {
            throw new ArgumentException(
                "Mandatory topology registration requires an actual stone topology change.",
                nameof(resultBoard));
        }

        var firstSeen = !seenKeys.Contains(resultKey);
        var observations = new StoneTopologyKey[observationView.Count + 1];
        observationView.CopyTo(observations, 0);
        observations[^1] = resultKey;
        var nextSeenKeys = new HashSet<StoneTopologyKey>(seenKeys)
        {
            resultKey,
        };
        var historyAfterCommit = new BattleRepetitionHistory(observations, nextSeenKeys);
        var registrationFact = new StoneTopologyRegisteredFact(
            resultKey,
            historyAfterCommit,
            firstSeen,
            reasonId);

        return new MandatoryTopologyCommit(
            sourceBoard,
            resultBoard,
            resultKey,
            firstSeen,
            historyAfterCommit,
            registrationFact);
    }

    public string ToCanonicalText()
    {
        var lines = new string[observationView.Count + 2];
        lines[0] = EncodingVersion;
        lines[1] = observationView.Count.ToString(CultureInfo.InvariantCulture);
        for (var index = 0; index < observationView.Count; index++)
        {
            lines[index + 2] = observationView[index].ToCanonicalText();
        }

        return string.Join('\n', lines);
    }
}
