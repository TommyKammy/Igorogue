using Igorogue.Application.Replay;

namespace Igorogue.Application.Battle;

/// <summary>
/// Creates short-lived legacy/card and Bandit session projections around the one
/// aggregate snapshot. Their command logs are deliberately discarded; only the
/// outer <see cref="CoreDuelBattleSession"/> log is authoritative.
/// </summary>
internal static class CoreDuelBattleProjectionAdapter
{
    internal static CoreDuelCardPlaySession CreateTemporaryCardSession(
        CoreDuelBattleState source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (source.BattleState.Phase != BattlePhase.PlayerAction || source.IsTerminal)
        {
            throw new ArgumentException(
                "Card projection requires an ongoing player-action aggregate.",
                nameof(source));
        }

        var detachedBattle = BattleState.DetachAuthoritativeRuntime(source.BattleState);
        var cardState = CoreDuelCardPlayState.Create(
            source.Bootstrap.Catalog.ContentHash,
            detachedBattle,
            source.RuntimeState,
            source.CardTurnState,
            source.Bootstrap.StoneDefinitions,
            source.Bootstrap.ReinforceDefinition,
            source.Bootstrap.DevelopmentDefinition);
        return new CoreDuelCardPlaySession(
            cardState,
            OrderedCommandLog.Create(source.Bootstrap.Metadata));
    }

    internal static BattleState AttachCardBattle(CoreDuelCardPlayState cardState)
    {
        ArgumentNullException.ThrowIfNull(cardState);
        return BattleState.AttachAuthoritativeRuntime(
            cardState.BattleState,
            cardState.RuntimeState);
    }

    internal static BanditEnemyTurnSession CreateTemporaryBanditSession(
        CoreDuelBattleState source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var battleSession = CreateTemporaryHeadlessSession(
            source.BattleState,
            source.Bootstrap.Metadata);
        var banditState = BanditEnemyTurnState.Create(
            source.Bootstrap.Catalog.ContentHash,
            source.Bootstrap.Catalog.Bandit,
            battleSession,
            source.NormalPlan,
            source.BonusPlan);
        return new BanditEnemyTurnSession(
            banditState,
            OrderedCommandLog.Create(source.Bootstrap.Metadata));
    }

    internal static HeadlessBattleSession CreateTemporaryHeadlessSession(
        BattleState state,
        ReplayMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(metadata);
        return new HeadlessBattleSession(
            state,
            OrderedCommandLog.Create(metadata));
    }

    internal static CoreDuelCardTurnState SynchronizeCardTurnResources(
        CoreDuelCardTurnState source,
        BattleAuthoritativeRuntimeState runtime)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(runtime);
        if (ReferenceEquals(source.ClosedWindowResources, runtime.ClosedWindowResources))
        {
            return source;
        }

        return CoreDuelCardTurnState.Create(
            source.Deck,
            source.RngState,
            source.SystemPolicy,
            runtime.ClosedWindowResources,
            source.Qi,
            source.TurnScopedFlags);
    }

    internal static BattleState SynchronizeBattleWithCardTurn(
        BattleState source,
        CoreDuelCardTurnState cardTurnState)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(cardTurnState);
        var runtime = source.AuthoritativeRuntime
            ?? throw new ArgumentException(
                "Core Duel synchronization requires authoritative runtime state.",
                nameof(source));
        var reboundRuntime = ReferenceEquals(
                runtime.ClosedWindowResources,
                cardTurnState.ClosedWindowResources)
            ? runtime
            : runtime.Transition(
                closedWindowResources: cardTurnState.ClosedWindowResources);
        var reboundBattle = ReferenceEquals(runtime, reboundRuntime)
            ? source
            : BattleState.RebindAuthoritativeRuntime(source, reboundRuntime);
        return BattleState.RebindRng(reboundBattle, cardTurnState.RngState);
    }
}
