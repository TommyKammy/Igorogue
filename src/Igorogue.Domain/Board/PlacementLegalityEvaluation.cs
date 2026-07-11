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
        StoneTopologyKey? candidateTopologyKey)
    {
        Status = status;
        CandidateTopologyKey = candidateTopologyKey;
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

    internal static PlacementLegalityEvaluation Legal(StoneTopologyKey candidateTopologyKey) =>
        new(PlacementLegalityStatus.Legal, candidateTopologyKey);

    internal static PlacementLegalityEvaluation TerminalCaptureRequired() =>
        new(PlacementLegalityStatus.TerminalCaptureRequired, null);

    internal static PlacementLegalityEvaluation Suicide() =>
        new(PlacementLegalityStatus.Suicide, null);

    internal static PlacementLegalityEvaluation Repetition(StoneTopologyKey candidateTopologyKey) =>
        new(PlacementLegalityStatus.StoneTopologyRepetition, candidateTopologyKey);
}
