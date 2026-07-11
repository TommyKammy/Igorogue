using Igorogue.Domain.Board;

namespace Igorogue.Domain.Facilities;

public static class FacilityBuildEvaluator
{
    public static FacilityBuildEvaluation Evaluate(
        FacilityRuntimeAnalysis analysis,
        FacilityBuildRequest request)
    {
        ArgumentNullException.ThrowIfNull(analysis);
        ArgumentNullException.ThrowIfNull(request);

        var state = analysis.FacilityState;
        state.SourceBoard.Geometry.ToCanonicalIndex(request.Point);
        if (state.FacilityById(request.InstanceId) is not null)
        {
            throw new ArgumentException(
                $"Facility instance ID {request.InstanceId} is already installed.",
                nameof(request));
        }

        if (!state.SourceBoard.IsEmpty(request.Point))
        {
            return Evaluation(FacilityBuildStatus.TargetHasStone, analysis, request);
        }

        if (state.FacilityAt(request.Point) is not null)
        {
            return Evaluation(FacilityBuildStatus.TargetOccupied, analysis, request);
        }

        var region = analysis.RegionAt(request.Point)
            ?? throw new InvalidOperationException(
                "An empty facility build target must belong to a territory region.");
        if (!OwnerMatches(request.ActorColor, region.Region.Owner))
        {
            return Evaluation(
                FacilityBuildStatus.TargetNotOwnedTerritory,
                analysis,
                request);
        }

        if (region.InstalledCount >= region.ConstructionCapacity)
        {
            return Evaluation(FacilityBuildStatus.CapacityFull, analysis, request);
        }

        if (region.InstalledCountFor(request.FacilityContentId) >=
            analysis.Policy.TypeLimitFor(request.FacilityContentId))
        {
            return Evaluation(FacilityBuildStatus.TypeLimitReached, analysis, request);
        }

        return Evaluation(FacilityBuildStatus.Legal, analysis, request);
    }

    public static FacilityBuildCommit Commit(FacilityBuildEvaluation legalEvaluation)
    {
        ArgumentNullException.ThrowIfNull(legalEvaluation);
        if (!legalEvaluation.IsLegal)
        {
            throw new InvalidOperationException(
                "Only a legal facility build evaluation can be committed.");
        }

        var analysisBefore = legalEvaluation.Analysis;
        var stateBefore = analysisBefore.FacilityState;
        var request = legalEvaluation.Request;
        if (stateBefore.FacilityById(request.InstanceId) is not null ||
            !stateBefore.SourceBoard.IsEmpty(request.Point) ||
            stateBefore.FacilityAt(request.Point) is not null)
        {
            throw new InvalidOperationException(
                "The legal facility build evaluation no longer matches its source state.");
        }

        var builtFacility = new FacilityInstance(
            request.InstanceId,
            request.FacilityContentId,
            request.ActorColor,
            request.Point,
            stateBefore.NextBuildSequence);
        var nextSequence = checked(stateBefore.NextBuildSequence + 1);
        var stateAfter = FacilityState.Create(
            stateBefore.SourceBoard,
            stateBefore.InstalledFacilities.Append(builtFacility),
            nextSequence);
        var analysisAfter = FacilityRuntimeAnalyzer.Analyze(
            stateAfter,
            analysisBefore.TerritoryAnalysis,
            analysisBefore.Policy);
        var regionAfter = analysisAfter.RegionAt(request.Point)
            ?? throw new InvalidOperationException(
                "A committed facility must belong to its controlled territory region.");
        var builtFact = new FacilityBuiltFact(builtFacility);
        var activatedFact = new FacilityActivatedFact(
            builtFacility,
            regionAfter.Region,
            FacilityActivationReason.BuiltInControlledTerritory);

        return new FacilityBuildCommit(
            legalEvaluation,
            builtFacility,
            stateAfter,
            analysisAfter,
            [builtFact, activatedFact]);
    }

    private static FacilityBuildEvaluation Evaluation(
        FacilityBuildStatus status,
        FacilityRuntimeAnalysis analysis,
        FacilityBuildRequest request) =>
        new(status, analysis, request);

    private static bool OwnerMatches(StoneColor actor, TerritoryOwner owner) =>
        (actor, owner) switch
        {
            (StoneColor.Black, TerritoryOwner.Black) => true,
            (StoneColor.White, TerritoryOwner.White) => true,
            _ => false,
        };
}
