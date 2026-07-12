using Igorogue.Domain.Board;
using Igorogue.Domain.Combat;

namespace Igorogue.Domain.Tests;

public sealed class TemporaryLibertyExpiryFixtureTests
{
    private static readonly Lazy<IReadOnlyDictionary<string, TemporaryLibertyFixture>> Fixtures =
        new(TemporaryLibertyFixtureData.LoadFixtures);

    [Fact]
    public void CanonicalTemporaryLibertyFixtureInventoryIsPresent()
    {
        Assert.Equal(
            Enumerable.Range(1, 15).Select(index => $"TLE-{index:00}"),
            Fixtures.Value.Keys.Order(StringComparer.Ordinal));
        Assert.Equal(
            TemporaryLibertyFixtureData.Task0027FixtureIds,
            Fixtures.Value.Values
                .Where(fixture => fixture.Operation == "expiry_sweep")
                .Where(fixture => fixture.Id is not "TLE-09" and not "TLE-10" and not "TLE-15")
                .Select(fixture => fixture.Id)
                .Order(StringComparer.Ordinal));
    }

    [Theory]
    [MemberData(nameof(Task0027FixtureIds))]
    public void AcceptedFixtureDomainProjectionMatchesProductionResolver(string fixtureId)
    {
        var fixture = RequiredFixture(fixtureId);
        var execution = TemporaryLibertyFixtureData.Execute(fixture);
        var resolution = execution.Resolution;
        var expected = fixture.Expected;

        if (expected.ExpiredEffectIds is not null)
        {
            Assert.Equal(
                expected.ExpiredEffectIds,
                resolution.ExpiredEffects.Select(effect => effect.EffectInstanceId));
        }

        if (expected.RemainingEffectIds is not null)
        {
            Assert.Equal(
                expected.RemainingEffectIds,
                resolution.TemporaryLibertiesAfterResolution.Effects
                    .Select(effect => effect.EffectInstanceId));
        }

        if (expected.ContinuousModifierIds is not null)
        {
            Assert.Equal(
                expected.ContinuousModifierIds,
                resolution.ContinuousLiberties.Modifiers
                    .Select(modifier => modifier.ModifierInstanceId));
        }

        if (expected.CapturedGroups is not null)
        {
            Assert.Equal(
                expected.CapturedGroups.Select(group => new CapturedGroupProjection(
                    group.Color,
                    group.Anchor,
                    group.Count,
                    group.ContainsKing)),
                resolution.CapturedGroups.Select(ProjectGroup));
        }

        if (expected.BattleResult is not null)
        {
            Assert.Equal(expected.BattleResult, resolution.KingCaptureResult.OutcomeId);
        }

        if (expected.TopologyFirstSeen is not null)
        {
            Assert.Equal(expected.TopologyFirstSeen, resolution.TopologyFirstSeen);
        }

        if (expected.GroupCaptureEventCount is not null)
        {
            Assert.Equal(
                expected.GroupCaptureEventCount,
                resolution.OrderedFacts.OfType<TemporaryLibertyGroupCapturedFact>().Count());
        }

        if (expected.PreExpiryAnchorGroup is not null)
        {
            var preExpiry = TemporaryLibertyEffectiveLibertyAnalyzer.Analyze(
                execution.SourceStones,
                execution.SourceTemporaryLiberties,
                execution.ContinuousLiberties);
            var group = Assert.IsType<StoneGroup>(preExpiry.GroupAnalysis.GroupAt(
                expected.PreExpiryAnchorGroup.Anchor));
            var breakdown = preExpiry.BreakdownFor(group);

            Assert.Equal(expected.PreExpiryAnchorGroup.Anchor, group.Anchor);
            Assert.Equal(expected.PreExpiryAnchorGroup.Count, group.Stones.Count);
            Assert.Equal(expected.PreExpiryAnchorGroup.TemporaryBonus, breakdown.TimedAmount);
        }

        if (expected.BenefitsSuppressed is not null)
        {
            Assert.Equal(expected.BenefitsSuppressed, resolution.BenefitsSuppressed);
        }

        if (expected.CapturedKingColors is not null)
        {
            Assert.Equal(
                expected.CapturedKingColors,
                resolution.CapturedGroups
                    .Where(group => group.Stones.Any(stone => stone.IsKing))
                    .Select(group => ColorId(group.Color)));
        }

        if (expected.CaptureWasBlockedByRepetition is not null)
        {
            Assert.False(expected.CaptureWasBlockedByRepetition);
            Assert.NotEmpty(resolution.CapturedGroups);
            Assert.NotEqual(
                StoneTopologyKey.FromBoard(execution.SourceStones.SourceBoard),
                StoneTopologyKey.FromBoard(resolution.BoardAfterResolution));
        }

        if (expected.EventOrder is not null)
        {
            Assert.Equal(expected.EventOrder, resolution.OrderedFacts.Select(ProjectFact));
        }
    }

