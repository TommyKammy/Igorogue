using Igorogue.Application.Battle;
using Igorogue.Application.Replay;
using Igorogue.Domain.Board;
using Igorogue.Domain.Combat;
using Igorogue.Domain.Determinism;
using Igorogue.Domain.Facilities;

namespace Igorogue.Application.Tests;

public sealed class AuthorizedRuntimeStonePlacementPipelineTests
{
    private const string ContentHash =
        "sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    private static readonly BoardGeometry Geometry =
        BoardGeometry.Create(BoardGeometry.AcceptedSize);

    [Fact]
    public void AcceptedPlacementReturnsTheExactLegalRuntimeAndLibertySnapshots()
    {
        var state = Start(Runtime(Board()));
        var runtime = Assert.IsType<BattleAuthoritativeRuntimeState>(
            state.AuthoritativeRuntime);

        var result = AuthorizedRuntimeStonePlacementPipeline.Resolve(
            state,
            runtime,
            StoneColor.Black,
            C(4, 4),
            PlacementAccessMode.Normal,
            Descriptor("stone.placed", "lure", ["captured.reserve_draw"]));

        Assert.True(result.Accepted);
        Assert.Equal("accepted", result.ReasonId);
        var legal = Assert.IsType<LegalPlacementCommit>(result.LegalPlacementCommit);
        var runtimeCommit = Assert.IsType<StoneRuntimePlacementCommit>(
            result.RuntimePlacementCommit);
        var capture = Assert.IsType<TemporaryLibertyEffectiveLibertyAnalysis>(
            result.CaptureEffectiveLiberties);
        var postCapture = Assert.IsType<TemporaryLibertyEffectiveLibertyAnalysis>(
            result.PostCaptureEffectiveLiberties);
        Assert.Same(legal.BoardAfterCommit, runtimeCommit.BoardAfterCommit);
        Assert.Same(legal.Candidate.GroupsAfterCapture, postCapture.GroupAnalysis);
        Assert.Same(runtimeCommit.StonesAfterCommit, postCapture.SourceStones);
        Assert.Same(
            runtimeCommit.TemporaryLibertiesAfterCommit,
            postCapture.TemporaryLiberties);
        Assert.Same(
            runtimeCommit.PlacedStoneInstance,
            capture.SourceStones.InstanceAt(C(4, 4)));
        Assert.Equal("stone.placed", runtimeCommit.PlacedStoneInstance.InstanceId);
        Assert.Equal("lure", runtimeCommit.PlacedStoneInstance.KindId);
        Assert.Equal(
            ["captured.reserve_draw"],
            runtimeCommit.PlacedStoneInstance.OrderedEffectMetadata);
        Assert.Equal(2, legal.HistoryAfterCommit.ObservationCount);
        Assert.Empty(runtime.StoneRuntimeState.Instances);
        Assert.True(state.Board.IsEmpty(C(4, 4)));
    }

    [Fact]
    public void UsedStoneInstanceRejectionPreservesEverySourceSnapshot()
    {
        var board = Board(Stone(StoneColor.Black, 1, 1));
        var state = Start(Runtime(board));
        var runtime = Assert.IsType<BattleAuthoritativeRuntimeState>(
            state.AuthoritativeRuntime);
        var stateText = state.CanonicalText;
        var runtimeText = runtime.CanonicalText;
        var sourceStones = runtime.StoneRuntimeState;

        var result = AuthorizedRuntimeStonePlacementPipeline.Resolve(
            state,
            runtime,
            StoneColor.White,
            C(7, 7),
            PlacementAccessMode.Normal,
            Descriptor("stone.1", "standard", []));

        Assert.False(result.Accepted);
        Assert.Equal("stone_instance_already_used", result.ReasonId);
        Assert.Null(result.LegalPlacementCommit);
        Assert.Null(result.RuntimePlacementCommit);
        Assert.Null(result.CaptureEffectiveLiberties);
        Assert.Null(result.PostCaptureEffectiveLiberties);
        Assert.Same(sourceStones, runtime.StoneRuntimeState);
        Assert.Same(board, state.Board);
        Assert.Equal(stateText, state.CanonicalText);
        Assert.Equal(runtimeText, runtime.CanonicalText);
        Assert.True(state.Board.IsEmpty(C(7, 7)));
    }

