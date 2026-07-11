namespace Igorogue.Domain.Facilities;

public enum FacilityBuildStatus : byte
{
    Legal = 1,
    TargetHasStone = 2,
    TargetOccupied = 3,
    TargetNotOwnedTerritory = 4,
    CapacityFull = 5,
    TypeLimitReached = 6,
}

public sealed class FacilityBuildEvaluation
{
    internal FacilityBuildEvaluation(
        FacilityBuildStatus status,
        FacilityRuntimeAnalysis analysis,
        FacilityBuildRequest request)
    {
        if (status is not FacilityBuildStatus.Legal and
            not FacilityBuildStatus.TargetHasStone and
            not FacilityBuildStatus.TargetOccupied and
            not FacilityBuildStatus.TargetNotOwnedTerritory and
            not FacilityBuildStatus.CapacityFull and
            not FacilityBuildStatus.TypeLimitReached)
        {
            throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown build status.");
        }

        ArgumentNullException.ThrowIfNull(analysis);
        ArgumentNullException.ThrowIfNull(request);

        Status = status;
        Analysis = analysis;
        Request = request;
    }

    public FacilityBuildStatus Status { get; }

    public bool IsLegal => Status == FacilityBuildStatus.Legal;

    public string ReasonId => Status switch
    {
        FacilityBuildStatus.Legal => "legal",
        FacilityBuildStatus.TargetHasStone => "facility_target_has_stone",
        FacilityBuildStatus.TargetOccupied => "facility_target_occupied",
        FacilityBuildStatus.TargetNotOwnedTerritory =>
            "facility_target_not_owned_territory",
        FacilityBuildStatus.CapacityFull => "facility_capacity_full",
        FacilityBuildStatus.TypeLimitReached => "facility_type_limit_reached",
        _ => throw new InvalidOperationException("Unknown build status."),
    };

    public FacilityRuntimeAnalysis Analysis { get; }

    public FacilityBuildRequest Request { get; }
}