    [Theory]
    [MemberData(nameof(Task0027FixtureIds))]
    public void ResolutionOrderingMatchesAcceptedEffectAndGroupOrder(string fixtureId)
    {
        var fixture = RequiredFixture(fixtureId);
        var execution = TemporaryLibertyFixtureData.Execute(fixture);
        var resolution = execution.Resolution;
        var enemyTurnIndex = Assert.IsType<int>(fixture.EnemyTurnIndex);
        var expectedDue = fixture.Effects
            .Where(effect => effect.ExpiresAfterEnemyTurnIndex == enemyTurnIndex)
            .OrderBy(effect => effect.CreatedSequence)
            .ThenBy(effect => effect.EffectInstanceId, StringComparer.Ordinal)
            .Select(effect => effect.EffectInstanceId)
            .ToArray();

        Assert.Equal(
            expectedDue,
            resolution.ExpiredEffects.Select(effect => effect.EffectInstanceId));
        Assert.Equal(
            expectedDue,
            resolution.OrderedFacts
                .OfType<TemporaryLibertyExpiredFact>()
                .Select(fact => fact.Effect.EffectInstanceId));
        Assert.Equal(
            resolution.CapturedGroups.OrderBy(group => group.Anchor),
            resolution.CapturedGroups);
        Assert.Equal(
            resolution.CapturedGroups.Select(group => group.Anchor),
            resolution.OrderedFacts
                .OfType<TemporaryLibertyGroupCapturedFact>()
                .Select(fact => fact.CapturedGroup.Anchor));

        if (expectedDue.Length == 0)
        {
            Assert.Empty(resolution.OrderedFacts);
            return;
        }

        Assert.IsType<TemporaryLibertyExpirySweepStartedFact>(resolution.OrderedFacts[0]);
        Assert.IsType<TemporaryLibertyExpirySweepResolvedFact>(resolution.OrderedFacts[^1]);
        var facts = resolution.OrderedFacts.ToArray();
        var lastExpired = LastIndexOf<TemporaryLibertyExpiredFact>(facts);
        var firstCaptured = FirstIndexOf<TemporaryLibertyGroupCapturedFact>(facts);
        if (firstCaptured >= 0)
        {
            Assert.True(lastExpired < firstCaptured);
            var topology = Assert.Single(facts.OfType<StoneTopologyRegisteredFact>());
            Assert.Equal(TemporaryLibertyExpiryResolver.TopologySourceReasonId, topology.SourceReasonId);
            Assert.True(LastIndexOf<TemporaryLibertyGroupCapturedFact>(facts) <
                FirstIndexOf<StoneTopologyRegisteredFact>(facts));
        }
        else
        {
            Assert.Empty(facts.OfType<StoneTopologyRegisteredFact>());
        }
    }

