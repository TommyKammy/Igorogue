using Igorogue.Domain.Board;
using Igorogue.Domain.Combat;
using Igorogue.Domain.Content;
using Igorogue.Domain.Enemies;
using Igorogue.Domain.Facilities;

namespace Igorogue.Domain.Tests;

public sealed class BanditIntentKernelTests
{
    private const string StateChecksum =
        "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    private static readonly BoardGeometry Geometry =
        BoardGeometry.Create(BoardGeometry.AcceptedSize);

    private static readonly FacilityRuntimePolicy FacilityPolicy =
        FacilityRuntimePolicy.Create(
            3,
            [new FacilityCapacityBand(1, 49, 1)],
            5,
            [new KeyValuePair<string, int>("default", 2)]);

    [Fact]
    public void SharedRuntimeEvaluatorUsesTimedLibertyForCaptureAndPreservesSource()
    {
        var board = Board(
            Stone(StoneColor.Black, 4, 4, isKing: true),
            Stone(StoneColor.White, 3, 4),
            Stone(StoneColor.White, 5, 4),
            Stone(StoneColor.White, 4, 3),
            Stone(StoneColor.White, 7, 7, isKing: true));
        var stones = Runtime(board);
        var guardedKing = stones.InstanceAt(C(4, 4))!;
        var effect = new TemporaryLibertyEffect(
            "effect.guard",
            1,
            StoneColor.Black,
            guardedKing.InstanceId,
            "test.guard",
            1,
            1);
        var temporary = TemporaryLibertyState.Create(stones, [effect], 2);

        var result = RuntimeStonePlacementEvaluator.Evaluate(
            stones,
            temporary,
            ContinuousLibertySnapshot.Empty(stones),
            BattleRepetitionHistory.Start(board),
            Stone(StoneColor.White, 4, 5),
            PlacementAccessMode.Normal,
            Descriptor());

        Assert.True(result.Accepted);
        Assert.Empty(result.LegalPlacementCommit!.Candidate.CapturedGroups);
        Assert.NotNull(result.RuntimePlacementCommit!.BoardAfterCommit.StoneAt(C(4, 4)));
        Assert.Same(
            effect,
            result.RuntimePlacementCommit.TemporaryLibertiesAfterCommit.EffectById(
                effect.EffectInstanceId));
        Assert.Same(board, stones.SourceBoard);
        Assert.Same(temporary, result.RuntimePlacementCommit.SourceTemporaryLiberties);
    }

    [Fact]
    public void F0901InitialBanditPlanUsesAcceptedLexicographicPointOrder()
    {
        var context = Context(StandardBoard());

        var plan = BanditIntentPlanner.Plan(
            context,
            BanditDefinition(),
            StateChecksum,
            Descriptor());

        Assert.Equal(EnemyIntentKind.AdvanceTowardBlackKing, plan.IntentKind);
        Assert.Equal(C(6, 4), plan.PrimaryPoint);
        Assert.Equal([C(4, 6), C(5, 5)], plan.AlternatePoints);
        Assert.Equal(context.BlackKingGroup.Anchor, plan.TargetReference!.Anchor);
        Assert.True(plan.Retargetable);
        Assert.Equal(64, plan.Checksum.Length);
    }

