using Igorogue.Domain.Board;
using Igorogue.Domain.Combat;

namespace Igorogue.Domain.Tests;

public sealed class TemporaryLibertyStateTests
{
    private static readonly BoardGeometry Geometry = BoardGeometry.Create(7);

    [Fact]
    public void GrantAnchorsCanonicalFirstStoneAndUsesMonotonicSequence()
    {
        var board = Board(
            Stone(StoneColor.Black, 4, 4),
            Stone(StoneColor.Black, 3, 4));
        var stones = Runtime(board);
        var source = TemporaryLibertyState.Create(stones, [], 7);

        var grant = TemporaryLibertyGrantResolver.Grant(
            source,
            C(4, 4),
            "effect.reinforce",
            1,
            "card_reinforce",
            3);

        Assert.Same(source, grant.SourceState);
        Assert.Equal(7, grant.GrantedEffect.CreatedSequence);
        Assert.Equal(8, grant.StateAfterGrant.NextCreatedSequence);
        Assert.Equal(3, grant.GrantedEffect.ExpiresAfterEnemyTurnIndex);
        Assert.Equal(
            stones.InstanceAt(C(3, 4))!.InstanceId,
            grant.GrantedEffect.AnchorStoneInstanceId);
        Assert.Equal(StoneColor.Black, grant.GrantedEffect.OwnerColor);
        Assert.Equal(C(3, 4), grant.GrantedFact.TargetGroupAnchor);
        Assert.Same(grant.GrantedEffect, grant.GrantedFact.Effect);
    }

    [Fact]
    public void GrantAfterSweepStartMovesExpiryToNextEnemyBoundary()
    {
        var board = Board(Stone(StoneColor.Black, 4, 4));
        var stones = Runtime(board);
        var started = TemporaryLibertyState.Create(stones, [], 1);
        var sweep = TemporaryLibertyExpiryResolver.Resolve(
            stones,
            started,
            ContinuousLibertySnapshot.Empty(stones),
            BattleRepetitionHistory.Start(board),
            5);
        Assert.True(sweep.IsExactNoOp);

        var grant = TemporaryLibertyGrantResolver.GrantAfterExpirySweepStarted(
            started,
            C(4, 4),
            "effect.late",
            1,
            "trigger.late",
            sweep.SweepWindow);

        Assert.Equal(6, grant.GrantedEffect.ExpiresAfterEnemyTurnIndex);
        Assert.Equal(5, grant.StateAfterGrant.ExpirySweepStartedForEnemyTurnIndex);
        Assert.Throws<ArgumentException>(() => TemporaryLibertyGrantResolver.Grant(
            grant.StateAfterGrant,
            C(4, 4),
            "effect.after-via-before-api",
            1,
            "trigger",
            5));
        Assert.Throws<ArgumentException>(() => TemporaryLibertyGrantResolver.Grant(
            TemporaryLibertyState.Create(stones, [], 1, 6),
            C(4, 4),
            "effect.past",
            1,
            "trigger",
            4));
        var foreignBoard = Board(Stone(StoneColor.Black, 1, 1));
        var foreignStones = Runtime(foreignBoard);
        var foreignTemporary = TemporaryLibertyState.Create(foreignStones, [], 1);
        var foreignSweep = TemporaryLibertyExpiryResolver.Resolve(
            foreignStones,
            foreignTemporary,
            ContinuousLibertySnapshot.Empty(foreignStones),
            BattleRepetitionHistory.Start(foreignBoard),
            5);
        Assert.Throws<ArgumentException>(() =>
            TemporaryLibertyGrantResolver.GrantAfterExpirySweepStarted(
            started,
            C(4, 4),
            "effect.foreign-window",
            1,
            "trigger",
            foreignSweep.SweepWindow));
    }

