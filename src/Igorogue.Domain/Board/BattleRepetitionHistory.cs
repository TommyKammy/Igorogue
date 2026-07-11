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

    public BattleRepetitionHistory RegisterLegalPlacement(StoneTopologyKey unseenKey)
    {
        ArgumentNullException.ThrowIfNull(unseenKey);
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
        return new BattleRepetitionHistory(observations, nextSeenKeys);
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
