using Igorogue.Domain.Combat;

namespace Igorogue.Domain.Board;

public interface ICommittedPlacementFact : IBattleFact
{
}

public sealed class StoneTopologyRegisteredFact : ICommittedPlacementFact
{
    internal StoneTopologyRegisteredFact(
        StoneTopologyKey registeredTopologyKey,
        BattleRepetitionHistory historyAfterRegistration)
        : this(
            registeredTopologyKey,
            historyAfterRegistration,
            true,
            "legal_placement")
    {
    }

    internal StoneTopologyRegisteredFact(
        StoneTopologyKey registeredTopologyKey,
        BattleRepetitionHistory historyAfterRegistration,
        bool firstSeen,
        string sourceReasonId)
    {
        ArgumentNullException.ThrowIfNull(registeredTopologyKey);
        ArgumentNullException.ThrowIfNull(historyAfterRegistration);

        RegisteredTopologyKey = registeredTopologyKey;
        HistoryAfterRegistration = historyAfterRegistration;
        FirstSeen = firstSeen;
        SourceReasonId = StableDomainId.Validate(sourceReasonId, nameof(sourceReasonId));
    }

    public StoneTopologyKey RegisteredTopologyKey { get; }

    public BattleRepetitionHistory HistoryAfterRegistration { get; }

    public bool FirstSeen { get; }

    public string SourceReasonId { get; }
}

public sealed class KingCaptureEvaluatedFact : ICommittedPlacementFact
{
    internal KingCaptureEvaluatedFact(KingCaptureResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        Result = result;
    }

    public KingCaptureResult Result { get; }
}
