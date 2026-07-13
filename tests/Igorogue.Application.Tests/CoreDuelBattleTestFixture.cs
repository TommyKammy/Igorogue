using System.Globalization;

using Igorogue.Application.Battle;
using Igorogue.Application.Replay;
using Igorogue.Content;
using Igorogue.Domain.Board;
using Igorogue.Domain.Combat;
using Igorogue.Domain.Content;
using Igorogue.Domain.Facilities;

namespace Igorogue.Application.Tests;

internal static class CoreDuelBattleTestFixture
{
    internal const string GameVersion = "v0.2.10";
    internal const long Seed = 39039;

    internal static CoreDuelContentCatalog LoadCatalog()
    {
        var root = GoldenBoardFixtureAdapter.FindRepositoryRoot();
        return new CoreDuelContentCatalogLoader().Load(Path.Combine(
            root.FullName,
            "build",
            "generated_content",
            "content_manifest.json"));
    }

    internal static CoreDuelBattleStartResult Start(
        long seed = Seed,
        int playerTurnLimit = 20)
    {
        var catalog = LoadCatalog();
        return CoreDuelBattleStateMachine.Start(
            InitialSnapshot(playerTurnLimit),
            catalog,
            ReplayMetadata.Create(GameVersion, catalog.ContentHash, seed));
    }

    internal static CoreDuelBattleStartResult StartLethal(
        long seed = Seed,
        int playerTurnLimit = 20)
    {
        var catalog = LoadCatalog();
        return CoreDuelBattleStateMachine.Start(
            LethalInitialSnapshot(playerTurnLimit),
            catalog,
            ReplayMetadata.Create(GameVersion, catalog.ContentHash, seed));
    }

    internal static BattleAuthoritativeInitialSnapshot InitialSnapshot(
        int playerTurnLimit = 20)
    {
        var geometry = BoardGeometry.Create(BoardGeometry.AcceptedSize);
        return CreateInitialSnapshot(
            geometry,
            [
                Stone(geometry, StoneColor.Black, isKing: true, 2, 2),
                Stone(geometry, StoneColor.Black, isKing: false, 2, 3),
                Stone(geometry, StoneColor.Black, isKing: false, 3, 2),
                Stone(geometry, StoneColor.White, isKing: true, 6, 6),
                Stone(geometry, StoneColor.White, isKing: false, 5, 6),
                Stone(geometry, StoneColor.White, isKing: false, 6, 5),
            ],
            playerTurnLimit);
    }

    internal static BattleAuthoritativeInitialSnapshot LethalInitialSnapshot(
        int playerTurnLimit = 20)
    {
        var geometry = BoardGeometry.Create(BoardGeometry.AcceptedSize);
        return CreateInitialSnapshot(
            geometry,
            [
                Stone(geometry, StoneColor.Black, isKing: true, 2, 2),
                Stone(geometry, StoneColor.White, isKing: false, 1, 2),
                Stone(geometry, StoneColor.White, isKing: false, 2, 1),
                Stone(geometry, StoneColor.White, isKing: false, 3, 2),
                Stone(geometry, StoneColor.White, isKing: true, 6, 6),
            ],
            playerTurnLimit);
    }

    internal static CanonicalPoint Point(int x, int y) =>
        BoardGeometry.Create(BoardGeometry.AcceptedSize).CreateCanonicalPoint(x, y);

    private static BattleAuthoritativeInitialSnapshot CreateInitialSnapshot(
        BoardGeometry geometry,
        IReadOnlyList<BoardStone> initialStones,
        int playerTurnLimit)
    {
        var board = BoardState.Create(geometry, initialStones);
        var instances = board.OccupiedStones
            .Select((stone, index) => new StoneRuntimeInstance(
                "initial.standard_v0_2.stone." +
                geometry.ToCanonicalIndex(stone.Point).ToString("D2", CultureInfo.InvariantCulture),
                stone,
                stone.IsKing ? "king" : "standard",
                index + 1L,
                []))
            .ToArray();
        var stones = StoneRuntimeState.Create(board, instances, instances.Length + 1L);
        var facilityPolicy = FacilityPolicy();
        var counterattackPolicy = new CounterattackBoundaryPolicy(
            thresholdUnits: 200,
            enemyTurnEndNaturalGainUnits: 12,
            sacrificeStonesPerBatch: 3,
            sacrificeUnitsPerBatch: 30);

        return BattleAuthoritativeInitialSnapshot.Create(
            stones,
            TemporaryLibertyState.Create(stones, [], nextCreatedSequence: 1),
            ContinuousLibertySnapshot.Empty(stones),
            BattleRepetitionHistory.Start(board),
            FacilityState.Create(board, [], nextBuildSequence: 1),
            ClosedWindowResourceState.Empty([]),
            CaptureBenefitTriggerPlan.Create([]),
            CounterattackBoundaryState.Create(
                gaugeUnits: 20,
                pending: false,
                sacrificeStoneRemainder: 0,
                counterattackPolicy),
            counterattackPolicy,
            new BattleRuntimePolicy(playerTurnLimit, facilityPolicy),
            playerTurnIndex: 1);
    }

    private static FacilityRuntimePolicy FacilityPolicy() =>
        FacilityRuntimePolicy.Create(
            territoryIncomeDivisor: 3,
            capacityBands:
            [
                new FacilityCapacityBand(1, 3, 1),
                new FacilityCapacityBand(4, 7, 2),
                new FacilityCapacityBand(8, 12, 3),
                new FacilityCapacityBand(13, 49, 4),
            ],
            slotCap: 5,
            typeLimits:
            [
                new KeyValuePair<string, int>("default", 1),
                new KeyValuePair<string, int>("development", 2),
                new KeyValuePair<string, int>("furnace", 2),
            ]);

    private static BoardStone Stone(
        BoardGeometry geometry,
        StoneColor color,
        bool isKing,
        int x,
        int y) =>
        new(color, isKing, geometry.CreateCanonicalPoint(x, y));
}
