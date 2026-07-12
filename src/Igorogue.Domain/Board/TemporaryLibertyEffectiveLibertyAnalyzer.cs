using System.Collections.ObjectModel;
using System.Globalization;

using Igorogue.Domain.Combat;

namespace Igorogue.Domain.Board;

public sealed class TemporaryLibertyGroupBreakdown
{
    internal TemporaryLibertyGroupBreakdown(
        StoneGroup group,
        int timedAmount,
        int continuousAmount,
        int effectiveLibertyCount)
    {
        Group = group;
        TimedAmount = timedAmount;
        ContinuousAmount = continuousAmount;
        EffectiveLibertyCount = effectiveLibertyCount;
    }

    public StoneGroup Group { get; }

    public int RealLibertyCount => Group.RealLibertyCount;

    public int TimedAmount { get; }

    public int ContinuousAmount { get; }

    public int EffectiveLibertyCount { get; }
}

public sealed class TemporaryLibertyEffectiveLibertyAnalysis
{
    private readonly ReadOnlyCollection<TemporaryLibertyGroupBreakdown> breakdownView;
    private readonly TemporaryLibertyGroupBreakdown?[] breakdownsByAnchorIndex;

    internal TemporaryLibertyEffectiveLibertyAnalysis(
        StoneRuntimeState sourceStones,
        TemporaryLibertyState temporaryLiberties,
        ContinuousLibertySnapshot continuousLiberties,
        StoneGroupAnalysis groupAnalysis,
        EffectiveLibertySnapshot effectiveLiberties,
        TemporaryLibertyGroupBreakdown[] breakdowns,
        TemporaryLibertyGroupBreakdown?[] breakdownsByAnchorIndex)
    {
        SourceStones = sourceStones;
        TemporaryLiberties = temporaryLiberties;
        ContinuousLiberties = continuousLiberties;
        GroupAnalysis = groupAnalysis;
        EffectiveLiberties = effectiveLiberties;
        breakdownView = Array.AsReadOnly(
            (TemporaryLibertyGroupBreakdown[])breakdowns.Clone());
        this.breakdownsByAnchorIndex =
            (TemporaryLibertyGroupBreakdown?[])breakdownsByAnchorIndex.Clone();
    }

    public StoneGroupAnalysis GroupAnalysis { get; }

    public StoneRuntimeState SourceStones { get; }

    public TemporaryLibertyState TemporaryLiberties { get; }

    public ContinuousLibertySnapshot ContinuousLiberties { get; }

    public EffectiveLibertySnapshot EffectiveLiberties { get; }

    public IReadOnlyList<TemporaryLibertyGroupBreakdown> Breakdowns => breakdownView;

    public TemporaryLibertyGroupBreakdown BreakdownFor(StoneGroup group)
    {
        ArgumentNullException.ThrowIfNull(group);
        var anchorIndex = GroupAnalysis.SourceBoard.Geometry.ToCanonicalIndex(group.Anchor);
        var breakdown = breakdownsByAnchorIndex[anchorIndex];
        if (breakdown is null || !ReferenceEquals(breakdown.Group, group))
        {
            throw new ArgumentException(
                "Stone group does not belong to this temporary-liberty analysis.",
                nameof(group));
        }

        return breakdown;
    }

    public string ToCanonicalText()
    {
        var lines = new List<string>(1 + (breakdownView.Count * 6))
        {
            "temporary-liberty-effective-analysis-v1",
        };
        for (var index = 0; index < breakdownView.Count; index++)
        {
            var breakdown = breakdownView[index];
            lines.Add($"group_index={index.ToString(CultureInfo.InvariantCulture)}");
            lines.Add(
                $"anchor={breakdown.Group.Anchor.X.ToString(CultureInfo.InvariantCulture)},{breakdown.Group.Anchor.Y.ToString(CultureInfo.InvariantCulture)}");
            lines.Add($"real={breakdown.RealLibertyCount.ToString(CultureInfo.InvariantCulture)}");
            lines.Add($"timed={breakdown.TimedAmount.ToString(CultureInfo.InvariantCulture)}");
            lines.Add(
                $"continuous={breakdown.ContinuousAmount.ToString(CultureInfo.InvariantCulture)}");
            lines.Add(
                $"effective={breakdown.EffectiveLibertyCount.ToString(CultureInfo.InvariantCulture)}");
        }

        return string.Join('\n', lines);
    }
}

public static class TemporaryLibertyEffectiveLibertyAnalyzer
{
    public static TemporaryLibertyEffectiveLibertyAnalysis Analyze(
        StoneRuntimeState sourceStones,
        TemporaryLibertyState temporaryLiberties,
        ContinuousLibertySnapshot continuousLiberties) =>
        Analyze(
            sourceStones,
            temporaryLiberties,
            continuousLiberties,
            StoneGroupAnalyzer.Analyze(sourceStones?.SourceBoard
                ?? throw new ArgumentNullException(nameof(sourceStones))));

