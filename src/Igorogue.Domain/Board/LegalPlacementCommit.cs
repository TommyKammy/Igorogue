using Igorogue.Domain.Combat;

namespace Igorogue.Domain.Board;

public sealed class LegalPlacementCommit
{
    internal LegalPlacementCommit(
        HypotheticalPlacementResolution candidate,
        StoneTopologyKey registeredTopologyKey,
        EffectiveLibertySnapshot evaluatedEffectiveLiberties,
        BattleRepetitionHistory historyAfterCommit)
    {
        Candidate = candidate;
        RegisteredTopologyKey = registeredTopologyKey;
        EvaluatedEffectiveLiberties = evaluatedEffectiveLiberties;
        HistoryAfterCommit = historyAfterCommit;
        KingCaptureResult = KingCaptureResultEvaluator.EvaluateAtomicCapture(
            candidate.CapturedGroups);
    }

    public HypotheticalPlacementResolution Candidate { get; }

    public BoardState BoardAfterCommit => Candidate.BoardAfterCapture;

    public IReadOnlyList<PlacementCaptureFact> OrderedFacts => Candidate.OrderedFacts;

    public StoneTopologyKey RegisteredTopologyKey { get; }

    public BattleRepetitionHistory HistoryAfterCommit { get; }

    internal EffectiveLibertySnapshot EvaluatedEffectiveLiberties { get; }

    public KingCaptureResult KingCaptureResult { get; }
}
