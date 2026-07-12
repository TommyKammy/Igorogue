using Igorogue.Domain.Board;
using Igorogue.Domain.Combat;

namespace Igorogue.Domain.Tests;

public sealed class ContinuousLibertySnapshotTests
{
    [Fact]
    public void RebindPreservesExactSurvivorModifiersAndDropsRemovedAnchors()
    {
        var geometry = BoardGeometry.Create(7);
        var survivingStone = Stone(geometry, StoneColor.Black, 1, 1);
        var removedStone = Stone(geometry, StoneColor.White, 2, 2);
        var sourceBoard = BoardState.Create(geometry, [survivingStone, removedStone]);
        var survivingInstance = new StoneRuntimeInstance(
            "stone.surviving",
            survivingStone,
            "standard",
            1,
            []);
        var removedInstance = new StoneRuntimeInstance(
            "stone.removed",
            removedStone,
            "standard",
            2,
            []);
        var sourceStones = StoneRuntimeState.Create(
            sourceBoard,
            [survivingInstance, removedInstance],
            3);
        var survivingModifier = Modifier(
            "modifier.surviving",
            survivingInstance);
        var removedModifier = Modifier("modifier.removed", removedInstance);
        var source = ContinuousLibertySnapshot.Create(
            sourceStones,
            [removedModifier, survivingModifier]);

        var resultBoard = BoardState.Create(geometry, [survivingStone]);
        var resultStones = StoneRuntimeState.Create(
            resultBoard,
            [survivingInstance],
            3);
        var rebound = source.Rebind(resultStones);

        Assert.Same(resultStones, rebound.SourceStones);
        Assert.Same(survivingModifier, Assert.Single(rebound.Modifiers));
        Assert.DoesNotContain(rebound.Modifiers, modifier =>
            modifier.ModifierInstanceId == removedModifier.ModifierInstanceId);
        Assert.Same(source, source.Rebind(sourceStones));
    }

    [Fact]
    public void RebindAllowsNewStoneAtPreviouslyEmptyPointWithoutChangingSurvivors()
    {
        var geometry = BoardGeometry.Create(7);
        var sourceStone = Stone(geometry, StoneColor.Black, 1, 1);
        var sourceBoard = BoardState.Create(geometry, [sourceStone]);
        var sourceInstance = new StoneRuntimeInstance(
            "stone.source",
            sourceStone,
            "standard",
            1,
            []);
        var sourceStones = StoneRuntimeState.Create(sourceBoard, [sourceInstance], 2);
        var modifier = Modifier("modifier.source", sourceInstance);
        var source = ContinuousLibertySnapshot.Create(sourceStones, [modifier]);

        var placedStone = Stone(geometry, StoneColor.White, 3, 3);
        var resultBoard = BoardState.Create(geometry, [sourceStone, placedStone]);
        var placedInstance = new StoneRuntimeInstance(
            "stone.placed",
            placedStone,
            "standard",
            2,
            []);
        var resultStones = StoneRuntimeState.Create(
            resultBoard,
            [sourceInstance, placedInstance],
            3);

        var rebound = source.Rebind(resultStones);

        Assert.Same(resultStones, rebound.SourceStones);
        Assert.Same(modifier, Assert.Single(rebound.Modifiers));
    }

    [Fact]
    public void RebindRejectsForeignGeometryAndRecreatedOrReplacedSourceIdentity()
    {
        var geometry = BoardGeometry.Create(7);
        var sourceStone = Stone(geometry, StoneColor.Black, 1, 1);
        var sourceBoard = BoardState.Create(geometry, [sourceStone]);
        var sourceInstance = new StoneRuntimeInstance(
            "stone.source",
            sourceStone,
            "standard",
            1,
            []);
        var sourceStones = StoneRuntimeState.Create(sourceBoard, [sourceInstance], 2);
        var source = ContinuousLibertySnapshot.Create(
            sourceStones,
            [Modifier("modifier.source", sourceInstance)]);

        var foreignGeometry = BoardGeometry.Create(7);
        var foreignStone = Stone(foreignGeometry, StoneColor.Black, 1, 1);
        var foreignBoard = BoardState.Create(foreignGeometry, [foreignStone]);
        var foreignStones = StoneRuntimeState.Create(
            foreignBoard,
            [new StoneRuntimeInstance("stone.source", foreignStone, "standard", 1, [])],
            2);
        Assert.Throws<ArgumentException>(() => source.Rebind(foreignStones));

        var recreatedBoard = BoardState.Create(geometry, [sourceStone]);
        var recreatedStones = StoneRuntimeState.Create(
            recreatedBoard,
            [new StoneRuntimeInstance("stone.source", sourceStone, "standard", 1, [])],
            2);
        Assert.Throws<ArgumentException>(() => source.Rebind(recreatedStones));

        var replacementStone = Stone(geometry, StoneColor.Black, 1, 1);
        var replacementBoard = BoardState.Create(geometry, [replacementStone]);
        var replacementStones = StoneRuntimeState.Create(
            replacementBoard,
            [new StoneRuntimeInstance("stone.replacement", replacementStone, "standard", 2, [])],
            3);
        Assert.Throws<ArgumentException>(() => source.Rebind(replacementStones));

        var outOfSequenceStone = Stone(geometry, StoneColor.White, 3, 3);
        var outOfSequenceBoard = BoardState.Create(
            geometry,
            [sourceStone, outOfSequenceStone]);
        var outOfSequenceStones = StoneRuntimeState.Create(
            outOfSequenceBoard,
            [
                sourceInstance,
                new StoneRuntimeInstance(
                    "stone.out_of_sequence",
                    outOfSequenceStone,
                    "standard",
                    3,
                    []),
            ],
            4);
        Assert.Throws<ArgumentException>(() => source.Rebind(outOfSequenceStones));

        var firstExtraStone = Stone(geometry, StoneColor.White, 3, 3);
        var secondExtraStone = Stone(geometry, StoneColor.White, 4, 4);
        var multipleExtraBoard = BoardState.Create(
            geometry,
            [sourceStone, firstExtraStone, secondExtraStone]);
        var multipleExtraStones = StoneRuntimeState.Create(
            multipleExtraBoard,
            [
                sourceInstance,
                new StoneRuntimeInstance(
                    "stone.extra_one",
                    firstExtraStone,
                    "standard",
                    2,
                    []),
                new StoneRuntimeInstance(
                    "stone.extra_two",
                    secondExtraStone,
                    "standard",
                    3,
                    []),
            ],
            4);
        Assert.Throws<ArgumentException>(() => source.Rebind(multipleExtraStones));
    }

    private static ContinuousLibertyModifier Modifier(
        string modifierInstanceId,
        StoneRuntimeInstance anchor) =>
        new(
            modifierInstanceId,
            1,
            anchor.Color,
            anchor.InstanceId,
            "test.source");

    private static BoardStone Stone(
        BoardGeometry geometry,
        StoneColor color,
        int x,
        int y) =>
        new(color, false, geometry.CreateCanonicalPoint(x, y));
}