    [Fact]
    public void Decision0010Option1UsesKingStonesWhenTimedLibertyProtectsZeroRealLibertyKing()
    {
        var context = TimedProtectedZeroRealLibertyKingContext(
            ZeroRealLibertyKingBoard(reverseInput: false));
        Assert.Empty(context.BlackKingGroup.RealLiberties);
        Assert.Equal(1, context.EffectiveLibertiesFor(context.BlackKingGroup));

        var ranked = BanditCandidateGenerator.Generate(
            context,
            BanditDefinition(),
            EnemyIntentKind.AdvanceTowardBlackKing,
            Descriptor());

        Assert.NotEmpty(ranked);
        Assert.Equal(C(2, 2), ranked[0].Point);
        Assert.Equal(2, ranked[0].DistanceToBlackKingAdvanceTarget);
        Assert.All(
            ranked,
            candidate => Assert.Equal(
                MinimumPointToGroupDistance(candidate.Point, context.BlackKingGroup),
                candidate.DistanceToBlackKingAdvanceTarget));

        var plan = BanditIntentPlanner.Plan(
            context,
            BanditDefinition(),
            StateChecksum,
            Descriptor());
        var reverseInputPlan = BanditIntentPlanner.Plan(
            TimedProtectedZeroRealLibertyKingContext(
                ZeroRealLibertyKingBoard(reverseInput: true)),
            BanditDefinition(reverseCanonicalInputs: true),
            StateChecksum,
            Descriptor());

        Assert.Equal(EnemyIntentKind.AdvanceTowardBlackKing, plan.IntentKind);
        Assert.Equal(C(2, 2), plan.PrimaryPoint);
        Assert.Equal(context.BlackKingGroup.Anchor, plan.TargetReference!.Anchor);
        Assert.Equal(plan.ToCanonicalText(), reverseInputPlan.ToCanonicalText());
        Assert.Equal(plan.Checksum, reverseInputPlan.Checksum);
    }

    [Fact]
    public void RepetitionCandidateIsRemovedBeforeAdvanceRanking()
    {
        var board = StandardBoard();
        var firstContext = Context(board);
        var first = Assert.Single(
            BanditCandidateGenerator.Generate(
                firstContext,
                BanditDefinition(),
                EnemyIntentKind.AdvanceTowardBlackKing,
                Descriptor()),
            candidate => candidate.Point.Equals(C(6, 4)));
        var repeatedBoard = first.PlacementEvaluation.LegalPlacementCommit!.BoardAfterCommit;
        var history = BattleRepetitionHistory.FromObservedBoards(
            [board, repeatedBoard, board]);

        var ranked = BanditCandidateGenerator.Generate(
            Context(board, history),
            BanditDefinition(),
            EnemyIntentKind.AdvanceTowardBlackKing,
            Descriptor());

        Assert.DoesNotContain(ranked, candidate => candidate.Point.Equals(C(6, 4)));
        Assert.Equal(C(4, 6), ranked[0].Point);
    }

    [Fact]
    public void MultiGroupCaptureTargetsLargestGroupAndUsesItsKingDistance()
    {
        var board = MultiGroupCaptureBoard(includePrimaryGroup: true);
        var context = Context(board);

        var candidate = Assert.Single(
            BanditCandidateGenerator.Generate(
                context,
                BanditDefinition(),
                EnemyIntentKind.CaptureNonKing,
                Descriptor()),
            candidate => candidate.Point.Equals(C(4, 4)));

        Assert.Equal(3, candidate.CapturedStoneCount);
        Assert.Equal(2, candidate.TargetGroup.Stones.Count);
        Assert.Equal(C(2, 4), candidate.TargetReference.Anchor);
        Assert.Equal(4, candidate.TargetGroupToBlackKingDistance);
    }