    [Fact]
    public void SourceWithoutAuthoritativeRuntimeAcceptsAnExplicitBoundSidecar()
    {
        var authoritativeState = Start(Runtime(Board()));
        var sidecar = Assert.IsType<BattleAuthoritativeRuntimeState>(
            authoritativeState.AuthoritativeRuntime);
        var source = BattleState.Start(
            authoritativeState.Board,
            authoritativeState.RepetitionHistory,
            authoritativeState.FacilityState,
            authoritativeState.RngState,
            authoritativeState.RuntimePolicy);

        Assert.Null(source.AuthoritativeRuntime);
        var result = AuthorizedRuntimeStonePlacementPipeline.Resolve(
            source,
            sidecar,
            StoneColor.Black,
            C(4, 4),
            PlacementAccessMode.Normal,
            Descriptor("stone.detached", "standard", []));

        Assert.True(result.Accepted);
        Assert.Equal("accepted", result.ReasonId);
        Assert.Same(
            result.LegalPlacementCommit!.BoardAfterCommit,
            result.RuntimePlacementCommit!.BoardAfterCommit);
    }

    [Fact]
    public void RuntimeSidecarBoundToAnotherBoardSnapshotIsRejected()
    {
        var state = Start(Runtime(Board()));
        var distinctBoard = Board();
        var distinctState = Start(Runtime(distinctBoard));
        var mismatchedRuntime = Assert.IsType<BattleAuthoritativeRuntimeState>(
            distinctState.AuthoritativeRuntime);

        var exception = Assert.Throws<ArgumentException>(() =>
            AuthorizedRuntimeStonePlacementPipeline.Resolve(
                state,
                mismatchedRuntime,
                StoneColor.Black,
                C(4, 4),
                PlacementAccessMode.Normal,
                Descriptor("stone.mismatched", "standard", [])));

        Assert.Equal("runtime", exception.ParamName);
        Assert.Same(distinctBoard, mismatchedRuntime.StoneRuntimeState.SourceBoard);
        Assert.NotSame(state.Board, mismatchedRuntime.StoneRuntimeState.SourceBoard);
    }

    [Fact]
    public void CapturePreparationUsesTimedEffectiveLibertyInsteadOfRealLiberty()
    {
        var board = Board(
            Stone(StoneColor.Black, 4, 4),
            Stone(StoneColor.White, 3, 4),
            Stone(StoneColor.White, 5, 4),
            Stone(StoneColor.White, 4, 3));
        var stones = Runtime(board);
        var anchor = stones.InstanceAt(C(4, 4))!;
        var temporary = TemporaryLibertyState.Create(
            stones,
            [new TemporaryLibertyEffect(
                "effect.guard",
                1,
                StoneColor.Black,
                anchor.InstanceId,
                "test.guard",
                1,
                1)],
            2);
        var state = Start(stones, temporary);
        var runtime = Assert.IsType<BattleAuthoritativeRuntimeState>(
            state.AuthoritativeRuntime);

        var result = AuthorizedRuntimeStonePlacementPipeline.Resolve(
            state,
            runtime,
            StoneColor.White,
            C(4, 5),
            PlacementAccessMode.Normal,
            Descriptor("stone.enemy", "standard", []));

        Assert.True(result.Accepted);
        var legal = Assert.IsType<LegalPlacementCommit>(result.LegalPlacementCommit);
        Assert.Empty(legal.Candidate.CapturedGroups);
        var postCapture = Assert.IsType<TemporaryLibertyEffectiveLibertyAnalysis>(
            result.PostCaptureEffectiveLiberties);
        var protectedGroup = Assert.IsType<StoneGroup>(
            postCapture.GroupAnalysis.GroupAt(C(4, 4)));
        Assert.Equal(0, protectedGroup.RealLibertyCount);
        Assert.Equal(
            1,
            postCapture.EffectiveLiberties.EffectiveLibertiesFor(protectedGroup));
        Assert.NotNull(legal.BoardAfterCommit.StoneAt(C(4, 4)));
    }

