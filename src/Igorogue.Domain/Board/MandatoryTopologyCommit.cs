namespace Igorogue.Domain.Board;

internal sealed class MandatoryTopologyCommit
{
    internal MandatoryTopologyCommit(
        BoardState sourceBoard,
        BoardState resultBoard,
        StoneTopologyKey registeredTopologyKey,
        bool firstSeen,
        BattleRepetitionHistory historyAfterCommit,
        StoneTopologyRegisteredFact registrationFact)
    {
        SourceBoard = sourceBoard;
        ResultBoard = resultBoard;
        RegisteredTopologyKey = registeredTopologyKey;
        FirstSeen = firstSeen;
        HistoryAfterCommit = historyAfterCommit;
        RegistrationFact = registrationFact;
    }

    internal BoardState SourceBoard { get; }

    internal BoardState ResultBoard { get; }

    internal StoneTopologyKey RegisteredTopologyKey { get; }

    internal bool FirstSeen { get; }

    internal BattleRepetitionHistory HistoryAfterCommit { get; }

    internal StoneTopologyRegisteredFact RegistrationFact { get; }
}
