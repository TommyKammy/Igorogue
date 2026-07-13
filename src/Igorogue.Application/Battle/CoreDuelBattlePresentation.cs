using Igorogue.Domain.Board;

namespace Igorogue.Application.Battle;

public sealed class CoreDuelStonePreview
{
    internal CoreDuelStonePreview(
        string instanceId,
        string kindId,
        string colorId,
        bool isKing)
    {
        InstanceId = instanceId;
        KindId = kindId;
        ColorId = colorId;
        IsKing = isKing;
    }

    public string InstanceId { get; }

    public string KindId { get; }

    public string ColorId { get; }

    public bool IsKing { get; }
}

public sealed class CoreDuelBoardPointPreview
{
    internal CoreDuelBoardPointPreview(
        CanonicalPoint point,
        CoreDuelStonePreview? stone,
        string territoryOwnerId,
        CoreDuelFacilityPreview? facility)
    {
        Point = point;
        Stone = stone;
        TerritoryOwnerId = territoryOwnerId;
        Facility = facility;
    }

    public CanonicalPoint Point { get; }

    public CoreDuelStonePreview? Stone { get; }

    public string TerritoryOwnerId { get; }

    public CoreDuelFacilityPreview? Facility { get; }
}

public sealed class CoreDuelCardInstancePreview
{
    internal CoreDuelCardInstancePreview(
        string instanceId,
        string contentId)
    {
        InstanceId = instanceId;
        ContentId = contentId;
    }

    public string InstanceId { get; }

    public string ContentId { get; }
}
