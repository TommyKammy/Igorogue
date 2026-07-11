using Igorogue.Application.Replay;

namespace Igorogue.Application.Battle;

public sealed class HeadlessBattleSession
{
    internal HeadlessBattleSession(
        BattleState state,
        OrderedCommandLog commandLog)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(commandLog);
        if (commandLog.Metadata.InitialSeed != state.RngState.InitialSeed)
        {
            throw new ArgumentException(
                "Command log metadata seed must match the authoritative battle RNG seed.",
                nameof(commandLog));
        }

        if (commandLog.Entries.Count > 0 &&
            !string.Equals(
                commandLog.Entries[^1].ResultChecksum,
                state.Checksum,
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "The last command-log result checksum must match the battle state.",
                nameof(commandLog));
        }

        State = state;
        CommandLog = commandLog;
    }

    public BattleState State { get; }

    public OrderedCommandLog CommandLog { get; }
}
