namespace Igorogue.Domain.Board;

public static class PlacementLegalityEvaluator
{
    public static PlacementLegalityEvaluation Evaluate(
        HypotheticalPlacementResolution candidate,
        EffectiveLibertySnapshot postCaptureEffectiveLiberties,
        BattleRepetitionHistory history,
        PlacementAccessMode accessMode)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(postCaptureEffectiveLiberties);
        ArgumentNullException.ThrowIfNull(history);
        if (accessMode is not PlacementAccessMode.Normal and
            not PlacementAccessMode.TerminalCapture)
        {
            throw new ArgumentOutOfRangeException(
                nameof(accessMode),
                accessMode,
                "Unknown placement access mode.");
        }

        if (!ReferenceEquals(
                candidate.GroupsAfterCapture,
                postCaptureEffectiveLiberties.GroupAnalysis))
        {
            throw new ArgumentException(
                "Effective liberties must belong to the candidate's exact post-capture snapshot.",
                nameof(postCaptureEffectiveLiberties));
        }

        var sourceTopologyKey = StoneTopologyKey.FromBoard(candidate.SourceBoard);
        if (!history.Current.Equals(sourceTopologyKey))
        {
            throw new ArgumentException(
                "Battle repetition history must end at the candidate's source board.",
                nameof(history));
        }

        if (accessMode == PlacementAccessMode.TerminalCapture &&
            !candidate.SatisfiesTerminalCaptureCondition)
        {
            return PlacementLegalityEvaluation.TerminalCaptureRequired();
        }

        if (postCaptureEffectiveLiberties.EffectiveLibertiesFor(
                candidate.PlacedGroupAfterCapture) == 0)
        {
            return PlacementLegalityEvaluation.Suicide();
        }

        var candidateTopologyKey = StoneTopologyKey.FromBoard(candidate.BoardAfterCapture);
        return history.HasSeen(candidateTopologyKey)
            ? PlacementLegalityEvaluation.Repetition(candidateTopologyKey)
            : PlacementLegalityEvaluation.Legal(candidateTopologyKey);
    }
}
