using System.Collections.ObjectModel;

namespace Igorogue.Domain.Board;

public sealed class EffectiveLibertySnapshot
{
    private readonly GroupEffectiveLiberty?[] groupsByAnchorIndex;
    private readonly ReadOnlyCollection<GroupEffectiveLiberty> groupView;

    private EffectiveLibertySnapshot(
        StoneGroupAnalysis groupAnalysis,
        GroupEffectiveLiberty[] groups,
        GroupEffectiveLiberty?[] groupsByAnchorIndex)
    {
        GroupAnalysis = groupAnalysis;
        groupView = Array.AsReadOnly((GroupEffectiveLiberty[])groups.Clone());
        this.groupsByAnchorIndex = (GroupEffectiveLiberty?[])groupsByAnchorIndex.Clone();
    }

    public StoneGroupAnalysis GroupAnalysis { get; }

    public IReadOnlyList<GroupEffectiveLiberty> Groups => groupView;

    public static EffectiveLibertySnapshot Create(
        StoneGroupAnalysis groupAnalysis,
        IEnumerable<GroupEffectiveLiberty> groups)
    {
        ArgumentNullException.ThrowIfNull(groupAnalysis);
        ArgumentNullException.ThrowIfNull(groups);

        var geometry = groupAnalysis.Geometry;
        var byAnchorIndex = new GroupEffectiveLiberty?[geometry.PointCount];
        foreach (var groupEffectiveLiberty in groups)
        {
            ArgumentNullException.ThrowIfNull(groupEffectiveLiberty);
            var group = groupEffectiveLiberty.Group;
            var anchorIndex = geometry.ToCanonicalIndex(group.Anchor);
            // Group anchors are derived, so identity binds these facts to one exact analysis
            // rather than allowing them to drift across a later board re-analysis.
            if (!ReferenceEquals(groupAnalysis.GroupAt(group.Anchor), group))
            {
                throw new ArgumentException(
                    $"Effective liberty group at {group.Anchor} does not belong to the supplied analysis.",
                    nameof(groups));
            }

            if (byAnchorIndex[anchorIndex] is not null)
            {
                throw new ArgumentException(
                    $"Effective liberty snapshot contains duplicate group anchor {group.Anchor}.",
                    nameof(groups));
            }

            byAnchorIndex[anchorIndex] = groupEffectiveLiberty;
        }

        var ordered = new GroupEffectiveLiberty[groupAnalysis.Groups.Count];
        for (var index = 0; index < groupAnalysis.Groups.Count; index++)
        {
            var group = groupAnalysis.Groups[index];
            var anchorIndex = geometry.ToCanonicalIndex(group.Anchor);
            ordered[index] = byAnchorIndex[anchorIndex]
                ?? throw new ArgumentException(
                    $"Effective liberty snapshot is missing group anchor {group.Anchor}.",
                    nameof(groups));
        }

        return new EffectiveLibertySnapshot(groupAnalysis, ordered, byAnchorIndex);
    }

    public int EffectiveLibertiesFor(StoneGroup group)
    {
        ArgumentNullException.ThrowIfNull(group);
        var anchorIndex = GroupAnalysis.Geometry.ToCanonicalIndex(group.Anchor);
        var entry = groupsByAnchorIndex[anchorIndex];
        if (entry is null || !ReferenceEquals(entry.Group, group))
        {
            throw new ArgumentException(
                "Stone group does not belong to this effective liberty snapshot.",
                nameof(group));
        }

        return entry.EffectiveLibertyCount;
    }
}

public sealed record GroupEffectiveLiberty
{
    public GroupEffectiveLiberty(StoneGroup group, int effectiveLibertyCount)
    {
        ArgumentNullException.ThrowIfNull(group);
        // Timed effects are positive today, but the accepted continuous-modifier formula
        // does not constrain every contribution to be positive. Only the final count is bounded.
        if (effectiveLibertyCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(effectiveLibertyCount),
                effectiveLibertyCount,
                "Effective liberty count cannot be negative.");
        }

        Group = group;
        EffectiveLibertyCount = effectiveLibertyCount;
    }

    public StoneGroup Group { get; }

    public CanonicalPoint GroupAnchor => Group.Anchor;

    public int RealLibertyCount => Group.RealLibertyCount;

    public int EffectiveLibertyCount { get; }
}
