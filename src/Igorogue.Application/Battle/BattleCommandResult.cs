using System.Collections.ObjectModel;
using Igorogue.Domain.Combat;

namespace Igorogue.Application.Battle;

public sealed class BattleCommandResult
{
    private readonly ReadOnlyCollection<IBattleFact> orderedFactView;

    internal BattleCommandResult(
        HeadlessBattleSession sessionBefore,
        HeadlessBattleSession sessionAfter,
        IBattleCommand command,
        bool accepted,
        string reasonId,
        IBattleFact[] orderedFacts)
    {
        ArgumentNullException.ThrowIfNull(sessionBefore);
        ArgumentNullException.ThrowIfNull(sessionAfter);
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(reasonId);
        ArgumentNullException.ThrowIfNull(orderedFacts);
        foreach (var fact in orderedFacts)
        {
            ArgumentNullException.ThrowIfNull(fact);
        }

        if (!accepted &&
            (!ReferenceEquals(sessionBefore, sessionAfter) || orderedFacts.Length != 1))
        {
            throw new ArgumentException(
                "Rejected command results must preserve the exact session and one diagnostic fact.");
        }

        SessionBefore = sessionBefore;
        SessionAfter = sessionAfter;
        Command = command;
        Accepted = accepted;
        ReasonId = reasonId;
        orderedFactView = Array.AsReadOnly((IBattleFact[])orderedFacts.Clone());
    }

    public HeadlessBattleSession SessionBefore { get; }

    public HeadlessBattleSession SessionAfter { get; }

    public IBattleCommand Command { get; }

    public bool Accepted { get; }

    public string ReasonId { get; }

    public IReadOnlyList<IBattleFact> OrderedFacts => orderedFactView;

    public string StateChecksum => SessionAfter.State.Checksum;

    public string LogChecksum => SessionAfter.CommandLog.CurrentChecksum;
}
