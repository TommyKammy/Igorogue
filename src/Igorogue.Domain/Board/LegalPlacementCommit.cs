namespace Igorogue.Domain.Board;

public sealed class LegalPlacementCommit
{
    internal LegalPlacementCommit(
        HypotheticalPlacementResolution candidate,
        StoneTopologyKey registeredTopologyKey,
        BattleRepetitionHistory historyAfterCommit)
    {
        Candidate = candidate;
        RegisteredTopologyKey = registeredTopologyKey;
        HistoryAfterCommit = historyAfterCommit;
    }

    public HypotheticalPlacementResolution Candidate { get; }

    public BoardState BoardAfterCommit => Candidate.BoardAfterCapture;

    public IReadOnlyList<PlacementCaptureFact> OrderedFacts => Candidate.OrderedFacts;

    public StoneTopologyKey RegisteredTopologyKey { get; }

    public BattleRepetitionHistory HistoryAfterCommit { get; }
}
