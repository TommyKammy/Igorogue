using System.Globalization;

using Igorogue.Application.Replay;
using Igorogue.Domain.Board;
using Igorogue.Domain.Combat;
using Igorogue.Domain.Content;
using Igorogue.Domain.Facilities;

namespace Igorogue.Application.Battle;

/// <summary>
/// Creates a fresh authoritative Core Duel from the typed content catalog.
/// Hosts supply replay identity only; all runtime rules and starting values come
/// from the catalog's content-owned battle setup.
/// </summary>
public static class CoreDuelBattleStartup
{
    public static CoreDuelBattleStartResult Start(
        CoreDuelContentCatalog catalog,
        string gameVersion,
        long seed)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        var metadata = ReplayMetadata.Create(gameVersion, catalog.ContentHash, seed);
        return CoreDuelBattleStateMachine.Start(
            CreateInitialSnapshot(catalog.BattleSetup),
            catalog,
            metadata);
    }

    private static BattleAuthoritativeInitialSnapshot CreateInitialSnapshot(
        CoreDuelBattleSetupDefinition setup)
    {
        var board = BoardState.FromInitialPosition(setup.InitialPosition);
        var geometry = board.Geometry;
        var instances = board.OccupiedStones
            .Select((stone, index) => new StoneRuntimeInstance(
                $"initial.{setup.InitialPosition.Id}.stone." +
                geometry.ToCanonicalIndex(stone.Point).ToString("D2", CultureInfo.InvariantCulture),
                stone,
                stone.IsKing ? "king" : "standard",
                index + 1L,
                []))
            .ToArray();
        var stones = StoneRuntimeState.Create(board, instances, instances.Length + 1L);

        return BattleAuthoritativeInitialSnapshot.Create(
            stones,
            TemporaryLibertyState.Create(stones, [], nextCreatedSequence: 1),
            ContinuousLibertySnapshot.Empty(stones),
            BattleRepetitionHistory.Start(board),
            FacilityState.Create(board, [], nextBuildSequence: 1),
            ClosedWindowResourceState.Empty([]),
            CaptureBenefitTriggerPlan.Create([]),
            CounterattackBoundaryState.Create(
                setup.CounterattackStartGaugeUnits,
                pending: false,
                sacrificeStoneRemainder: 0,
                setup.CounterattackPolicy),
            setup.CounterattackPolicy,
            new BattleRuntimePolicy(setup.PlayerTurnLimit, setup.FacilityPolicy),
            playerTurnIndex: 1);
    }
}