    [Theory]
    [MemberData(nameof(Task0027FixtureIds))]
    public void SameAndReversedInputEnumerationHaveIdenticalCanonicalResolution(
        string fixtureId)
    {
        var fixture = RequiredFixture(fixtureId);
        var first = TemporaryLibertyFixtureData.Execute(fixture);
        var second = TemporaryLibertyFixtureData.Execute(fixture);
        var reversed = TemporaryLibertyFixtureData.Execute(fixture, reverseEnumeration: true);

        Assert.Equal(first.SourceStones.ToCanonicalText(), second.SourceStones.ToCanonicalText());
        Assert.Equal(first.SourceStones.ToCanonicalText(), reversed.SourceStones.ToCanonicalText());
        Assert.Equal(
            first.SourceTemporaryLiberties.ToCanonicalText(),
            reversed.SourceTemporaryLiberties.ToCanonicalText());
        Assert.Equal(
            first.ContinuousLiberties.ToCanonicalText(),
            reversed.ContinuousLiberties.ToCanonicalText());
        Assert.Equal(first.Resolution.CanonicalText, second.Resolution.CanonicalText);
        Assert.Equal(first.Resolution.CanonicalText, reversed.Resolution.CanonicalText);
        Assert.Equal(first.Resolution.Checksum, second.Resolution.Checksum);
        Assert.Equal(first.Resolution.Checksum, reversed.Resolution.Checksum);
    }

    [Fact]
    public void Tle02ExpiresStackedEffectsBeforeCapturingGroupExactlyOnce()
    {
        var execution = TemporaryLibertyFixtureData.Execute(RequiredFixture("TLE-02"));
        var preExpiry = TemporaryLibertyEffectiveLibertyAnalyzer.Analyze(
            execution.SourceStones,
            execution.SourceTemporaryLiberties,
            execution.ContinuousLiberties);
        var protectedGroup = Assert.Single(
            preExpiry.Breakdowns,
            breakdown => breakdown.Group.Color == StoneColor.Black);

        Assert.Equal(0, protectedGroup.RealLibertyCount);
        Assert.Equal(2, protectedGroup.TimedAmount);
        Assert.Equal(2, protectedGroup.EffectiveLibertyCount);
        Assert.Collection(
            execution.Resolution.OrderedFacts,
            fact => Assert.IsType<TemporaryLibertyExpirySweepStartedFact>(fact),
            fact => Assert.Equal(
                "effect_a",
                Assert.IsType<TemporaryLibertyExpiredFact>(fact).Effect.EffectInstanceId),
            fact => Assert.Equal(
                "effect_b",
                Assert.IsType<TemporaryLibertyExpiredFact>(fact).Effect.EffectInstanceId),
            fact => Assert.Equal(
                TemporaryLibertyFixtureData.Geometry.CreateCanonicalPoint(4, 4),
                Assert.IsType<TemporaryLibertyGroupCapturedFact>(fact).CapturedGroup.Anchor),
            fact => Assert.True(Assert.IsType<StoneTopologyRegisteredFact>(fact).FirstSeen),
            fact => Assert.IsType<TemporaryLibertyExpirySweepResolvedFact>(fact));
    }

    [Theory]
    [InlineData("TLE-07")]
    [InlineData("TLE-08")]
    public void TerminalKingCaptureSuppressesBenefitPipeline(string fixtureId)
    {
        var fixture = RequiredFixture(fixtureId);
        var resolution = TemporaryLibertyFixtureData.Execute(fixture).Resolution;
        var facts = resolution.OrderedFacts.ToArray();

        Assert.Equal("loss", resolution.KingCaptureResult.OutcomeId);
        Assert.True(resolution.KingCaptureResult.IsTerminal);
        Assert.True(resolution.BenefitsSuppressed);
        Assert.False(resolution.CanProcessCaptureBenefits);
        Assert.Equal(
            TemporaryLibertyCaptureBenefitDisposition.SuppressedByTerminalKingCapture,
            resolution.BenefitDisposition);
        Assert.Single(facts.OfType<TemporaryLibertyKingGateFact>());
        Assert.Single(facts.OfType<CaptureBenefitSuppressedFact>());
        Assert.True(
            FirstIndexOf<TemporaryLibertyKingGateFact>(facts) <
            FirstIndexOf<CaptureBenefitSuppressedFact>(facts));
        Assert.Equal(
            facts.Length - 2,
            FirstIndexOf<CaptureBenefitSuppressedFact>(facts));
        Assert.IsType<TemporaryLibertyExpirySweepResolvedFact>(facts[^1]);
        Assert.Throws<InvalidOperationException>(() =>
            TemporaryLibertyGrantResolver.GrantAfterExpirySweepStarted(
                resolution.TemporaryLibertiesAfterResolution,
                resolution.BoardAfterResolution.Geometry.CreateCanonicalPoint(1, 1),
                "grant.after.terminal",
                1,
                "test",
                resolution.SweepWindow));

        if (fixtureId == "TLE-07")
        {
            // TASK-0027 owns the terminal gate, not the TASK-0028 benefit/resource
            // pipeline. Preserve the armed fixture payload and prove that Domain
            // refuses to expose a continuation which could consume it.
            Assert.Equal("style_sacrifice", fixture.StyleId);
            Assert.Equal(["seal_sacrifice"], fixture.EquippedSeals);
            Assert.Equal(0, fixture.Expected.ReservedDrawDelta);
            Assert.Equal(0, fixture.Expected.SoulDelta);
            Assert.Equal(0, fixture.Expected.CounterattackDeltaUnits);
            Assert.False(resolution.CanProcessCaptureBenefits);
        }
    }

