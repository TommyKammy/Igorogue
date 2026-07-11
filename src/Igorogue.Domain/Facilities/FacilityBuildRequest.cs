using Igorogue.Domain.Board;

namespace Igorogue.Domain.Facilities;

public sealed class FacilityBuildRequest
{
    public FacilityBuildRequest(
        StoneColor actorColor,
        CanonicalPoint point,
        string facilityContentId,
        string instanceId)
    {
        if (actorColor is not StoneColor.Black and not StoneColor.White)
        {
            throw new ArgumentOutOfRangeException(
                nameof(actorColor),
                actorColor,
                "Unknown facility builder color.");
        }

        ArgumentNullException.ThrowIfNull(point);
        facilityContentId = FacilityInstance.ValidateStableId(
            facilityContentId,
            nameof(facilityContentId));
        instanceId = FacilityInstance.ValidateStableId(instanceId, nameof(instanceId));

        ActorColor = actorColor;
        Point = point;
        FacilityContentId = facilityContentId;
        InstanceId = instanceId;
    }

    public StoneColor ActorColor { get; }

    public CanonicalPoint Point { get; }

    public string FacilityContentId { get; }

    public string InstanceId { get; }
}