    public static TemporaryLibertyEffectiveLibertyAnalysis Analyze(
        StoneRuntimeState sourceStones,
        TemporaryLibertyState temporaryLiberties,
        ContinuousLibertySnapshot continuousLiberties,
        StoneGroupAnalysis exactGroupAnalysis)
    {
        ArgumentNullException.ThrowIfNull(sourceStones);
        ArgumentNullException.ThrowIfNull(temporaryLiberties);
        ArgumentNullException.ThrowIfNull(continuousLiberties);
        ArgumentNullException.ThrowIfNull(exactGroupAnalysis);
        if (!ReferenceEquals(sourceStones, temporaryLiberties.SourceStones))
        {
            throw new ArgumentException(
                "Temporary liberties must belong to the exact stone runtime snapshot.",
                nameof(temporaryLiberties));
        }

        if (!ReferenceEquals(sourceStones, continuousLiberties.SourceStones))
        {
            throw new ArgumentException(
                "Continuous liberties must belong to the exact stone runtime snapshot.",
                nameof(continuousLiberties));
        }

        if (!ReferenceEquals(sourceStones.SourceBoard, exactGroupAnalysis.SourceBoard))
        {
            throw new ArgumentException(
                "Stone groups must belong to the runtime's exact board snapshot.",
                nameof(exactGroupAnalysis));
        }

        var groupAnalysis = exactGroupAnalysis;
        var timedByAnchorIndex = new int[sourceStones.SourceBoard.Geometry.PointCount];
        var continuousByAnchorIndex = new int[sourceStones.SourceBoard.Geometry.PointCount];

        foreach (var effect in temporaryLiberties.Effects)
        {
            AddContribution(
                sourceStones,
                groupAnalysis,
                effect.AnchorStoneInstanceId,
                effect.OwnerColor,
                effect.Amount,
                timedByAnchorIndex);
        }

        foreach (var modifier in continuousLiberties.Modifiers)
        {
            AddContribution(
                sourceStones,
                groupAnalysis,
                modifier.AnchorStoneInstanceId,
                modifier.OwnerColor,
                modifier.Amount,
                continuousByAnchorIndex);
        }

        var breakdowns = new TemporaryLibertyGroupBreakdown[groupAnalysis.Groups.Count];
        var breakdownsByAnchorIndex =
            new TemporaryLibertyGroupBreakdown?[sourceStones.SourceBoard.Geometry.PointCount];
        var effectiveGroups = new GroupEffectiveLiberty[groupAnalysis.Groups.Count];
        for (var index = 0; index < groupAnalysis.Groups.Count; index++)
        {
            var group = groupAnalysis.Groups[index];
            var anchorIndex = sourceStones.SourceBoard.Geometry.ToCanonicalIndex(group.Anchor);
            int effective;
            try
            {
                effective = checked(
                    group.RealLibertyCount +
                    timedByAnchorIndex[anchorIndex] +
                    continuousByAnchorIndex[anchorIndex]);
            }
            catch (OverflowException exception)
            {
                throw new InvalidOperationException(
                    $"Effective liberty count overflowed for group {group.Anchor}.",
                    exception);
            }

            if (effective < 0)
            {
                throw new InvalidOperationException(
                    $"Effective liberty count cannot be negative for group {group.Anchor}.");
            }

            var breakdown = new TemporaryLibertyGroupBreakdown(
                group,
                timedByAnchorIndex[anchorIndex],
                continuousByAnchorIndex[anchorIndex],
                effective);
            breakdowns[index] = breakdown;
            breakdownsByAnchorIndex[anchorIndex] = breakdown;
            effectiveGroups[index] = new GroupEffectiveLiberty(group, effective);
        }

        return new TemporaryLibertyEffectiveLibertyAnalysis(
            sourceStones,
            temporaryLiberties,
            continuousLiberties,
            groupAnalysis,
            EffectiveLibertySnapshot.Create(groupAnalysis, effectiveGroups),
            breakdowns,
            breakdownsByAnchorIndex);
    }

    private static void AddContribution(
        StoneRuntimeState sourceStones,
        StoneGroupAnalysis groupAnalysis,
        string anchorStoneInstanceId,
        StoneColor ownerColor,
        int amount,
        int[] contributionsByAnchorIndex)
    {
        var anchor = sourceStones.InstanceById(anchorStoneInstanceId)
            ?? throw new InvalidOperationException(
                $"Liberty contribution anchor {anchorStoneInstanceId} is no longer live.");
        if (anchor.Color != ownerColor)
        {
            throw new InvalidOperationException(
                $"Liberty contribution owner does not match anchor {anchorStoneInstanceId}.");
        }

        var group = groupAnalysis.GroupAt(anchor.Point)
            ?? throw new InvalidOperationException(
                $"Liberty contribution anchor {anchorStoneInstanceId} has no current group.");
        var groupAnchorIndex = sourceStones.SourceBoard.Geometry.ToCanonicalIndex(group.Anchor);
        try
        {
            contributionsByAnchorIndex[groupAnchorIndex] = checked(
                contributionsByAnchorIndex[groupAnchorIndex] + amount);
        }
        catch (OverflowException exception)
        {
            throw new InvalidOperationException(
                $"Liberty contribution overflowed for group {group.Anchor}.",
                exception);
        }
    }
}