    [Fact]
    public void Tle11MandatoryCaptureRevisitsRestoredTopologyWithoutChangingSeenSet()
    {
        var execution = TemporaryLibertyFixtureData.Execute(RequiredFixture("TLE-11"));
        var resolution = execution.Resolution;

        Assert.Equal(2, execution.SourceHistory.ObservationCount);
        Assert.Equal(2, execution.SourceHistory.UniqueKeyCount);
        Assert.True(execution.SourceHistory.HasSeen(
            StoneTopologyKey.FromBoard(resolution.BoardAfterResolution)));
        Assert.False(resolution.TopologyFirstSeen);
        Assert.Equal(3, resolution.HistoryAfterResolution.ObservationCount);
        Assert.Equal(2, resolution.HistoryAfterResolution.UniqueKeyCount);
        Assert.Equal(
            execution.SourceHistory.OrderedObservations[0],
            resolution.HistoryAfterResolution.Current);
        Assert.Equal(
            resolution.RegisteredTopologyKey,
            resolution.HistoryAfterResolution.Current);
        Assert.Single(resolution.CapturedGroups);
    }

    [Fact]
    public void Tle12CreatesAcceptedIncomeTerritoryWithoutMomentumOrBrilliantFacts()
    {
        var fixture = RequiredFixture("TLE-12");
        var resolution = TemporaryLibertyFixtureData.Execute(fixture).Resolution;
        var policy = FacilityFixtureData.LoadRuntimePolicy();

        Assert.Equal(0, fixture.Expected.MomentumDelta);
        Assert.Equal(0.0, fixture.Expected.BrilliantDelta);
        foreach (var query in fixture.TerritoryQueries)
        {
            var key = $"{query.X},{query.Y}";
            var expected = fixture.Expected.Territories[key];
            var region = Assert.IsType<TerritoryRegion>(
                resolution.TerritoryAfterResolution.RegionAt(query));

            Assert.Equal(expected.Owner, region.Owner);
            Assert.Equal(expected.Size, region.Size);
            Assert.Equal(expected.BasicIncome, policy.TerritoryIncomeForSize(region.Size));
        }

        Assert.DoesNotContain(
            resolution.OrderedFacts,
            fact => fact.GetType().Name.Contains("Momentum", StringComparison.Ordinal));
        Assert.DoesNotContain(
            resolution.OrderedFacts,
            fact => fact.GetType().Name.Contains("Brilliant", StringComparison.Ordinal));
    }