    [Fact]
    public void PostSweepGrantRejectsPreSweepStateAndChainsExactWindowState()
    {
        var board = Board(Stone(StoneColor.Black, 4, 4));
        var stones = Runtime(board);
        var anchor = Assert.Single(stones.Instances);
        var due = Effect(
            "effect.due",
            anchor.InstanceId,
            1,
            5,
            StoneColor.Black);
        var beforeSweep = TemporaryLibertyState.Create(stones, [due], 2);
        var sweep = TemporaryLibertyExpiryResolver.Resolve(
            stones,
            beforeSweep,
            ContinuousLibertySnapshot.Empty(stones),
            BattleRepetitionHistory.Start(board),
            5);

        Assert.Same(stones, sweep.StonesAfterResolution);
        Assert.NotSame(beforeSweep, sweep.TemporaryLibertiesAfterResolution);
        Assert.Empty(sweep.TemporaryLibertiesAfterResolution.Effects);
        Assert.Throws<ArgumentException>(() =>
            TemporaryLibertyGrantResolver.GrantAfterExpirySweepStarted(
                beforeSweep,
                C(4, 4),
                "effect.stale",
                1,
                "trigger",
                sweep.SweepWindow));

        var first = TemporaryLibertyGrantResolver.GrantAfterExpirySweepStarted(
            sweep.TemporaryLibertiesAfterResolution,
            C(4, 4),
            "effect.first",
            1,
            "trigger",
            sweep.SweepWindow);
        var continuedWindow = Assert.IsType<TemporaryLibertyExpirySweepWindow>(
            first.ContinuedSweepWindow);
        var second = TemporaryLibertyGrantResolver.GrantAfterExpirySweepStarted(
            first.StateAfterGrant,
            C(4, 4),
            "effect.second",
            1,
            "trigger",
            continuedWindow);

        Assert.Equal(
            new[] { "effect.first", "effect.second" },
            second.StateAfterGrant.Effects.Select(effect => effect.EffectInstanceId));
        Assert.All(
            second.StateAfterGrant.Effects,
            effect => Assert.Equal(6, effect.ExpiresAfterEnemyTurnIndex));
        Assert.NotNull(second.ContinuedSweepWindow);
    }

