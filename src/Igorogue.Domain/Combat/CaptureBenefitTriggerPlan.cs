using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

using Igorogue.Domain.Board;
using Igorogue.Domain.Determinism;

namespace Igorogue.Domain.Combat;

public enum CaptureBenefitTriggerCondition : byte
{
    AnyCapture = 1,
    CapturedWhiteGroup = 2,
    CapturedNonKingBlackStone = 3,
    CapturedSourceStone = 4,
}

public enum CaptureBenefitTriggerMaterializationMode : byte
{
    Fixed = 1,
    GainStandardCaptureSoulPerWhiteGroup = 2,
}

public sealed class CaptureBenefitTriggerPlanEntry
{
    public const string EncodingVersion = "capture-benefit-trigger-plan-entry-v1";

    public CaptureBenefitTriggerPlanEntry(
        CaptureBenefitTrigger trigger,
        CaptureBenefitTriggerCondition condition,
        CaptureBenefitTriggerMaterializationMode materializationMode =
            CaptureBenefitTriggerMaterializationMode.Fixed)
    {
        ArgumentNullException.ThrowIfNull(trigger);
        ValidateCondition(condition);
        ValidateMaterialization(trigger, condition, materializationMode);

        Trigger = trigger;
        Condition = condition;
        MaterializationMode = materializationMode;
        CanonicalText = CreateCanonicalText();
    }

    public CaptureBenefitTrigger Trigger { get; }

    public CaptureBenefitTriggerCondition Condition { get; }

    public CaptureBenefitTriggerMaterializationMode MaterializationMode { get; }

    public string CanonicalText { get; }

    public string ToCanonicalText() => CanonicalText;

    internal CaptureBenefitTrigger? SelectFor(CaptureBatch captureBatch)
    {
        ArgumentNullException.ThrowIfNull(captureBatch);
        var capturedWhiteGroupCount = captureBatch.CapturedGroups.Count(group =>
            group.Color == StoneColor.White &&
            group.CapturingColor == StoneColor.Black);
        var matches = Condition switch
        {
            CaptureBenefitTriggerCondition.AnyCapture => true,
            CaptureBenefitTriggerCondition.CapturedWhiteGroup =>
                capturedWhiteGroupCount > 0,
            CaptureBenefitTriggerCondition.CapturedNonKingBlackStone =>
                captureBatch.CapturedGroups.Any(group =>
                    group.Color == StoneColor.Black &&
                    group.CapturingColor == StoneColor.White &&
                    group.StoneInstances.Any(instance => !instance.IsKing)),
            CaptureBenefitTriggerCondition.CapturedSourceStone =>
                ContainsCapturedSourceStone(captureBatch),
            _ => throw new InvalidOperationException(
                "Capture benefit trigger plan entry contains an unknown condition."),
        };
        if (!matches)
        {
            return null;
        }

        return MaterializationMode switch
        {
            CaptureBenefitTriggerMaterializationMode.Fixed => Trigger,
            CaptureBenefitTriggerMaterializationMode
                    .GainStandardCaptureSoulPerWhiteGroup =>
                MaterializeStandardCaptureSoul(capturedWhiteGroupCount),
            _ => throw new InvalidOperationException(
                "Capture benefit trigger plan entry contains an unknown materialization mode."),
        };
    }

    private bool ContainsCapturedSourceStone(CaptureBatch captureBatch)
    {
        var belongsToBatch = captureBatch.CapturedStoneInstances.Any(instance =>
            StringComparer.Ordinal.Equals(instance.InstanceId, Trigger.Source.SourceId));
        if (belongsToBatch)
        {
            _ = Trigger.Source.Bind(captureBatch);
        }

        return belongsToBatch;
    }

    private CaptureBenefitTrigger MaterializeStandardCaptureSoul(
        int capturedWhiteGroupCount)
    {
        if (capturedWhiteGroupCount <= 0)
        {
            throw new InvalidOperationException(
                "Per-white-group standard accounting requires a captured white group.");
        }

        var template = (GainStandardCaptureSoulOperation)Trigger.OrderedOperations[0];
        return new CaptureBenefitTrigger(
            Trigger.Source,
            Trigger.TriggerId,
            Trigger.EventPath,
            [new GainStandardCaptureSoulOperation(
                template.SoulPerCapturedGroup,
                capturedWhiteGroupCount,
                template.BattleRewardLimit)],
            Trigger.FirstUseFlagId);
    }