    [Fact]
    public void Tle13NoDueEffectsIsExactReferenceAndEventNoOp()
    {
        var execution = TemporaryLibertyFixtureData.Execute(RequiredFixture("TLE-13"));
        var resolution = execution.Resolution;

        Assert.True(resolution.IsExactNoOp);
        Assert.Same(execution.SourceStones, resolution.StonesAfterResolution);
        Assert.Same(
            execution.SourceTemporaryLiberties,
            resolution.TemporaryLibertiesAfterResolution);
        Assert.Same(execution.SourceHistory, resolution.HistoryAfterResolution);
        Assert.Same(execution.ContinuousLiberties, resolution.ContinuousLiberties);
        Assert.Empty(resolution.ExpiredEffects);
        Assert.Empty(resolution.CarrierRemovedEffects);
        Assert.Empty(resolution.CapturedGroups);
        Assert.Empty(resolution.OrderedFacts);
        Assert.Null(resolution.EffectiveLibertiesAfterExpiry);
        Assert.Null(resolution.TopologyFirstSeen);
        Assert.Null(resolution.RegisteredTopologyKey);
    }

    [Fact]
    public void GrantUsesMergedGroupCanonicalAnchorAndDefersAfterSweepStart()
    {
        var fixture = RequiredFixture("TLE-06");
        var board = Assert.IsType<BoardState>(fixture.Board);
        var stones = TemporaryLibertyFixtureData.CreateStoneRuntime(board);
        var empty = TemporaryLibertyState.Create(stones, [], 1);
        var target = TemporaryLibertyFixtureData.Geometry.CreateCanonicalPoint(4, 4);
        var expectedAnchor = TemporaryLibertyFixtureData.Geometry.CreateCanonicalPoint(3, 4);
        var expectedAnchorInstance = Assert.IsType<StoneRuntimeInstance>(
            stones.InstanceAt(expectedAnchor));

        var beforeSweep = TemporaryLibertyGrantResolver.Grant(
            empty,
            target,
            "grant_before",
            1,
            "test",
            6);
        var sweep = TemporaryLibertyExpiryResolver.Resolve(
            stones,
            empty,
            ContinuousLibertySnapshot.Empty(stones),
            BattleRepetitionHistory.Start(board),
            6);
        Assert.True(sweep.IsExactNoOp);
        var afterSweep = TemporaryLibertyGrantResolver.GrantAfterExpirySweepStarted(
            empty,
            target,
            "grant_after",
            1,
            "test",
            sweep.SweepWindow);

        Assert.Equal(expectedAnchor, beforeSweep.GrantedFact.TargetGroupAnchor);
        Assert.Equal(expectedAnchorInstance.InstanceId, beforeSweep.GrantedEffect.AnchorStoneInstanceId);
        Assert.Equal(6, beforeSweep.GrantedEffect.ExpiresAfterEnemyTurnIndex);
        Assert.Equal(7, afterSweep.GrantedEffect.ExpiresAfterEnemyTurnIndex);
    }

    [Fact]
    public void CarrierRemovalUsesStableAnchorAndDoesNotLeaveFutureExpiryWork()
    {
        var fixture = RequiredFixture("TLE-01");
        var execution = TemporaryLibertyFixtureData.Execute(fixture);
        var anchorPoint = fixture.Effects[0].AnchorPoint;
        var resultBoard = BoardState.Create(
            execution.SourceStones.SourceBoard.Geometry,
            execution.SourceStones.SourceBoard.OccupiedStones
                .Where(stone => stone.Point != anchorPoint));
        var resultStones = StoneRuntimeState.Create(
            resultBoard,
            execution.SourceStones.Instances.Where(instance => instance.Point != anchorPoint),
            execution.SourceStones.NextCreatedSequence);

        var removal = TemporaryLibertyCarrierRemovalResolver.Resolve(
            execution.SourceTemporaryLiberties,
            resultStones);

        Assert.Equal("effect_01", Assert.Single(removal.RemovedEffects).EffectInstanceId);
        Assert.Equal(
            TemporaryLibertyRemovalReason.CarrierRemoved,
            Assert.Single(removal.OrderedFacts).Reason);
        Assert.Empty(removal.StateAfterRemoval.Effects);
    }

