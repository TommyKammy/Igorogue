using Igorogue.Domain.Board;
using Igorogue.Domain.Content;
using Igorogue.Domain.Facilities;

namespace Igorogue.Domain.Enemies;

public static class BanditCandidateGenerator
{
    public static IReadOnlyList<BanditPlacementCandidate> Generate(
        BanditPlanningContext context,
        EnemyContentDefinition enemy,
        EnemyIntentKind intentKind,
        StoneRuntimePlacementDescriptor placementDescriptor)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(enemy);
        ArgumentNullException.ThrowIfNull(placementDescriptor);
        BanditContentContract.Validate(enemy);
        if (!Enum.IsDefined(intentKind))
        {
            throw new ArgumentOutOfRangeException(nameof(intentKind), intentKind, "Unknown enemy intent.");
        }

        var intent = enemy.Intents.SingleOrDefault(candidate => candidate.Kind == intentKind)
            ?? throw new ArgumentException(
                $"Enemy definition does not contain intent {intentKind}.",
                nameof(enemy));
        if (intentKind == EnemyIntentKind.DefendWhiteKing &&
            context.EffectiveLibertiesFor(context.WhiteKingGroup) >
                enemy.Parameters.DefenseThreshold)
        {
            return Array.Empty<BanditPlacementCandidate>();
        }

        var candidates = new List<BanditPlacementCandidate>();
        foreach (var point in context.Board.Geometry.CanonicalPoints)
        {
            var accessMode = ResolveAccessMode(context.Board, point, intent.PlacementModes);
            if (accessMode is null)
            {
                continue;
            }

            var placement = RuntimeStonePlacementEvaluator.Evaluate(
                context.StoneRuntimeState,
                context.TemporaryLibertyState,
                context.ContinuousLibertySnapshot,
                context.RepetitionHistory,
                new BoardStone(StoneColor.White, false, point),
                accessMode.Value,
                placementDescriptor);
            if (!placement.Accepted)
            {
                continue;
            }

            var candidate = CreateCandidate(
                context,
                enemy,
                intentKind,
                point,
                accessMode.Value,
                placement);
            if (candidate is not null)
            {
                candidates.Add(candidate);
            }
        }

