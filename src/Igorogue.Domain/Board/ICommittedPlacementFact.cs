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
    {
        ArgumentNullException.ThrowIfNull(registeredTopologyKey);
        ArgumentNullException.ThrowIfNull(historyAfterRegistration);

        RegisteredTopologyKey = registeredTopologyKey;
        HistoryAfterRegistration = historyAfterRegistration;
    }

    public StoneTopologyKey RegisteredTopologyKey { get; }

    public BattleRepetitionHistory HistoryAfterRegistration { get; }
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
