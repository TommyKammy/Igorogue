namespace Igorogue.Domain.Board;

public enum PlacementAccessMode : byte
{
    // A non-terminal placement rule has already authorized the candidate point.
    Normal = 1,

    // The caller has an explicit terminal grant; the Domain still requires an immediate capture.
    TerminalCapture = 2,
}

public enum PlacementLegalityStatus : byte
{
    Legal = 1,
    TerminalCaptureRequired = 2,
    Suicide = 3,
    StoneTopologyRepetition = 4,
}

public sealed class PlacementLegalityEvaluation
{
    private PlacementLegalityEvaluation(
        PlacementLegalityStatus status,
        StoneTopologyKey? candidateTopologyKey,
        HypotheticalPlacementResolution? acceptedCandidate,
        BattleRepetitionHistory evaluatedHistory)
    {
        Status = status;
        CandidateTopologyKey = candidateTopologyKey;
        AcceptedCandidate = acceptedCandidate;
        EvaluatedHistory = evaluatedHistory;
    }

    public PlacementLegalityStatus Status { get; }

    public bool IsLegal => Status == PlacementLegalityStatus.Legal;

    public string ReasonId => Status switch
    {
        PlacementLegalityStatus.Legal => "legal",
        PlacementLegalityStatus.TerminalCaptureRequired => "terminal_capture_required",
        PlacementLegalityStatus.Suicide => "suicide",
        PlacementLegalityStatus.StoneTopologyRepetition => "stone_topology_repetition",
        _ => throw new InvalidOperationException("Unknown placement legality status."),
    };

    public StoneTopologyKey? CandidateTopologyKey { get; }

    public HypotheticalPlacementResolution? AcceptedCandidate { get; }

    internal BattleRepetitionHistory EvaluatedHistory { get; }

    internal static PlacementLegalityEvaluation Legal(
        HypotheticalPlacementResolution acceptedCandidate,
        StoneTopologyKey candidateTopologyKey,
        BattleRepetitionHistory evaluatedHistory) =>
        new(
            PlacementLegalityStatus.Legal,
            candidateTopologyKey,
            acceptedCandidate,
            evaluatedHistory);

    internal static PlacementLegalityEvaluation TerminalCaptureRequired(
        BattleRepetitionHistory evaluatedHistory) =>
        new(
            PlacementLegalityStatus.TerminalCaptureRequired,
            null,
            null,
            evaluatedHistory);

    internal static PlacementLegalityEvaluation Suicide(
        BattleRepetitionHistory evaluatedHistory) =>
        new(PlacementLegalityStatus.Suicide, null, null, evaluatedHistory);

    internal static PlacementLegalityEvaluation Repetition(
        StoneTopologyKey candidateTopologyKey,
        BattleRepetitionHistory evaluatedHistory) =>
        new(
            PlacementLegalityStatus.StoneTopologyRepetition,
            candidateTopologyKey,
            null,
            evaluatedHistory);
}