    [Fact]
    public void MultiGroupPrimaryDisappearanceRetargetsToTheRemainingCapture()
    {
        var enemy = BanditDefinition();
        var planned = BanditIntentPlanner.Plan(
            Context(MultiGroupCaptureBoard(includePrimaryGroup: true)),
            enemy,
            StateChecksum,
            Descriptor());
        Assert.Equal(EnemyIntentKind.CaptureNonKing, planned.IntentKind);
        Assert.Equal(C(2, 4), planned.TargetReference!.Anchor);
        Assert.Equal(C(4, 4), planned.PrimaryPoint);

        var decision = BanditIntentExecutor.Decide(
            Context(MultiGroupCaptureBoard(includePrimaryGroup: false)),
            enemy,
            planned,
            Descriptor());

        Assert.Equal(BanditExecutionReason.SameIntentRetarget, decision.Reason);
        Assert.Equal(C(5, 4), decision.TargetAfter!.Anchor);
        Assert.Equal(C(4, 4), decision.Candidate!.Point);
        Assert.True(decision.Retargeted);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(4, 1)]
    public void EqualCaptureGroupsUseKingDistanceThenCanonicalAnchor(
        int blackKingX,
        int blackKingY)
    {
        var context = Context(EqualCaptureBoard(blackKingX, blackKingY));

        var candidate = Assert.Single(
            BanditCandidateGenerator.Generate(
                context,
                BanditDefinition(),
                EnemyIntentKind.CaptureNonKing,
                Descriptor()),
            candidate => candidate.Point.Equals(C(4, 4)));

        Assert.Equal(C(3, 4), candidate.TargetReference.Anchor);
        Assert.Equal(
            blackKingX == 1 ? 5 : 4,
            candidate.TargetGroupToBlackKingDistance);
        var otherGroup = candidate.CapturedGroups.Single(group =>
            group.Anchor.Equals(C(5, 4)));
        var otherDistance = MinimumStoneDistance(otherGroup, context.BlackKingGroup);
        Assert.True(
            blackKingX == 1
                ? candidate.TargetGroupToBlackKingDistance < otherDistance
                : candidate.TargetGroupToBlackKingDistance == otherDistance);
    }

    [Fact]
    public void F0902ReachableCandidatesRankTwoStoneCaptureBeforeOneStoneCapture()
    {
        var board = Board(
            Stone(StoneColor.Black, 1, 1, isKing: true),
            Stone(StoneColor.White, 7, 7, isKing: true),
            Stone(StoneColor.Black, 3, 3),
            Stone(StoneColor.Black, 3, 4),
            Stone(StoneColor.White, 2, 3),
            Stone(StoneColor.White, 3, 2),
            Stone(StoneColor.White, 2, 4),
            Stone(StoneColor.White, 4, 4),
            Stone(StoneColor.White, 3, 5),
            Stone(StoneColor.Black, 5, 5),
            Stone(StoneColor.White, 4, 5),
            Stone(StoneColor.White, 6, 5),
            Stone(StoneColor.White, 5, 6),
            Stone(StoneColor.White, 6, 7),
            Stone(StoneColor.White, 7, 6));

        var ranked = BanditCandidateGenerator.Generate(
            Context(board),
            BanditDefinition(),
            EnemyIntentKind.CaptureNonKing,
            Descriptor());

        Assert.True(ranked.Count >= 2);
        Assert.Equal(C(4, 3), ranked[0].Point);
        Assert.Equal(2, ranked[0].CapturedStoneCount);
        var oneStone = Assert.Single(
            ranked,
            candidate => candidate.Point.Equals(C(5, 4)));
        Assert.Equal(1, oneStone.CapturedStoneCount);
    }

    [Fact]
    public void ExecutionMandatoryLethalOverridesTheDisplayedPlan()
    {
        var context = Context(Board(
            Stone(StoneColor.Black, 4, 4, isKing: true),
            Stone(StoneColor.White, 3, 4),
            Stone(StoneColor.White, 5, 4),
            Stone(StoneColor.White, 4, 3),
            Stone(StoneColor.White, 7, 7, isKing: true)));
        var displayed = PlannedEnemyIntent.Create(
            EnemyIntentKind.AdvanceTowardBlackKing,
            new EnemyTargetReference(StoneColor.Black, C(4, 4)),
            C(6, 6),
            [],
            true,
            StateChecksum);

        var decision = BanditIntentExecutor.Decide(
            context,
            BanditDefinition(),
            displayed,
            Descriptor());

        Assert.Equal(BanditExecutionReason.MandatoryLethalOverride, decision.Reason);
        Assert.Equal(EnemyIntentKind.CaptureBlackKing, decision.ExecutedIntentKind);
        Assert.Equal(C(4, 5), decision.Candidate!.Point);
    }