    [Fact]
    public void CanonicalPreparationIsIndependentOfRuntimeInputEnumeration()
    {
        var board = Board(
            Stone(StoneColor.Black, 2, 2),
            Stone(StoneColor.White, 6, 6));
        var firstState = Start(Runtime(board, reverse: false));
        var reversedState = Start(Runtime(board, reverse: true));

        var first = Resolve(firstState, "stone.placed");
        var reversed = Resolve(reversedState, "stone.placed");

        Assert.True(first.Accepted);
        Assert.True(reversed.Accepted);
        Assert.Equal(
            first.LegalPlacementCommit!.RegisteredTopologyKey.ToCanonicalText(),
            reversed.LegalPlacementCommit!.RegisteredTopologyKey.ToCanonicalText());
        Assert.Equal(
            first.LegalPlacementCommit.HistoryAfterCommit.ToCanonicalText(),
            reversed.LegalPlacementCommit.HistoryAfterCommit.ToCanonicalText());
        Assert.Equal(
            first.RuntimePlacementCommit!.StonesAfterCommit.ToCanonicalText(),
            reversed.RuntimePlacementCommit!.StonesAfterCommit.ToCanonicalText());
        Assert.Equal(
            first.CaptureEffectiveLiberties!.ToCanonicalText(),
            reversed.CaptureEffectiveLiberties!.ToCanonicalText());
        Assert.Equal(
            first.PostCaptureEffectiveLiberties!.ToCanonicalText(),
            reversed.PostCaptureEffectiveLiberties!.ToCanonicalText());
    }

    private static AuthorizedRuntimeStonePlacementResolution Resolve(
        BattleState state,
        string instanceId) =>
        AuthorizedRuntimeStonePlacementPipeline.Resolve(
            state,
            Assert.IsType<BattleAuthoritativeRuntimeState>(state.AuthoritativeRuntime),
            StoneColor.Black,
            C(3, 2),
            PlacementAccessMode.Normal,
            Descriptor(instanceId, "standard", []));

    private static BattleState Start(
        StoneRuntimeState stones,
        TemporaryLibertyState? temporary = null)
    {
        var counterPolicy = new CounterattackBoundaryPolicy(200, 12, 3, 30);
        var runtimePolicy = new BattleRuntimePolicy(
            20,
            FacilityRuntimePolicy.Create(
                5,
                [new FacilityCapacityBand(1, 49, 1)],
                3,
                [new KeyValuePair<string, int>("default", 1)]));
        var snapshot = BattleAuthoritativeInitialSnapshot.Create(
            stones,
            temporary ?? TemporaryLibertyState.Create(stones, [], 1),
            ContinuousLibertySnapshot.Empty(stones),
            BattleRepetitionHistory.Start(stones.SourceBoard),
            FacilityState.Create(stones.SourceBoard, [], 1),
            ClosedWindowResourceState.Empty([]),
            CaptureBenefitTriggerPlan.Create([]),
            CounterattackBoundaryState.Create(0, false, 0, counterPolicy),
            counterPolicy,
            runtimePolicy,
            1);
        return HeadlessBattleStateMachine.Start(
            snapshot,
            ReplayMetadata.Create("test-v1", ContentHash, 42)).State;
    }

    private static StoneRuntimePlacementDescriptor Descriptor(
        string instanceId,
        string kindId,
        IEnumerable<string> metadata) =>
        new(instanceId, kindId, metadata);

    private static StoneRuntimeState Runtime(
        BoardState board,
        bool reverse = false)
    {
        var instances = board.OccupiedStones
            .Select(stone =>
            {
                var canonicalIndex = board.Geometry.ToCanonicalIndex(stone.Point);
                return new StoneRuntimeInstance(
                    $"stone.{canonicalIndex + 1}",
                    stone,
                    stone.IsKing ? "king" : "standard",
                    canonicalIndex + 1L,
                    []);
            })
            .ToArray();
        return StoneRuntimeState.Create(
            board,
            reverse ? instances.Reverse() : instances,
            board.Geometry.PointCount + 1L);
    }

    private static BoardState Board(params BoardStone[] stones) =>
        BoardState.Create(Geometry, stones);

    private static BoardStone Stone(
        StoneColor color,
        int x,
        int y,
        bool isKing = false) =>
        new(color, isKing, C(x, y));

    private static CanonicalPoint C(int x, int y) =>
        Geometry.CreateCanonicalPoint(x, y);
}