    [Fact]
    public void CreateCanonicalizesEffectsAndRejectsInvalidIdentity()
    {
        var board = Board(Stone(StoneColor.Black, 4, 4));
        var stones = Runtime(board);
        var anchorId = stones.Instances[0].InstanceId;
        var later = Effect("effect-b", anchorId, 2, 3, StoneColor.Black);
        var earlier = Effect("effect-a", anchorId, 1, 3, StoneColor.Black);

        var state = TemporaryLibertyState.Create(stones, [later, earlier], 3);
        var reversed = TemporaryLibertyState.Create(stones, [earlier, later], 3);

        Assert.Equal(new[] { "effect-a", "effect-b" },
            state.Effects.Select(effect => effect.EffectInstanceId));
        Assert.Equal(state.ToCanonicalText(), reversed.ToCanonicalText());
        Assert.Throws<ArgumentException>(() =>
            TemporaryLibertyState.Create(stones, [earlier, earlier], 3));
        var sameSequence = TemporaryLibertyState.Create(
            stones,
            [
                Effect("effect-b", anchorId, 1, 3, StoneColor.Black),
                Effect("effect-a", anchorId, 1, 3, StoneColor.Black),
            ],
            2);
        Assert.Equal(
            new[] { "effect-a", "effect-b" },
            sameSequence.Effects.Select(effect => effect.EffectInstanceId));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            TemporaryLibertyState.Create(stones, [earlier, later], 2));
        Assert.Throws<ArgumentException>(() =>
            TemporaryLibertyState.Create(
                stones,
                [Effect("wrong-owner", anchorId, 1, 3, StoneColor.White)],
                2));
        Assert.Throws<ArgumentException>(() =>
            TemporaryLibertyState.Create(
                stones,
                [Effect("missing", "no-stone", 1, 3, StoneColor.Black)],
                2));
        Assert.Throws<ArgumentException>(() =>
            TemporaryLibertyState.Create(
                stones,
                [Effect("due-at-marker", anchorId, 1, 5, StoneColor.Black)],
                2,
                5));
        Assert.Throws<ArgumentException>(() =>
            TemporaryLibertyState.Create(
                stones,
                [Effect("overdue-at-marker", anchorId, 1, 4, StoneColor.Black)],
                2,
                5));
        var restoredAfterSweep = TemporaryLibertyState.Create(
            stones,
            [Effect("future-after-marker", anchorId, 1, 6, StoneColor.Black)],
            2,
            5);
        Assert.Equal(5, restoredAfterSweep.ExpirySweepStartedForEnemyTurnIndex);
    }

    [Fact]
    public void CarrierRemovalDropsAnchoredEffectAndPreventsLaterExpiry()
    {
        var board = Board(
            Stone(StoneColor.Black, 4, 4),
            Stone(StoneColor.White, 4, 5));
        var stones = Runtime(board);
        var anchorId = stones.InstanceAt(C(4, 4))!.InstanceId;
        var effect = Effect("effect.carrier", anchorId, 1, 9, StoneColor.Black);
        var temporary = TemporaryLibertyState.Create(stones, [effect], 2);
        var resultBoard = BoardState.Create(
            Geometry,
            board.OccupiedStones.Where(stone => stone.Point != C(4, 4)));
        var resultStones = stones.RebindAfterRemoval(resultBoard);

        var removal = TemporaryLibertyCarrierRemovalResolver.Resolve(
            temporary,
            resultStones);

        Assert.Equal(new[] { "effect.carrier" },
            removal.RemovedEffects.Select(item => item.EffectInstanceId));
        var fact = Assert.Single(removal.OrderedFacts);
        Assert.Equal("carrier_removed", fact.ReasonId);
        Assert.Empty(removal.StateAfterRemoval.Effects);
        Assert.Same(resultStones, removal.StateAfterRemoval.SourceStones);

        var history = BattleRepetitionHistory.Start(resultBoard);
        var laterSweep = TemporaryLibertyExpiryResolver.Resolve(
            resultStones,
            removal.StateAfterRemoval,
            ContinuousLibertySnapshot.Empty(resultStones),
            history,
            9);
        Assert.True(laterSweep.IsExactNoOp);
        Assert.Empty(laterSweep.ExpiredEffects);
        Assert.Empty(laterSweep.OrderedFacts);
    }

    [Fact]
    public void CarrierRemovalRejectsRecreatedLiveInstanceIdentity()
    {
        var board = Board(Stone(StoneColor.Black, 4, 4));
        var stones = Runtime(board);
        var temporary = TemporaryLibertyState.Create(stones, [], 1);
        var source = Assert.Single(stones.Instances);
        var recreated = new StoneRuntimeInstance(
            source.InstanceId,
            source.Stone,
            source.KindId,
            source.CreatedSequence,
            source.OrderedEffectMetadata);
        var foreignRuntime = StoneRuntimeState.Create(board, [recreated], 2);

        Assert.Throws<ArgumentException>(() =>
            TemporaryLibertyCarrierRemovalResolver.Resolve(temporary, foreignRuntime));
    }

    private static TemporaryLibertyEffect Effect(
        string id,
        string anchorId,
        long sequence,
        int expiry,
        StoneColor owner) =>
        new(id, 1, owner, anchorId, "test", sequence, expiry);

    private static StoneRuntimeState Runtime(BoardState board)
    {
        var instances = board.OccupiedStones
            .Select((stone, index) => new StoneRuntimeInstance(
                $"stone-{stone.Point.X}-{stone.Point.Y}",
                stone,
                "basic",
                index + 1,
                []))
            .ToArray();
        return StoneRuntimeState.Create(board, instances, instances.Length + 1L);
    }

    private static BoardState Board(params BoardStone[] stones) =>
        BoardState.Create(Geometry, stones);

    private static BoardStone Stone(StoneColor color, int x, int y) =>
        new(color, false, C(x, y));

    private static CanonicalPoint C(int x, int y) =>
        Geometry.CreateCanonicalPoint(x, y);
}