    [Fact]
    public void ExecutionMandatoryDefenseOverridesTheDisplayedPlan()
    {
        var context = Context(Board(
            Stone(StoneColor.Black, 1, 1, isKing: true),
            Stone(StoneColor.White, 4, 4, isKing: true),
            Stone(StoneColor.Black, 3, 4),
            Stone(StoneColor.Black, 5, 4)));
        Assert.Equal(2, context.EffectiveLibertiesFor(context.WhiteKingGroup));
        var displayed = PlannedEnemyIntent.Create(
            EnemyIntentKind.AdvanceTowardBlackKing,
            new EnemyTargetReference(StoneColor.Black, C(1, 1)),
            C(4, 3),
            [],
            true,
            StateChecksum);

        var decision = BanditIntentExecutor.Decide(
            context,
            BanditDefinition(),
            displayed,
            Descriptor());

        Assert.Equal(BanditExecutionReason.MandatoryDefenseOverride, decision.Reason);
        Assert.Equal(EnemyIntentKind.DefendWhiteKing, decision.ExecutedIntentKind);
        Assert.True(
            decision.Candidate!.WhiteKingEffectiveLibertiesAfter >
            context.EffectiveLibertiesFor(context.WhiteKingGroup));
    }

    [Fact]
    public void MissingCaptureTargetRetargetsWithinTheSameIntent()
    {
        var board = Board(
            Stone(StoneColor.Black, 1, 1, isKing: true),
            Stone(StoneColor.White, 7, 7, isKing: true),
            Stone(StoneColor.Black, 5, 5),
            Stone(StoneColor.White, 4, 5),
            Stone(StoneColor.White, 6, 5),
            Stone(StoneColor.White, 5, 6),
            Stone(StoneColor.White, 6, 7),
            Stone(StoneColor.White, 7, 6));
        var context = Context(board);
        Assert.True(context.EffectiveLibertiesFor(context.WhiteKingGroup) > 2);
        var displayed = PlannedEnemyIntent.Create(
            EnemyIntentKind.CaptureNonKing,
            new EnemyTargetReference(StoneColor.Black, C(3, 3)),
            C(3, 4),
            [],
            true,
            StateChecksum);

        var decision = BanditIntentExecutor.Decide(
            context,
            BanditDefinition(),
            displayed,
            Descriptor());

        Assert.Equal(BanditExecutionReason.SameIntentRetarget, decision.Reason);
        Assert.Equal(0, decision.FallbackDepth);
        Assert.Equal(C(5, 5), decision.TargetAfter!.Anchor);
        Assert.Equal(C(5, 4), decision.Candidate!.Point);
        Assert.True(decision.Retargeted);
    }

    [Fact]
    public void ExhaustedPressureFallsBackToAdvanceBeforePass()
    {
        var context = Context(StandardBoard());
        var displayed = PlannedEnemyIntent.Create(
            EnemyIntentKind.PressureBlackKing,
            new EnemyTargetReference(StoneColor.Black, C(2, 2)),
            C(2, 4),
            [],
            true,
            StateChecksum);

        var decision = BanditIntentExecutor.Decide(
            context,
            BanditDefinition(),
            displayed,
            Descriptor());

        Assert.Equal(BanditExecutionReason.Fallback, decision.Reason);
        Assert.Equal(1, decision.FallbackDepth);
        Assert.Equal(EnemyIntentKind.AdvanceTowardBlackKing, decision.ExecutedIntentKind);
        Assert.Equal(C(6, 4), decision.Candidate!.Point);
        Assert.True(decision.Retargeted);
    }