    [Fact]
    public void ForeignSnapshotsAndDuplicateStableIdsAreRejected()
    {
        var fixture = RequiredFixture("TLE-06");
        var board = Assert.IsType<BoardState>(fixture.Board);
        var firstStones = TemporaryLibertyFixtureData.CreateStoneRuntime(board);
        var secondStones = TemporaryLibertyFixtureData.CreateStoneRuntime(board);
        var firstTemporary = TemporaryLibertyState.Create(firstStones, [], 1);
        var secondTemporary = TemporaryLibertyState.Create(secondStones, [], 1);
        var firstContinuous = ContinuousLibertySnapshot.Empty(firstStones);

        Assert.Throws<ArgumentException>(() =>
            TemporaryLibertyEffectiveLibertyAnalyzer.Analyze(
                firstStones,
                secondTemporary,
                firstContinuous));

        var duplicateStoneInstances = board.OccupiedStones
            .Select((stone, index) => new StoneRuntimeInstance(
                "duplicate_stone",
                stone,
                "standard",
                index + 1L,
                []))
            .ToArray();
        Assert.Throws<ArgumentException>(() => StoneRuntimeState.Create(
            board,
            duplicateStoneInstances,
            duplicateStoneInstances.Length + 1L));

        var anchor = firstStones.Instances[0];
        var duplicateEffects = new[]
        {
            new TemporaryLibertyEffect(
                "duplicate_effect",
                1,
                anchor.Color,
                anchor.InstanceId,
                "test",
                1,
                6),
            new TemporaryLibertyEffect(
                "duplicate_effect",
                1,
                anchor.Color,
                anchor.InstanceId,
                "test",
                2,
                6),
        };
        Assert.Throws<ArgumentException>(() => TemporaryLibertyState.Create(
            firstStones,
            duplicateEffects,
            3));
    }

    public static TheoryData<string> Task0027FixtureIds =>
        new(TemporaryLibertyFixtureData.Task0027FixtureIds);

    private static TemporaryLibertyFixture RequiredFixture(string fixtureId) =>
        Fixtures.Value.TryGetValue(fixtureId, out var fixture)
            ? fixture
            : throw new InvalidOperationException(
                $"Missing temporary-liberty fixture {fixtureId}.");

    private static CapturedGroupProjection ProjectGroup(StoneGroup group) =>
        new(
            group.Color,
            group.Anchor,
            group.Stones.Count,
            group.Stones.Any(stone => stone.IsKing));

    private static string ProjectFact(IBattleFact fact) => fact switch
    {
        TemporaryLibertyExpirySweepStartedFact => "sweep_started",
        TemporaryLibertyExpiredFact expired => $"expired:{expired.Effect.EffectInstanceId}",
        TemporaryLibertyGroupCapturedFact captured =>
            $"captured:{captured.CapturedGroup.Anchor.X},{captured.CapturedGroup.Anchor.Y}",
        TemporaryLibertyRemovedFact removed =>
            $"removed:{removed.Effect.EffectInstanceId}:{removed.ReasonId}",
        StoneTopologyRegisteredFact topology =>
            $"topology:first_seen={topology.FirstSeen.ToString().ToLowerInvariant()}",
        TemporaryLibertyKingGateFact king => $"king:{king.Result.OutcomeId}",
        CaptureBenefitSuppressedFact suppressed => $"suppressed:{suppressed.ReasonId}",
        TemporaryLibertyExpirySweepResolvedFact => "sweep_resolved",
        _ => fact.GetType().Name,
    };

    private static int FirstIndexOf<TFact>(IReadOnlyList<IBattleFact> facts)
        where TFact : IBattleFact
    {
        for (var index = 0; index < facts.Count; index++)
        {
            if (facts[index] is TFact)
            {
                return index;
            }
        }

        return -1;
    }

    private static int LastIndexOf<TFact>(IReadOnlyList<IBattleFact> facts)
        where TFact : IBattleFact
    {
        for (var index = facts.Count - 1; index >= 0; index--)
        {
            if (facts[index] is TFact)
            {
                return index;
            }
        }

        return -1;
    }

    private static string ColorId(StoneColor color) => color switch
    {
        StoneColor.Black => "black",
        StoneColor.White => "white",
        _ => throw new InvalidOperationException("Unknown stone color."),
    };

    private sealed record CapturedGroupProjection(
        StoneColor Color,
        CanonicalPoint Anchor,
        int Count,
        bool ContainsKing);
}