    private string CreateCanonicalText() => string.Join(
        '\n',
        EncodingVersion,
        $"condition={ConditionId(Condition)}",
        $"materialization={MaterializationId(MaterializationMode)}",
        $"trigger={EncodeStableText(Trigger.ToCanonicalText())}");

    private static void ValidateCondition(CaptureBenefitTriggerCondition condition)
    {
        if (condition is < CaptureBenefitTriggerCondition.AnyCapture or
            > CaptureBenefitTriggerCondition.CapturedSourceStone)
        {
            throw new ArgumentOutOfRangeException(
                nameof(condition),
                condition,
                "Unknown capture benefit trigger condition.");
        }
    }

    private static void ValidateMaterialization(
        CaptureBenefitTrigger trigger,
        CaptureBenefitTriggerCondition condition,
        CaptureBenefitTriggerMaterializationMode materializationMode)
    {
        if (materializationMode is < CaptureBenefitTriggerMaterializationMode.Fixed or
            > CaptureBenefitTriggerMaterializationMode
                .GainStandardCaptureSoulPerWhiteGroup)
        {
            throw new ArgumentOutOfRangeException(
                nameof(materializationMode),
                materializationMode,
                "Unknown capture benefit trigger materialization mode.");
        }

        if (condition == CaptureBenefitTriggerCondition.CapturedSourceStone &&
            trigger.Source.Kind != CaptureBenefitSourceKind.CapturedStoneSelf)
        {
            throw new ArgumentException(
                "Captured-source-stone conditions require a captured-stone-self source.",
                nameof(trigger));
        }

        if (trigger.Source.Kind == CaptureBenefitSourceKind.CapturedStoneSelf &&
            condition != CaptureBenefitTriggerCondition.CapturedSourceStone)
        {
            throw new ArgumentException(
                "Captured-stone-self sources require a captured-source-stone condition.",
                nameof(trigger));
        }

        var containsStandardCaptureReward = trigger.OrderedOperations.Any(operation =>
            operation is GainStandardCaptureSoulOperation);
        if (containsStandardCaptureReward &&
            materializationMode != CaptureBenefitTriggerMaterializationMode
                .GainStandardCaptureSoulPerWhiteGroup)
        {
            throw new ArgumentException(
                "Standard capture Soul operations require standard per-white-group materialization.",
                nameof(trigger));
        }

        if (materializationMode !=
            CaptureBenefitTriggerMaterializationMode
                .GainStandardCaptureSoulPerWhiteGroup)
        {
            return;
        }

        if (condition != CaptureBenefitTriggerCondition.CapturedWhiteGroup ||
            trigger.Source.Kind != CaptureBenefitSourceKind.StandardAccounting ||
            trigger.FirstUseFlagId is not null ||
            trigger.OrderedOperations.Count != 1 ||
            trigger.OrderedOperations[0] is not GainStandardCaptureSoulOperation standard ||
            standard.CapturedWhiteGroupCount != 1)
        {
            throw new ArgumentException(
                "Per-white-group standard Soul materialization requires an unguarded standard-accounting trigger with one single-group standard reward template operation.",
                nameof(trigger));
        }
    }

    private static string ConditionId(CaptureBenefitTriggerCondition condition) =>
        condition switch
        {
            CaptureBenefitTriggerCondition.AnyCapture => "any_capture",
            CaptureBenefitTriggerCondition.CapturedWhiteGroup => "captured_white_group",
            CaptureBenefitTriggerCondition.CapturedNonKingBlackStone =>
                "captured_nonking_black_stone",
            CaptureBenefitTriggerCondition.CapturedSourceStone =>
                "captured_source_stone",
            _ => throw new InvalidOperationException(
                "Capture benefit trigger plan entry contains an unknown condition."),
        };

    private static string MaterializationId(
        CaptureBenefitTriggerMaterializationMode materializationMode) =>
        materializationMode switch
        {
            CaptureBenefitTriggerMaterializationMode.Fixed => "fixed",
            CaptureBenefitTriggerMaterializationMode
                    .GainStandardCaptureSoulPerWhiteGroup =>
                "gain_standard_capture_soul_per_white_group",
            _ => throw new InvalidOperationException(
                "Capture benefit trigger plan entry contains an unknown materialization mode."),
        };

    private static string EncodeStableText(string value) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
}

public sealed class CaptureBenefitTriggerPlan
{
    public const string EncodingVersion = "capture-benefit-trigger-plan-v2";

    private readonly ReadOnlyCollection<CaptureBenefitTriggerPlanEntry> entryView;
    private readonly ReadOnlyCollection<CaptureBenefitTrigger> triggerView;