        return BanditCandidateRanker.Rank(candidates);
    }

    private static BanditPlacementCandidate? CreateCandidate(
        BanditPlanningContext context,
        EnemyContentDefinition enemy,
        EnemyIntentKind intentKind,
        CanonicalPoint point,
        PlacementAccessMode accessMode,
        RuntimeStonePlacementEvaluation placement)
    {
        var legal = placement.LegalPlacementCommit
            ?? throw new InvalidOperationException("Accepted placement is missing its legal commit.");
        var postCapture = placement.PostCaptureEffectiveLiberties
            ?? throw new InvalidOperationException("Accepted placement is missing post-capture liberties.");
        var facilityCommit = FacilityPlacementIntegrator.Apply(context.FacilityState, legal);
        var territoryAfter = TerritoryAnalyzer.Analyze(facilityCommit.BoardAfterCommit);
        var facilityTransition = FacilityOperatingTransitionResolver.ReassociateAfterPlacement(
            context.FacilityRuntimeAnalysis,
            facilityCommit,
            territoryAfter);
        var capturedSourceGroups = legal.Candidate.CapturedGroups
            .Select(group => context.GroupAnalysis.GroupAt(group.Anchor)
                ?? throw new InvalidOperationException(
                    "A captured group must map to its exact source group."))
            .DistinctBy(group => group.Anchor)
            .OrderBy(group => group.Anchor)
            .ToArray();
        var placedGroup = postCapture.GroupAnalysis.GroupAt(point)
            ?? throw new InvalidOperationException("A legal placement must retain its placed group.");
        var placedEffective = postCapture.BreakdownFor(placedGroup).EffectiveLibertyCount;
        var blackKingAfter = ResultKingGroup(postCapture, context.BlackKingGroup);
        var whiteKingAfter = ResultKingGroup(postCapture, context.WhiteKingGroup);
        int? blackKingEffectiveAfter = blackKingAfter is null
            ? null
            : postCapture.BreakdownFor(blackKingAfter).EffectiveLibertyCount;
        int? whiteKingEffectiveAfter = whiteKingAfter is null
            ? null
            : postCapture.BreakdownFor(whiteKingAfter).EffectiveLibertyCount;
        var connectedWhiteGroups = AdjacentGroups(
            context,
            point,
            StoneColor.White);

        StoneGroup? targetGroup = intentKind switch
        {
            EnemyIntentKind.CaptureBlackKing =>
                capturedSourceGroups.Any(ContainsKing)
                    ? context.BlackKingGroup
                    : null,
            EnemyIntentKind.DefendWhiteKing => DefenseTarget(
                context,
                placedGroup,
                whiteKingAfter,
                whiteKingEffectiveAfter,
                capturedSourceGroups),
            EnemyIntentKind.CaptureNonKing => CaptureNonKingTarget(
                context,
                enemy,
                capturedSourceGroups),
            EnemyIntentKind.PressureBlackKing => PressureTarget(
                context,
                point,
                blackKingAfter,
                blackKingEffectiveAfter,
                capturedSourceGroups),
            EnemyIntentKind.AdvanceTowardBlackKing => context.BlackKingGroup,
            _ => throw new ArgumentOutOfRangeException(
                nameof(intentKind),
                intentKind,
                "Unknown enemy intent."),
        };
        if (targetGroup is null)
        {
            return null;
        }

        var connectedOtherWhiteGroups = intentKind == EnemyIntentKind.DefendWhiteKing
            ? connectedWhiteGroups.Count(group => !ReferenceEquals(group, context.WhiteKingGroup))
            : connectedWhiteGroups.Length;
        var targetDistance = MinimumStoneDistance(targetGroup, context.BlackKingGroup);
        var advanceDistance = MinimumAdvanceDistance(point, context.BlackKingGroup);
        var centerCoordinate = (context.Board.Geometry.Size + 1) / 2;
        var center = context.Board.Geometry.CreateCanonicalPoint(
            centerCoordinate,
            centerCoordinate);

        return new BanditPlacementCandidate(
            intentKind,
            targetGroup,
            point,
            accessMode,
            placement,
            facilityCommit,
            territoryAfter,
            facilityTransition,
            capturedSourceGroups,
            placedEffective,
            whiteKingEffectiveAfter,
            blackKingEffectiveAfter,
            connectedOtherWhiteGroups,
            targetDistance,
            advanceDistance,
            Manhattan(point, center));
    }

    private static PlacementAccessMode? ResolveAccessMode(
        BoardState board,
        CanonicalPoint point,
        IReadOnlyCollection<EnemyPlacementMode> modes)
    {
        if (!board.IsEmpty(point))
        {
            return null;
        }

        var adjacentWhite = HasAdjacentStone(board, point, StoneColor.White);
        var adjacentBlack = HasAdjacentStone(board, point, StoneColor.Black);
        var normalAllowed =
            (modes.Contains(EnemyPlacementMode.WhiteFrontline) && adjacentWhite) ||
            (modes.Contains(EnemyPlacementMode.WhiteContact) && adjacentWhite && adjacentBlack);
        if (normalAllowed)
        {
            return PlacementAccessMode.Normal;
        }

        return modes.Contains(EnemyPlacementMode.WhiteTerminal)
            ? PlacementAccessMode.TerminalCapture
            : null;
    }

    private static StoneGroup? DefenseTarget(
        BanditPlanningContext context,
        StoneGroup placedGroup,
        StoneGroup? whiteKingAfter,
        int? whiteKingEffectiveAfter,
        IReadOnlyList<StoneGroup> capturedSourceGroups)
    {
        var effectiveBefore = context.EffectiveLibertiesFor(context.WhiteKingGroup);
        if (whiteKingEffectiveAfter is int after &&
            after > effectiveBefore &&
            whiteKingAfter is not null &&
            (ReferenceEquals(placedGroup, whiteKingAfter) ||
             CapturedGroupWasAdjacentToKing(
                 context,
                 capturedSourceGroups,
                 context.WhiteKingGroup)))
        {
            return context.WhiteKingGroup;
        }

        return null;
    }

    private static StoneGroup? CaptureNonKingTarget(
        BanditPlanningContext context,
        EnemyContentDefinition enemy,
        IReadOnlyList<StoneGroup> capturedSourceGroups)
    {
        if (capturedSourceGroups.Count == 0 ||
            capturedSourceGroups.Any(ContainsKing) ||
            capturedSourceGroups.Sum(group => group.Stones.Count) <
                enemy.Parameters.OpportunisticCaptureMinStones)
        {
            return null;
        }

        return capturedSourceGroups
            .OrderByDescending(group => group.Stones.Count)
            .ThenBy(group => MinimumStoneDistance(group, context.BlackKingGroup))
            .ThenBy(group => group.Anchor)
            .First();
    }

    private static StoneGroup? PressureTarget(
        BanditPlanningContext context,
        CanonicalPoint point,
        StoneGroup? blackKingAfter,
        int? blackKingEffectiveAfter,
        IReadOnlyList<StoneGroup> capturedSourceGroups)
    {
        if (!context.BlackKingGroup.RealLiberties.Contains(point) ||
            capturedSourceGroups.Any(ContainsKing) ||
            blackKingAfter is null ||
            blackKingEffectiveAfter is not int after ||
            after >= context.EffectiveLibertiesFor(context.BlackKingGroup))
        {
            return null;
        }

        return context.BlackKingGroup;
    }

    private static StoneGroup? ResultKingGroup(
        TemporaryLibertyEffectiveLibertyAnalysis postCapture,
        StoneGroup sourceKingGroup)
    {
        var king = sourceKingGroup.Stones.Single(stone => stone.IsKing);
        return postCapture.SourceStones.SourceBoard.StoneAt(king.Point) is null
            ? null
            : postCapture.GroupAnalysis.GroupAt(king.Point);
    }

    private static StoneGroup[] AdjacentGroups(
        BanditPlanningContext context,
        CanonicalPoint point,
        StoneColor color) =>
        context.Board.Geometry.GetOrthogonalNeighbours(point)
            .Select(context.GroupAnalysis.GroupAt)
            .Where(group => group?.Color == color)
            .Cast<StoneGroup>()
            .DistinctBy(group => group.Anchor)
            .OrderBy(group => group.Anchor)
            .ToArray();

    private static bool CapturedGroupWasAdjacentToKing(
        BanditPlanningContext context,
        IReadOnlyList<StoneGroup> capturedGroups,
        StoneGroup kingGroup) =>
        capturedGroups.Any(captured => captured.Stones.Any(capturedStone =>
            kingGroup.Stones.Any(kingStone =>
                context.Board.Geometry.GetOrthogonalNeighbours(kingStone.Point)
                    .Contains(capturedStone.Point))));

    private static bool HasAdjacentStone(
        BoardState board,
        CanonicalPoint point,
        StoneColor color) =>
        board.Geometry.GetOrthogonalNeighbours(point)
            .Any(neighbour => board.StoneAt(neighbour)?.Color == color);

    private static bool ContainsKing(StoneGroup group) =>
        group.Stones.Any(stone => stone.IsKing);

    internal static int MinimumStoneDistance(StoneGroup left, StoneGroup right) =>
        left.Stones.Min(leftStone => right.Stones.Min(rightStone =>
            Manhattan(leftStone.Point, rightStone.Point)));

    internal static int MinimumAdvanceDistance(
        CanonicalPoint point,
        StoneGroup blackKingGroup) =>
        blackKingGroup.RealLiberties.Count > 0
            ? blackKingGroup.RealLiberties.Min(liberty => Manhattan(point, liberty))
            : blackKingGroup.Stones.Min(stone => Manhattan(point, stone.Point));

    internal static int Manhattan(CanonicalPoint left, CanonicalPoint right) =>
        Math.Abs(left.X - right.X) + Math.Abs(left.Y - right.Y);
}