    [Fact]
    public void PlannedAnchorStoneSurvivesGroupMergeWithoutRetargeting()
    {
        var mergedBoard = Board(
            Stone(StoneColor.Black, 2, 2),
            Stone(StoneColor.Black, 3, 2),
            Stone(StoneColor.Black, 4, 2, isKing: true),
            Stone(StoneColor.Black, 4, 3),
            Stone(StoneColor.White, 6, 6, isKing: true),
            Stone(StoneColor.White, 5, 6),
            Stone(StoneColor.White, 6, 5));
        var context = Context(mergedBoard);
        var originalAnchor = new EnemyTargetReference(StoneColor.Black, C(4, 2));
        var planned = PlannedEnemyIntent.Create(
            EnemyIntentKind.AdvanceTowardBlackKing,
            originalAnchor,
            C(6, 4),
            [],
            retargetable: true,
            StateChecksum);

        var decision = BanditIntentExecutor.Decide(
            context,
            BanditDefinition(),
            planned,
            Descriptor());

        Assert.Equal(BanditExecutionReason.PlannedTarget, decision.Reason);
        Assert.False(decision.Retargeted);
        Assert.Equal(originalAnchor, decision.TargetAfter);
        Assert.Equal(C(2, 2), decision.Candidate!.TargetGroup.Anchor);
        Assert.Contains(
            decision.Candidate.TargetGroup.StonePoints,
            point => point.Equals(originalAnchor.Anchor));
    }

    [Fact]
    public void LegalCandidateProjectsFacilityDestructionWithoutMutatingSource()
    {
        var board = StandardBoard();
        var facility = new FacilityInstance(
            "facility.preview",
            "default",
            StoneColor.Black,
            C(6, 4),
            1);
        var facilityState = FacilityState.Create(board, [facility], 2);
        var context = Context(board, facilities: facilityState);

        var candidate = BanditCandidateGenerator.Generate(
                context,
                BanditDefinition(),
                EnemyIntentKind.AdvanceTowardBlackKing,
                Descriptor())
            .Single(candidate => candidate.Point.Equals(C(6, 4)));

        Assert.Same(facility, context.FacilityState.FacilityById(facility.InstanceId));
        Assert.Same(facility, candidate.FacilityPlacementCommit.DestructionFact!.Facility);
        Assert.Null(
            candidate.FacilityPlacementCommit.FacilityStateAfterCommit.FacilityById(
                facility.InstanceId));
        Assert.Same(
            candidate.TerritoryAfter,
            candidate.FacilityTransitionAfter.AnalysisAfter.TerritoryAnalysis);
    }

    [Fact]
    public void ExhaustedBoardPlansCanonicalPassWithoutRngOrMutation()
    {
        var stones = Geometry.CanonicalPoints
            .Select((point, index) => point switch
            {
                { X: 1, Y: 1 } => new BoardStone(StoneColor.Black, true, point),
                { X: 7, Y: 7 } => new BoardStone(StoneColor.White, true, point),
                _ => new BoardStone(
                    index % 2 == 0 ? StoneColor.Black : StoneColor.White,
                    false,
                    point),
            })
            .ToArray();
        var context = Context(Board(stones));

        var plan = BanditIntentPlanner.Plan(
            context,
            BanditDefinition(),
            StateChecksum,
            Descriptor());

        Assert.True(plan.IsPass);
        Assert.Equal("pass", plan.IntentId);
        Assert.Null(plan.TargetReference);
        Assert.Null(plan.PrimaryPoint);
        Assert.Empty(plan.AlternatePoints);
        Assert.False(plan.Retargetable);
        Assert.Same(context.Board, context.StoneRuntimeState.SourceBoard);
    }

    [Fact]
    public void EnemyPolicyProjectionIsCanonicalAcrossSetAndIntentEnumeration()
    {
        var forward = BanditDefinition(reverseCanonicalInputs: false);
        var reversed = BanditDefinition(reverseCanonicalInputs: true);

        BanditContentContract.Validate(forward);
        BanditContentContract.Validate(reversed);
        Assert.Equal(forward.ToCanonicalText(), reversed.ToCanonicalText());
        Assert.StartsWith(EnemyContentDefinition.EncodingVersion, forward.ToCanonicalText());

        var forwardPlan = BanditIntentPlanner.Plan(
            Context(StandardBoard()),
            forward,
            StateChecksum,
            Descriptor());
        var reversedPlan = BanditIntentPlanner.Plan(
            Context(StandardBoard()),
            reversed,
            StateChecksum,
            Descriptor());
        Assert.Equal(forwardPlan.ToCanonicalText(), reversedPlan.ToCanonicalText());
        Assert.Equal(forwardPlan.Checksum, reversedPlan.Checksum);
    }