    private CaptureBenefitTriggerPlan(
        CaptureBenefitTriggerPlanEntry[] canonicalEntries)
    {
        entryView = Array.AsReadOnly(
            (CaptureBenefitTriggerPlanEntry[])canonicalEntries.Clone());
        triggerView = Array.AsReadOnly(
            canonicalEntries.Select(entry => entry.Trigger).ToArray());
        CanonicalText = CreateCanonicalText();
        Checksum = DeterministicChecksum.Sha256Hex(CanonicalText);
    }

    public IReadOnlyList<CaptureBenefitTriggerPlanEntry> Entries => entryView;

    public IReadOnlyList<CaptureBenefitTrigger> Triggers => triggerView;

    public string CanonicalText { get; }

    public string Checksum { get; }

    public static CaptureBenefitTriggerPlan Create(
        IEnumerable<CaptureBenefitTrigger> triggers)
    {
        ArgumentNullException.ThrowIfNull(triggers);
        return CreateConditional(triggers.Select(trigger =>
            new CaptureBenefitTriggerPlanEntry(
                trigger ?? throw new ArgumentNullException(nameof(triggers)),
                CaptureBenefitTriggerCondition.AnyCapture,
                CaptureBenefitTriggerMaterializationMode.Fixed)));
    }

    public static CaptureBenefitTriggerPlan CreateConditional(
        IEnumerable<CaptureBenefitTriggerPlanEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        var canonicalEntries = entries.ToArray();
        foreach (var entry in canonicalEntries)
        {
            ArgumentNullException.ThrowIfNull(entry);
        }

        RejectDuplicates(
            canonicalEntries,
            entry => entry.Trigger.TriggerId,
            "Capture benefit trigger plan IDs must be unique.",
            nameof(entries));
        RejectDuplicates(
            canonicalEntries,
            entry => $"{((int)entry.Trigger.Source.Kind).ToString(CultureInfo.InvariantCulture)}\0{entry.Trigger.Source.SourceId}",
            "Capture benefit trigger plan source identities must be unique.",
            nameof(entries));
        RejectDuplicates(
            canonicalEntries,
            entry => string.Join(':', entry.Trigger.EventPath),
            "Capture benefit trigger plan event paths must be unique.",
            nameof(entries));
        RejectDuplicates(
            canonicalEntries.Where(entry => entry.Trigger.FirstUseFlagId is not null),
            entry => entry.Trigger.FirstUseFlagId!,
            "Capture benefit trigger plan first-use flag IDs must be unique.",
            nameof(entries));

        Array.Sort(canonicalEntries, CompareEntries);
        return new CaptureBenefitTriggerPlan(canonicalEntries);
    }

    public IReadOnlyList<CaptureBenefitTrigger> SelectFor(CaptureBatch captureBatch)
    {
        ArgumentNullException.ThrowIfNull(captureBatch);
        return Array.AsReadOnly(entryView
            .Select(entry => entry.SelectFor(captureBatch))
            .Where(trigger => trigger is not null)
            .Cast<CaptureBenefitTrigger>()
            .ToArray());
    }

    public string ToCanonicalText() => CanonicalText;

    private string CreateCanonicalText()
    {
        var lines = new List<string>(2 + entryView.Count)
        {
            EncodingVersion,
            $"entry_count={entryView.Count.ToString(CultureInfo.InvariantCulture)}",
        };
        lines.AddRange(entryView.Select(entry =>
            $"entry={EncodeStableText(entry.ToCanonicalText())}"));
        return string.Join('\n', lines);
    }

    private static int CompareEntries(
        CaptureBenefitTriggerPlanEntry left,
        CaptureBenefitTriggerPlanEntry right)
    {
        var conditionComparison = left.Condition.CompareTo(right.Condition);
        if (conditionComparison != 0)
        {
            return conditionComparison;
        }

        var modeComparison = left.MaterializationMode.CompareTo(
            right.MaterializationMode);
        return modeComparison != 0
            ? modeComparison
            : StringComparer.Ordinal.Compare(
                left.Trigger.ToCanonicalText(),
                right.Trigger.ToCanonicalText());
    }

    private static void RejectDuplicates(
        IEnumerable<CaptureBenefitTriggerPlanEntry> entries,
        Func<CaptureBenefitTriggerPlanEntry, string> keySelector,
        string message,
        string parameterName)
    {
        if (entries
            .GroupBy(keySelector, StringComparer.Ordinal)
            .Any(group => group.Count() > 1))
        {
            throw new ArgumentException(message, parameterName);
        }
    }

    private static string EncodeStableText(string value) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
}
