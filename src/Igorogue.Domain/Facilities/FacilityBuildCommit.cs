using System.Collections.ObjectModel;

namespace Igorogue.Domain.Facilities;

public sealed class FacilityBuildCommit
{
    private readonly ReadOnlyCollection<FacilityFact> orderedFactView;

    internal FacilityBuildCommit(
        FacilityBuildEvaluation evaluation,
        FacilityInstance builtFacility,
        FacilityState stateAfterCommit,
        FacilityRuntimeAnalysis analysisAfterCommit,
        FacilityFact[] orderedFacts)
    {
        ArgumentNullException.ThrowIfNull(evaluation);
        ArgumentNullException.ThrowIfNull(builtFacility);
        ArgumentNullException.ThrowIfNull(stateAfterCommit);
        ArgumentNullException.ThrowIfNull(analysisAfterCommit);
        ArgumentNullException.ThrowIfNull(orderedFacts);

        Evaluation = evaluation;
        BuiltFacility = builtFacility;
        StateAfterCommit = stateAfterCommit;
        AnalysisAfterCommit = analysisAfterCommit;
        orderedFactView = Array.AsReadOnly((FacilityFact[])orderedFacts.Clone());
    }

    public FacilityBuildEvaluation Evaluation { get; }

    public FacilityRuntimeAnalysis AnalysisBeforeCommit => Evaluation.Analysis;

    public FacilityInstance BuiltFacility { get; }

    public FacilityState StateAfterCommit { get; }

    public FacilityRuntimeAnalysis AnalysisAfterCommit { get; }

    public IReadOnlyList<FacilityFact> OrderedFacts => orderedFactView;
}