    [Theory]
    [InlineData(BanditContractMutation.ActionBudget, "action_budget")]
    [InlineData(BanditContractMutation.MandatoryOverrides, "mandatory_overrides")]
    [InlineData(BanditContractMutation.PlanPriority, "plan_priority")]
    [InlineData(BanditContractMutation.CounterattackPriority, "counterattack_priority")]
    [InlineData(BanditContractMutation.ScoreProfile, "score profile")]
    public void CandidateGenerationRejectsMismatchedBanditContentBeforeBoardEvaluation(
        BanditContractMutation mutation,
        string expectedDetail)
    {
        var context = Context(StandardBoard());

        var exception = Assert.Throws<ArgumentException>(() =>
            BanditCandidateGenerator.Generate(
                context,
                BanditDefinition(contractMutation: mutation),
                EnemyIntentKind.AdvanceTowardBlackKing,
                Descriptor()));

        Assert.Equal("enemy", exception.ParamName);
        Assert.Contains(expectedDetail, exception.Message);
        Assert.Same(context.Board, context.StoneRuntimeState.SourceBoard);
        Assert.Equal(6, context.Board.OccupiedStones.Count);
    }

    [Fact]
    public void UndefinedBanditTieBreakIsRejectedAtDefinitionBoundary()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            BanditDefinition(tieBreak: (EnemyTieBreak)byte.MaxValue));
    }

    private static BanditPlanningContext Context(
        BoardState board,
        BattleRepetitionHistory? history = null,
        FacilityState? facilities = null)
    {
        var stones = Runtime(board);
        var temporary = TemporaryLibertyState.Create(stones, [], 1);
        var continuous = ContinuousLibertySnapshot.Empty(stones);
        var facilityState = facilities ?? FacilityState.Create(board, [], 1);
        var territory = TerritoryAnalyzer.Analyze(board);
        var facilityRuntime = FacilityRuntimeAnalyzer.Analyze(
            facilityState,
            territory,
            FacilityPolicy);
        return new BanditPlanningContext(
            stones,
            temporary,
            continuous,
            history ?? BattleRepetitionHistory.Start(board),
            facilityRuntime);
    }

    private static BanditPlanningContext TimedProtectedZeroRealLibertyKingContext(
        BoardState board)
    {
        var stones = Runtime(board);
        var blackKing = stones.Instances.Single(instance =>
            instance.Color == StoneColor.Black && instance.IsKing);
        var timedProtection = new TemporaryLibertyEffect(
            "effect.black-king-guard",
            1,
            StoneColor.Black,
            blackKing.InstanceId,
            "test.black-king-guard",
            1,
            1);
        var temporary = TemporaryLibertyState.Create(stones, [timedProtection], 2);
        var continuous = ContinuousLibertySnapshot.Empty(stones);
        var facilityState = FacilityState.Create(board, [], 1);
        var territory = TerritoryAnalyzer.Analyze(board);
        var facilityRuntime = FacilityRuntimeAnalyzer.Analyze(
            facilityState,
            territory,
            FacilityPolicy);
        return new BanditPlanningContext(
            stones,
            temporary,
            continuous,
            BattleRepetitionHistory.Start(board),
            facilityRuntime);
    }

    private static EnemyContentDefinition BanditDefinition(
        bool reverseCanonicalInputs = false,
        BanditContractMutation contractMutation = BanditContractMutation.None,
        EnemyTieBreak tieBreak = EnemyTieBreak.CanonicalYThenX)
    {
        var captureNonKingProfile = contractMutation == BanditContractMutation.ScoreProfile
            ? EnemyScoreProfile.KingAdvance
            : EnemyScoreProfile.CaptureValue;
        var advanceProfile = contractMutation == BanditContractMutation.ScoreProfile
            ? EnemyScoreProfile.CaptureValue
            : EnemyScoreProfile.KingAdvance;
        var intents = new[]
        {
            EnemyIntentDefinition.Create(
                EnemyIntentKind.CaptureBlackKing,
                EnemyIntentKind.CaptureBlackKing,
                [EnemyPlacementMode.WhiteTerminal, EnemyPlacementMode.WhiteFrontline, EnemyPlacementMode.WhiteContact],
                EnemyScoreProfile.KingExecution,
                []),
            EnemyIntentDefinition.Create(
                EnemyIntentKind.DefendWhiteKing,
                EnemyIntentKind.DefendWhiteKing,
                [EnemyPlacementMode.WhiteFrontline, EnemyPlacementMode.WhiteContact, EnemyPlacementMode.WhiteTerminal],
                EnemyScoreProfile.KingDefense,
                [EnemyIntentKind.CaptureNonKing, EnemyIntentKind.PressureBlackKing, EnemyIntentKind.AdvanceTowardBlackKing]),
            EnemyIntentDefinition.Create(
                EnemyIntentKind.CaptureNonKing,
                EnemyIntentKind.CaptureNonKing,
                [EnemyPlacementMode.WhiteTerminal, EnemyPlacementMode.WhiteFrontline, EnemyPlacementMode.WhiteContact],
                captureNonKingProfile,
                [EnemyIntentKind.PressureBlackKing, EnemyIntentKind.AdvanceTowardBlackKing]),
            EnemyIntentDefinition.Create(
                EnemyIntentKind.PressureBlackKing,
                EnemyIntentKind.PressureBlackKing,
                [EnemyPlacementMode.WhiteFrontline, EnemyPlacementMode.WhiteContact],
                EnemyScoreProfile.KingPressure,
                [EnemyIntentKind.AdvanceTowardBlackKing]),
            EnemyIntentDefinition.Create(
                EnemyIntentKind.AdvanceTowardBlackKing,
                EnemyIntentKind.AdvanceTowardBlackKing,
                [EnemyPlacementMode.WhiteFrontline],
                advanceProfile,
                []),
        };
        var permissions = new[]
        {
            EnemyPlacementMode.WhiteFrontline,
            EnemyPlacementMode.WhiteContact,
            EnemyPlacementMode.WhiteTerminal,
        };
        var mandatoryOverrides = new[]
        {
            EnemyIntentKind.CaptureBlackKing,
            EnemyIntentKind.DefendWhiteKing,
        };
        var planPriority = new[]
        {
            EnemyIntentKind.CaptureNonKing,
            EnemyIntentKind.PressureBlackKing,
            EnemyIntentKind.AdvanceTowardBlackKing,
        };
        var counterattackPriority = planPriority.ToArray();
        var actionBudget = contractMutation == BanditContractMutation.ActionBudget
            ? new EnemyActionBudget(2, 0, 2)
            : new EnemyActionBudget(1, 1, 2);

        if (contractMutation == BanditContractMutation.MandatoryOverrides)
        {
            Array.Reverse(mandatoryOverrides);
        }

        if (contractMutation == BanditContractMutation.PlanPriority)
        {
            Array.Reverse(planPriority);
        }

        if (contractMutation == BanditContractMutation.CounterattackPriority)
        {
            Array.Reverse(counterattackPriority);
        }

        return EnemyContentDefinition.Create(
            "enemy_test",
            "FEAT-009",
            "1.0.0",
            actionBudget,
            reverseCanonicalInputs ? permissions.Reverse() : permissions,
            new BanditParameters(2, 1),
            mandatoryOverrides,
            planPriority,
            counterattackPriority,
            reverseCanonicalInputs ? intents.Reverse() : intents,
            tieBreak);
    }

    public enum BanditContractMutation : byte
    {
        None = 0,
        ActionBudget = 1,
        MandatoryOverrides = 2,
        PlanPriority = 3,
        CounterattackPriority = 4,
        ScoreProfile = 5,
    }

    private static BoardState StandardBoard() => Board(
        Stone(StoneColor.Black, 2, 2, isKing: true),
        Stone(StoneColor.Black, 2, 3),
        Stone(StoneColor.Black, 3, 2),
        Stone(StoneColor.White, 6, 6, isKing: true),
        Stone(StoneColor.White, 5, 6),
        Stone(StoneColor.White, 6, 5));

    private static BoardState ZeroRealLibertyKingBoard(bool reverseInput)
    {
        var stones = new[]
        {
            Stone(StoneColor.Black, 1, 1, isKing: true),
            Stone(StoneColor.White, 2, 1),
            Stone(StoneColor.White, 1, 2),
            Stone(StoneColor.White, 7, 7, isKing: true),
            Stone(StoneColor.White, 6, 7),
            Stone(StoneColor.White, 7, 6),
        };
        return Board((reverseInput ? stones.Reverse() : stones).ToArray());
    }

    private static BoardState MultiGroupCaptureBoard(bool includePrimaryGroup)
    {
        var stones = new List<BoardStone>
        {
            Stone(StoneColor.Black, 1, 1, isKing: true),
            Stone(StoneColor.White, 7, 7, isKing: true),
            Stone(StoneColor.White, 6, 7),
            Stone(StoneColor.White, 7, 6),
            Stone(StoneColor.Black, 5, 4),
            Stone(StoneColor.White, 1, 4),
            Stone(StoneColor.White, 2, 3),
            Stone(StoneColor.White, 2, 5),
            Stone(StoneColor.White, 3, 3),
            Stone(StoneColor.White, 3, 5),
            Stone(StoneColor.White, 4, 3),
            Stone(StoneColor.White, 4, 5),
            Stone(StoneColor.White, 5, 3),
            Stone(StoneColor.White, 5, 5),
            Stone(StoneColor.White, 6, 4),
        };
        if (includePrimaryGroup)
        {
            stones.Add(Stone(StoneColor.Black, 2, 4));
            stones.Add(Stone(StoneColor.Black, 3, 4));
        }

        return Board(stones.ToArray());
    }

    private static BoardState EqualCaptureBoard(int blackKingX, int blackKingY) => Board(
        Stone(StoneColor.Black, blackKingX, blackKingY, isKing: true),
        Stone(StoneColor.White, 7, 7, isKing: true),
        Stone(StoneColor.Black, 3, 4),
        Stone(StoneColor.Black, 5, 4),
        Stone(StoneColor.White, 2, 4),
        Stone(StoneColor.White, 3, 3),
        Stone(StoneColor.White, 3, 5),
        Stone(StoneColor.White, 4, 3),
        Stone(StoneColor.White, 4, 5),
        Stone(StoneColor.White, 5, 3),
        Stone(StoneColor.White, 5, 5),
        Stone(StoneColor.White, 6, 4));

    private static int MinimumStoneDistance(StoneGroup left, StoneGroup right) =>
        left.Stones.Min(leftStone => right.Stones.Min(rightStone =>
            Math.Abs(leftStone.Point.X - rightStone.Point.X) +
            Math.Abs(leftStone.Point.Y - rightStone.Point.Y)));

    private static int MinimumPointToGroupDistance(
        CanonicalPoint point,
        StoneGroup group) =>
        group.Stones.Min(stone =>
            Math.Abs(point.X - stone.Point.X) +
            Math.Abs(point.Y - stone.Point.Y));

    private static StoneRuntimePlacementDescriptor Descriptor() =>
        new("stone.probe", "enemy_basic", []);

    private static StoneRuntimeState Runtime(BoardState board)
    {
        var instances = board.OccupiedStones
            .Select((stone, index) => new StoneRuntimeInstance(
                $"stone.{index + 1}",
                stone,
                "standard",
                index + 1L,
                []))
            .ToArray();
        return StoneRuntimeState.Create(board, instances, instances.Length + 1L);
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
