using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

using Igorogue.Domain.Board;
using Igorogue.Domain.Determinism;

namespace Igorogue.Domain.Combat;

public enum CaptureBenefitStage : byte
{
    StandardAccounting = 1,
    SourceOrArmedEffect = 2,
    CapturedStoneSelf = 3,
    StyleOrSeal = 4,
    Relic = 5,
    Facility = 6,
    EnemyPassive = 7,
    Sacrifice = 8,
    ScoreOrTelemetry = 9,
}

internal sealed class CaptureBenefitOrderKey : IComparable<CaptureBenefitOrderKey>
{
    internal CaptureBenefitOrderKey(
        int primary,
        int secondary,
        int tertiary,
        string stableTieBreakId)
    {
        ValidateNonnegative(primary, nameof(primary));
        ValidateNonnegative(secondary, nameof(secondary));
        ValidateNonnegative(tertiary, nameof(tertiary));
        Primary = primary;
        Secondary = secondary;
        Tertiary = tertiary;
        StableTieBreakId = StableDomainId.Validate(
            stableTieBreakId,
            nameof(stableTieBreakId));
    }

    public int Primary { get; }

    public int Secondary { get; }

    public int Tertiary { get; }

    public string StableTieBreakId { get; }

    public int CompareTo(CaptureBenefitOrderKey? other)
    {
        if (other is null)
        {
            return 1;
        }

        var primaryComparison = Primary.CompareTo(other.Primary);
        if (primaryComparison != 0)
        {
            return primaryComparison;
        }

        var secondaryComparison = Secondary.CompareTo(other.Secondary);
        if (secondaryComparison != 0)
        {
            return secondaryComparison;
        }

        var tertiaryComparison = Tertiary.CompareTo(other.Tertiary);
        return tertiaryComparison != 0
            ? tertiaryComparison
            : StringComparer.Ordinal.Compare(StableTieBreakId, other.StableTieBreakId);
    }

    internal string ToCanonicalText() =>
        $"{Primary.ToString(CultureInfo.InvariantCulture)}," +
        $"{Secondary.ToString(CultureInfo.InvariantCulture)}," +
        $"{Tertiary.ToString(CultureInfo.InvariantCulture)}," +
        EncodeStableText(StableTieBreakId);

    private static void ValidateNonnegative(int value, string parameterName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "Capture benefit order components cannot be negative.");
        }
    }

    private static string EncodeStableText(string value) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
}

public enum CaptureBenefitSourceKind : byte
{
    StandardAccounting = 1,
    SourceOrArmedEffect = 2,
    CapturedStoneSelf = 3,
    Style = 4,
    Seal = 5,
    Relic = 6,
    Facility = 7,
    EnemyPassive = 8,
    Sacrifice = 9,
    ScoreOrTelemetry = 10,
}

public sealed class CaptureBenefitSource
{
    private CaptureBenefitSource(
        CaptureBenefitSourceKind kind,
        string sourceId,
        int stableOrder,
        CanonicalPoint? facilityPoint)
    {
        ValidateKind(kind);
        SourceId = StableDomainId.Validate(sourceId, nameof(sourceId));
        if (stableOrder < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(stableOrder),
                stableOrder,
                "Capture benefit source order cannot be negative.");
        }

        Kind = kind;
        StableOrder = stableOrder;
        FacilityPoint = facilityPoint;
    }

    public CaptureBenefitSourceKind Kind { get; }

    public string SourceId { get; }

    public int StableOrder { get; }

    public CanonicalPoint? FacilityPoint { get; }

    internal CaptureBenefitStage Stage => Kind switch
    {
        CaptureBenefitSourceKind.StandardAccounting =>
            CaptureBenefitStage.StandardAccounting,
        CaptureBenefitSourceKind.SourceOrArmedEffect =>
            CaptureBenefitStage.SourceOrArmedEffect,
        CaptureBenefitSourceKind.CapturedStoneSelf =>
            CaptureBenefitStage.CapturedStoneSelf,
        CaptureBenefitSourceKind.Style or CaptureBenefitSourceKind.Seal =>
            CaptureBenefitStage.StyleOrSeal,
        CaptureBenefitSourceKind.Relic => CaptureBenefitStage.Relic,
        CaptureBenefitSourceKind.Facility => CaptureBenefitStage.Facility,
        CaptureBenefitSourceKind.EnemyPassive => CaptureBenefitStage.EnemyPassive,
        CaptureBenefitSourceKind.Sacrifice => CaptureBenefitStage.Sacrifice,
        CaptureBenefitSourceKind.ScoreOrTelemetry =>
            CaptureBenefitStage.ScoreOrTelemetry,
        _ => throw new InvalidOperationException("Unknown capture benefit source kind."),
    };

    public static CaptureBenefitSource StandardAccounting(
        string sourceId,
        int stableOrder) =>
        new(CaptureBenefitSourceKind.StandardAccounting, sourceId, stableOrder, null);

    public static CaptureBenefitSource SourceOrArmedEffect(
        string sourceId,
        int stableOrder) =>
        new(CaptureBenefitSourceKind.SourceOrArmedEffect, sourceId, stableOrder, null);

    public static CaptureBenefitSource CapturedStoneSelf(string stoneInstanceId) =>
        new(CaptureBenefitSourceKind.CapturedStoneSelf, stoneInstanceId, 0, null);

    public static CaptureBenefitSource Style(string styleInstanceId) =>
        new(CaptureBenefitSourceKind.Style, styleInstanceId, 0, null);

    public static CaptureBenefitSource Seal(string sealInstanceId, int equippedSlot) =>
        new(CaptureBenefitSourceKind.Seal, sealInstanceId, equippedSlot, null);

    public static CaptureBenefitSource Relic(string relicInstanceId, int equippedSlot) =>
        new(CaptureBenefitSourceKind.Relic, relicInstanceId, equippedSlot, null);

    public static CaptureBenefitSource Facility(
        string facilityInstanceId,
        CanonicalPoint facilityPoint)
    {
        ArgumentNullException.ThrowIfNull(facilityPoint);
        return new CaptureBenefitSource(
            CaptureBenefitSourceKind.Facility,
            facilityInstanceId,
            0,
            facilityPoint);
    }

    public static CaptureBenefitSource EnemyPassive(string contentId) =>
        new(CaptureBenefitSourceKind.EnemyPassive, contentId, 0, null);

    public static CaptureBenefitSource Sacrifice() =>
        new(CaptureBenefitSourceKind.Sacrifice, "sacrifice_pressure", 0, null);

    public static CaptureBenefitSource ScoreOrTelemetry(
        string sourceId,
        int stableOrder) =>
        new(CaptureBenefitSourceKind.ScoreOrTelemetry, sourceId, stableOrder, null);

    internal BoundCaptureBenefitSource Bind(CaptureBatch captureBatch)
    {
        ArgumentNullException.ThrowIfNull(captureBatch);
        CaptureBenefitOrderKey orderKey;
        switch (Kind)
        {
            case CaptureBenefitSourceKind.CapturedStoneSelf:
                orderKey = BindCapturedStone(captureBatch);
                break;
            case CaptureBenefitSourceKind.Style:
                orderKey = new CaptureBenefitOrderKey(0, 0, 0, SourceId);
                break;
            case CaptureBenefitSourceKind.Seal:
                orderKey = new CaptureBenefitOrderKey(
                    checked(StableOrder + 1),
                    0,
                    0,
                    SourceId);
                break;
            case CaptureBenefitSourceKind.Facility:
                var point = FacilityPoint
                    ?? throw new InvalidOperationException(
                        "Facility capture benefit source is missing its point.");
                orderKey = new CaptureBenefitOrderKey(
                    ((point.Y - 1) * BoardGeometry.AcceptedSize) + (point.X - 1),
                    0,
                    0,
                    SourceId);
                break;
            case CaptureBenefitSourceKind.EnemyPassive:
                orderKey = new CaptureBenefitOrderKey(0, 0, 0, SourceId);
                break;
            default:
                orderKey = new CaptureBenefitOrderKey(
                    StableOrder,
                    0,
                    0,
                    SourceId);
                break;
        }

        return new BoundCaptureBenefitSource(
            orderKey,
            $"{((int)Kind).ToString(CultureInfo.InvariantCulture)}." +
            SourceId);
    }

    internal string ToCanonicalText() => string.Join(
        ':',
        ((int)Kind).ToString(CultureInfo.InvariantCulture),
        EncodeStableText(SourceId),
        StableOrder.ToString(CultureInfo.InvariantCulture),
        FacilityPoint is null
            ? "none"
            : $"{FacilityPoint.X.ToString(CultureInfo.InvariantCulture)}," +
              FacilityPoint.Y.ToString(CultureInfo.InvariantCulture));

    private CaptureBenefitOrderKey BindCapturedStone(CaptureBatch captureBatch)
    {
        for (var groupIndex = 0; groupIndex < captureBatch.CapturedGroups.Count; groupIndex++)
        {
            var group = captureBatch.CapturedGroups[groupIndex];
            for (var stoneIndex = 0; stoneIndex < group.StoneInstances.Count; stoneIndex++)
            {
                if (StringComparer.Ordinal.Equals(
                        group.StoneInstances[stoneIndex].InstanceId,
                        SourceId))
                {
                    return new CaptureBenefitOrderKey(
                        groupIndex,
                        stoneIndex,
                        0,
                        SourceId);
                }
            }
        }

        throw new ArgumentException(
            $"Captured-stone benefit source {SourceId} does not belong to capture batch {captureBatch.BatchId}.",
            nameof(captureBatch));
    }

    private static void ValidateKind(CaptureBenefitSourceKind kind)
    {
        if (kind is < CaptureBenefitSourceKind.StandardAccounting or
            > CaptureBenefitSourceKind.ScoreOrTelemetry)
        {
            throw new ArgumentOutOfRangeException(
                nameof(kind),
                kind,
                "Unknown capture benefit source kind.");
        }
    }

    private static string EncodeStableText(string value) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
}

internal sealed record BoundCaptureBenefitSource(
    CaptureBenefitOrderKey OrderKey,
    string UniqueIdentity);

public abstract class CaptureBenefitOperation
{
    private protected CaptureBenefitOperation()
    {
    }

    internal abstract string ToCanonicalText();
}

public sealed class ReserveDrawCaptureBenefitOperation : CaptureBenefitOperation
{
    public ReserveDrawCaptureBenefitOperation(int amount)
    {
        Amount = ValidatePositive(amount, nameof(amount));
    }

    public int Amount { get; }

    internal override string ToCanonicalText() =>
        $"reserve_draw:{Amount.ToString(CultureInfo.InvariantCulture)}";

    private static int ValidatePositive(int value, string parameterName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "Capture benefit amounts must be positive.");
        }

        return value;
    }
}

public sealed class ReserveQiCaptureBenefitOperation : CaptureBenefitOperation
{
    public ReserveQiCaptureBenefitOperation(int amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                amount,
                "Capture benefit amounts must be positive.");
        }

        Amount = amount;
    }

    public int Amount { get; }

    internal override string ToCanonicalText() =>
        $"reserve_qi:{Amount.ToString(CultureInfo.InvariantCulture)}";
}

public sealed class GainSoulCaptureBenefitOperation : CaptureBenefitOperation
{
    public GainSoulCaptureBenefitOperation(int amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                amount,
                "Capture benefit amounts must be positive.");
        }

        Amount = amount;
    }

    public int Amount { get; }

    internal override string ToCanonicalText() =>
        $"gain_soul:{Amount.ToString(CultureInfo.InvariantCulture)}";
}

public sealed class GainStandardCaptureSoulOperation : CaptureBenefitOperation
{
    public GainStandardCaptureSoulOperation(
        int soulPerCapturedGroup,
        int capturedWhiteGroupCount,
        int battleRewardLimit)
    {
        SoulPerCapturedGroup = ValidatePositive(
            soulPerCapturedGroup,
            nameof(soulPerCapturedGroup));
        CapturedWhiteGroupCount = ValidatePositive(
            capturedWhiteGroupCount,
            nameof(capturedWhiteGroupCount));
        BattleRewardLimit = ValidatePositive(
            battleRewardLimit,
            nameof(battleRewardLimit));
    }

    public int SoulPerCapturedGroup { get; }

    public int CapturedWhiteGroupCount { get; }

    public int BattleRewardLimit { get; }

    internal override string ToCanonicalText() =>
        $"gain_standard_capture_soul:" +
        $"{SoulPerCapturedGroup.ToString(CultureInfo.InvariantCulture)}:" +
        $"{CapturedWhiteGroupCount.ToString(CultureInfo.InvariantCulture)}:" +
        BattleRewardLimit.ToString(CultureInfo.InvariantCulture);

    private static int ValidatePositive(int value, string parameterName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "Standard capture reward values must be positive.");
        }

        return value;
    }
}

public sealed class CreateDeferredChoiceCaptureBenefitOperation : CaptureBenefitOperation
{
    public CreateDeferredChoiceCaptureBenefitOperation(
        string sourceId,
        string choiceId)
    {
        SourceId = StableDomainId.Validate(sourceId, nameof(sourceId));
        ChoiceId = StableDomainId.Validate(choiceId, nameof(choiceId));
    }

    public string SourceId { get; }

    public string ChoiceId { get; }

    internal override string ToCanonicalText() =>
        $"defer_choice:{EncodeStableText(SourceId)}:{EncodeStableText(ChoiceId)}";

    private static string EncodeStableText(string value) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
}

public sealed class AdvanceSacrificePressureCaptureBenefitOperation : CaptureBenefitOperation
{
    public AdvanceSacrificePressureCaptureBenefitOperation()
    {
    }

    internal override string ToCanonicalText() => "advance_sacrifice_pressure";
}

public sealed class CaptureBenefitTrigger
{
    private readonly ReadOnlyCollection<string> eventPathView;
    private readonly ReadOnlyCollection<CaptureBenefitOperation> operationView;

    public CaptureBenefitTrigger(
        CaptureBenefitSource source,
        string triggerId,
        IEnumerable<string> eventPath,
        IEnumerable<CaptureBenefitOperation> orderedOperations,
        string? firstUseFlagId)
    {
        ArgumentNullException.ThrowIfNull(source);
        TriggerId = StableDomainId.Validate(triggerId, nameof(triggerId));
        ArgumentNullException.ThrowIfNull(eventPath);
        ArgumentNullException.ThrowIfNull(orderedOperations);

        var canonicalEventPath = eventPath
            .Select(component => StableDomainId.Validate(component, nameof(eventPath)))
            .ToArray();
        if (canonicalEventPath.Length == 0)
        {
            throw new ArgumentException(
                "Capture benefit triggers require a non-empty event path.",
                nameof(eventPath));
        }

        var operations = orderedOperations.ToArray();
        if (operations.Length == 0)
        {
            throw new ArgumentException(
                "Capture benefit triggers require at least one typed operation.",
                nameof(orderedOperations));
        }

        foreach (var operation in operations)
        {
            ArgumentNullException.ThrowIfNull(operation);
        }

        var standardRewardOperationCount = operations.Count(operation =>
            operation is GainStandardCaptureSoulOperation);
        var uncappedSoulOperationCount = operations.Count(operation =>
            operation is GainSoulCaptureBenefitOperation);
        if (standardRewardOperationCount > 0 &&
            source.Kind != CaptureBenefitSourceKind.StandardAccounting)
        {
            throw new ArgumentException(
                "Standard capture reward operations belong only to standard accounting.",
                nameof(orderedOperations));
        }

        if (source.Kind == CaptureBenefitSourceKind.StandardAccounting &&
            uncappedSoulOperationCount > 0)
        {
            throw new ArgumentException(
                "Standard accounting cannot bypass its battle cap with uncapped Soul operations.",
                nameof(orderedOperations));
        }

        var sacrificeOperationCount = operations
            .Count(operation =>
                operation is AdvanceSacrificePressureCaptureBenefitOperation);
        if (source.Kind == CaptureBenefitSourceKind.Sacrifice)
        {
            if (operations.Length != 1 || sacrificeOperationCount != 1)
            {
                throw new ArgumentException(
                    "The sacrifice stage requires exactly one sacrifice-pressure operation.",
                    nameof(orderedOperations));
            }

            if (firstUseFlagId is not null)
            {
                throw new ArgumentException(
                    "Persistent sacrifice pressure cannot be guarded by a first-use flag.",
                    nameof(firstUseFlagId));
            }
        }
        else if (sacrificeOperationCount != 0)
        {
            throw new ArgumentException(
                "Sacrifice-pressure operations belong only to the sacrifice stage.",
                nameof(orderedOperations));
        }

        Source = source;
        eventPathView = Array.AsReadOnly(canonicalEventPath);
        operationView = Array.AsReadOnly((CaptureBenefitOperation[])operations.Clone());
        FirstUseFlagId = firstUseFlagId is null
            ? null
            : StableDomainId.Validate(firstUseFlagId, nameof(firstUseFlagId));
    }

    public CaptureBenefitSource Source { get; }

    public CaptureBenefitStage Stage => Source.Stage;

    public string TriggerId { get; }

    public IReadOnlyList<string> EventPath => eventPathView;

    public IReadOnlyList<CaptureBenefitOperation> OrderedOperations => operationView;

    public string? FirstUseFlagId { get; }

    internal string EventPrefix => string.Join(':', eventPathView);

    internal string ToCanonicalText() => string.Join(
        '|',
        Source.ToCanonicalText(),
        EncodeStableText(TriggerId),
        string.Join(',', eventPathView.Select(EncodeStableText)),
        FirstUseFlagId is null ? "none" : EncodeStableText(FirstUseFlagId),
        string.Join(',', operationView.Select(operation => operation.ToCanonicalText())));

    internal static int StageRank(CaptureBenefitStage stage) => stage switch
    {
        CaptureBenefitStage.StandardAccounting => 1,
        CaptureBenefitStage.SourceOrArmedEffect => 2,
        CaptureBenefitStage.CapturedStoneSelf => 3,
        CaptureBenefitStage.StyleOrSeal => 4,
        CaptureBenefitStage.Relic => 5,
        CaptureBenefitStage.Facility => 6,
        CaptureBenefitStage.EnemyPassive => 7,
        CaptureBenefitStage.Sacrifice => 8,
        CaptureBenefitStage.ScoreOrTelemetry => 9,
        _ => throw new ArgumentOutOfRangeException(nameof(stage), stage, "Unknown benefit stage."),
    };

    private static string EncodeStableText(string value) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
}

public interface ICaptureBenefitAppliedFact : IBattleFact
{
    string TriggerId { get; }

    string EventId { get; }
}

public sealed class CaptureBatchStartedFact : IBattleFact
{
    internal CaptureBatchStartedFact(CaptureBatch captureBatch)
    {
        CaptureBatch = captureBatch;
    }

    public CaptureBatch CaptureBatch { get; }
}

public sealed class TurnReservedDrawChangedFact : ICaptureBenefitAppliedFact
{
    internal TurnReservedDrawChangedFact(
        string triggerId,
        string eventId,
        int amountBefore,
        int amountAfter,
        int delta)
    {
        TriggerId = triggerId;
        EventId = eventId;
        AmountBefore = amountBefore;
        AmountAfter = amountAfter;
        Delta = delta;
    }

    public string TriggerId { get; }

    public string EventId { get; }

    public int AmountBefore { get; }

    public int AmountAfter { get; }

    public int Delta { get; }
}

public sealed class TurnReservedQiChangedFact : ICaptureBenefitAppliedFact
{
    internal TurnReservedQiChangedFact(
        string triggerId,
        string eventId,
        int amountBefore,
        int amountAfter,
        int delta)
    {
        TriggerId = triggerId;
        EventId = eventId;
        AmountBefore = amountBefore;
        AmountAfter = amountAfter;
        Delta = delta;
    }

    public string TriggerId { get; }

    public string EventId { get; }

    public int AmountBefore { get; }

    public int AmountAfter { get; }

    public int Delta { get; }
}

public sealed class SoulChangedFact : ICaptureBenefitAppliedFact
{
    internal SoulChangedFact(
        string triggerId,
        string eventId,
        int amountBefore,
        int amountAfter,
        int delta)
    {
        TriggerId = triggerId;
        EventId = eventId;
        AmountBefore = amountBefore;
        AmountAfter = amountAfter;
        Delta = delta;
    }

    public string TriggerId { get; }

    public string EventId { get; }

    public int AmountBefore { get; }

    public int AmountAfter { get; }

    public int Delta { get; }
}

public sealed class DeferredPlayerChoiceCreatedFact : ICaptureBenefitAppliedFact
{
    internal DeferredPlayerChoiceCreatedFact(
        string triggerId,
        string eventId,
        DeferredPlayerChoice choice)
    {
        TriggerId = triggerId;
        EventId = eventId;
        Choice = choice;
    }

    public string TriggerId { get; }

    public string EventId { get; }

    public DeferredPlayerChoice Choice { get; }
}

public sealed class FirstUseFlagConsumedFact : IBattleFact
{
    internal FirstUseFlagConsumedFact(string triggerId, string flagId)
    {
        TriggerId = triggerId;
        FlagId = flagId;
    }

    public string TriggerId { get; }

    public string FlagId { get; }
}

public sealed class CaptureBatchResolvedFact : IBattleFact
{
    internal CaptureBatchResolvedFact(string batchId, bool benefitsSuppressed)
    {
        BatchId = batchId;
        BenefitsSuppressed = benefitsSuppressed;
    }

    public string BatchId { get; }

    public bool BenefitsSuppressed { get; }
}

public sealed class ClosedWindowCaptureBenefitResolution
{
    public const string EncodingVersion = "closed-window-capture-benefit-resolution-v1";

    private readonly ReadOnlyCollection<IBattleFact> orderedFactView;
    private readonly ReadOnlyCollection<CaptureBenefitTrigger> orderedTriggerView;

    internal ClosedWindowCaptureBenefitResolution(
        CaptureBatch captureBatch,
        ClosedWindowResourceState sourceResources,
        CounterattackBoundaryState sourceCounterattack,
        CounterattackBoundaryPolicy policy,
        ClosedWindowResourceState resourcesAfterResolution,
        CounterattackBoundaryState counterattackAfterResolution,
        bool benefitsSuppressed,
        CaptureBenefitTrigger[] orderedTriggers,
        IBattleFact[] orderedFacts)
    {
        CaptureBatch = captureBatch;
        SourceResources = sourceResources;
        SourceCounterattack = sourceCounterattack;
        Policy = policy;
        ResourcesAfterResolution = resourcesAfterResolution;
        CounterattackAfterResolution = counterattackAfterResolution;
        BenefitsSuppressed = benefitsSuppressed;
        orderedTriggerView = Array.AsReadOnly(
            (CaptureBenefitTrigger[])orderedTriggers.Clone());
        orderedFactView = Array.AsReadOnly((IBattleFact[])orderedFacts.Clone());
        CanonicalText = CreateCanonicalText();
        Checksum = DeterministicChecksum.Sha256Hex(CanonicalText);
    }

    public CaptureBatch CaptureBatch { get; }

    public ClosedWindowResourceState SourceResources { get; }

    public CounterattackBoundaryState SourceCounterattack { get; }

    public CounterattackBoundaryPolicy Policy { get; }

    public ClosedWindowResourceState ResourcesAfterResolution { get; }

    public CounterattackBoundaryState CounterattackAfterResolution { get; }

    public bool BenefitsSuppressed { get; }

    public IReadOnlyList<CaptureBenefitTrigger> OrderedTriggers => orderedTriggerView;

    public IReadOnlyList<IBattleFact> OrderedFacts => orderedFactView;

    public string CanonicalText { get; }

    public string Checksum { get; }

    private string CreateCanonicalText()
    {
        var lines = new List<string>
        {
            EncodingVersion,
            $"capture_batch_checksum={DeterministicChecksum.Sha256Hex(CaptureBatch.CanonicalText)}",
            $"source_resource_checksum={DeterministicChecksum.Sha256Hex(SourceResources.ToCanonicalText())}",
            $"source_counterattack_checksum={DeterministicChecksum.Sha256Hex(SourceCounterattack.ToCanonicalText())}",
            $"policy_checksum={DeterministicChecksum.Sha256Hex(Policy.ToCanonicalText())}",
            $"result_resource_checksum={DeterministicChecksum.Sha256Hex(ResourcesAfterResolution.ToCanonicalText())}",
            $"result_counterattack_checksum={DeterministicChecksum.Sha256Hex(CounterattackAfterResolution.ToCanonicalText())}",
            $"benefits_suppressed={(BenefitsSuppressed ? "1" : "0")}",
            $"trigger_count={orderedTriggerView.Count.ToString(CultureInfo.InvariantCulture)}",
            $"fact_count={orderedFactView.Count.ToString(CultureInfo.InvariantCulture)}",
        };
        lines.AddRange(orderedTriggerView.Select(trigger =>
            $"trigger={EncodeStableText(trigger.ToCanonicalText())}"));
        lines.AddRange(orderedFactView.Select(fact => $"fact={ProjectFact(fact)}"));
        return string.Join('\n', lines);
    }

    private static string ProjectFact(IBattleFact fact) => fact switch
    {
        CaptureBatchStartedFact started =>
            $"batch_started:{EncodeStableText(started.CaptureBatch.BatchId)}",
        TurnReservedDrawChangedFact draw =>
            $"reserved_draw:{EncodeStableText(draw.TriggerId)}:{EncodeEventText(draw.EventId)}:{Invariant(draw.AmountBefore)}:{Invariant(draw.AmountAfter)}:{Invariant(draw.Delta)}",
        TurnReservedQiChangedFact qi =>
            $"reserved_qi:{EncodeStableText(qi.TriggerId)}:{EncodeEventText(qi.EventId)}:{Invariant(qi.AmountBefore)}:{Invariant(qi.AmountAfter)}:{Invariant(qi.Delta)}",
        SoulChangedFact soul =>
            $"soul:{EncodeStableText(soul.TriggerId)}:{EncodeEventText(soul.EventId)}:{Invariant(soul.AmountBefore)}:{Invariant(soul.AmountAfter)}:{Invariant(soul.Delta)}",
        DeferredPlayerChoiceCreatedFact choice =>
            $"deferred_choice:{EncodeStableText(choice.TriggerId)}:{EncodeEventText(choice.EventId)}:" +
            $"{EncodeStableText(choice.Choice.SourceId)}:" +
            $"{EncodeStableText(choice.Choice.ChoiceId)}:" +
            $"{choice.Choice.CreatedSequence.ToString(CultureInfo.InvariantCulture)}",
        FirstUseFlagConsumedFact firstUse =>
            $"first_use:{EncodeStableText(firstUse.TriggerId)}:{EncodeStableText(firstUse.FlagId)}",
        SacrificeRemainderChangedFact remainder =>
            $"sacrifice_remainder:{Invariant(remainder.CapturedNonKingBlackStoneCount)}:" +
            $"{Invariant(remainder.RemainderBefore)}:{Invariant(remainder.RemainderAfter)}",
        SacrificeBatchAdvancedFact sacrifice =>
            $"sacrifice:{Invariant(sacrifice.CapturedNonKingBlackStoneCount)}:" +
            $"{Invariant(sacrifice.CompletedBatchCount)}:{Invariant(sacrifice.RemainderBefore)}:" +
            $"{Invariant(sacrifice.RemainderAfter)}:{Invariant(sacrifice.DeltaUnits)}",
        CounterattackAdvancedFact advanced =>
            $"counterattack:{advanced.ReasonId}:{Invariant(advanced.GaugeUnitsBefore)}:" +
            $"{Invariant(advanced.UnitsAfterGainBeforePrime)}:{Invariant(advanced.GaugeUnitsAfter)}:" +
            $"{Invariant(advanced.DeltaUnits)}:{(advanced.PendingBefore ? "1" : "0")}:" +
            $"{(advanced.PendingAfter ? "1" : "0")}",
        CounterattackPendingPrimedFact primed =>
            $"counterattack_primed:{Invariant((int)primed.Reason)}:{Invariant(primed.ResidualGaugeUnits)}",
        CaptureBenefitSuppressedFact suppressed =>
            $"benefit_suppressed:{EncodeStableText(suppressed.ReasonId)}",
        CaptureBatchResolvedFact resolved =>
            $"batch_resolved:{EncodeStableText(resolved.BatchId)}:" +
            $"{(resolved.BenefitsSuppressed ? "1" : "0")}",
        _ => throw new InvalidOperationException(
            $"Unhandled closed-window capture benefit fact {fact.GetType().FullName}."),
    };

    private static string EncodeStableText(string value) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(value));

    private static string EncodeEventText(string value) => EncodeStableText(value);

    private static string Invariant(int value) =>
        value.ToString(CultureInfo.InvariantCulture);
}

public static class ClosedWindowCaptureBenefitResolver
{
    public static ClosedWindowCaptureBenefitResolution ResolvePlacement(
        CaptureBatch captureBatch,
        ClosedWindowResourceState sourceResources,
        CounterattackBoundaryState sourceCounterattack,
        CounterattackBoundaryPolicy policy,
        IEnumerable<CaptureBenefitTrigger> triggers)
    {
        ArgumentNullException.ThrowIfNull(captureBatch);
        ArgumentNullException.ThrowIfNull(sourceResources);
        ArgumentNullException.ThrowIfNull(sourceCounterattack);
        ArgumentNullException.ThrowIfNull(policy);
        if (captureBatch.Boundary != CaptureBoundary.PlacementResolution)
        {
            throw new ArgumentException(
                "Placement capture benefit resolution requires a placement capture batch.",
                nameof(captureBatch));
        }

        if (captureBatch.ContainsKing)
        {
            return new ClosedWindowCaptureBenefitResolution(
                captureBatch,
                sourceResources,
                sourceCounterattack,
                policy,
                sourceResources,
                sourceCounterattack,
                true,
                [],
                [
                    new CaptureBatchStartedFact(captureBatch),
                    new CaptureBenefitSuppressedFact("terminal_king_capture"),
                    new CaptureBatchResolvedFact(captureBatch.BatchId, true),
                ]);
        }

        return Resolve(
            captureBatch,
            sourceResources,
            sourceCounterattack,
            policy,
            triggers);
    }

    public static ClosedWindowCaptureBenefitResolution Resolve(
        CaptureBatch captureBatch,
        ClosedWindowResourceState sourceResources,
        CounterattackBoundaryState sourceCounterattack,
        CounterattackBoundaryPolicy policy,
        IEnumerable<CaptureBenefitTrigger> triggers)
    {
        ArgumentNullException.ThrowIfNull(captureBatch);
        ArgumentNullException.ThrowIfNull(sourceResources);
        ArgumentNullException.ThrowIfNull(sourceCounterattack);
        ArgumentNullException.ThrowIfNull(policy);
        if (captureBatch.CapturingWindow != CapturingWindow.ClosedPlayerWindow)
        {
            throw new ArgumentException(
                "Closed-window capture benefits require a closed-player-window batch.",
                nameof(captureBatch));
        }

        var orderedFacts = new List<IBattleFact>
        {
            new CaptureBatchStartedFact(captureBatch),
        };
        if (captureBatch.ContainsKing)
        {
            orderedFacts.Add(new CaptureBatchResolvedFact(captureBatch.BatchId, true));
            return new ClosedWindowCaptureBenefitResolution(
                captureBatch,
                sourceResources,
                sourceCounterattack,
                policy,
                sourceResources,
                sourceCounterattack,
                true,
                [],
                orderedFacts.ToArray());
        }

        ArgumentNullException.ThrowIfNull(triggers);
        CounterattackBoundaryResolver.ValidateStateAgainstPolicy(
            sourceCounterattack,
            policy);
        var canonicalTriggers = triggers.ToArray();
        foreach (var trigger in canonicalTriggers)
        {
            ArgumentNullException.ThrowIfNull(trigger);
        }

        var boundTriggers = canonicalTriggers
            .Select(trigger => new BoundCaptureBenefitTrigger(
                trigger,
                trigger.Source.Bind(captureBatch)))
            .ToArray();
        Array.Sort(boundTriggers, CompareTriggers);
        if (canonicalTriggers
            .GroupBy(trigger => trigger.TriggerId, StringComparer.Ordinal)
            .Any(group => group.Count() > 1))
        {
            throw new ArgumentException(
                "Capture benefit trigger IDs must be unique within a batch.",
                nameof(triggers));
        }
        if (boundTriggers
            .GroupBy(bound => bound.Source.UniqueIdentity, StringComparer.Ordinal)
            .Any(group => group.Count() > 1))
        {
            throw new ArgumentException(
                "Capture benefit source identities must be unique within a batch.",
                nameof(triggers));
        }

        if (boundTriggers
            .GroupBy(bound => bound.Trigger.EventPrefix, StringComparer.Ordinal)
            .Any(group => group.Count() > 1))
        {
            throw new ArgumentException(
                "Capture benefit event paths must be unique within a batch.",
                nameof(triggers));
        }

        if (boundTriggers
            .Where(bound => bound.Trigger.FirstUseFlagId is not null)
            .GroupBy(
                bound => bound.Trigger.FirstUseFlagId!,
                StringComparer.Ordinal)
            .Any(group => group.Count() > 1))
        {
            throw new ArgumentException(
                "First-use flag IDs must be unique among capture benefit sources.",
                nameof(triggers));
        }

        var resources = sourceResources;
        var counterattack = sourceCounterattack;
        foreach (var boundTrigger in boundTriggers)
        {
            var trigger = boundTrigger.Trigger;
            if (trigger.FirstUseFlagId is not null &&
                resources.IsFirstUseConsumed(trigger.FirstUseFlagId))
            {
                continue;
            }

            foreach (var operation in trigger.OrderedOperations)
            {
                switch (operation)
                {
                    case ReserveDrawCaptureBenefitOperation draw:
                    {
                        var before = resources.TurnReservedDraw;
                        resources = resources.AddReservedDraw(draw.Amount);
                        orderedFacts.Add(new TurnReservedDrawChangedFact(
                            trigger.TriggerId,
                            EventId(trigger, $"reserve_draw_{draw.Amount.ToString(CultureInfo.InvariantCulture)}"),
                            before,
                            resources.TurnReservedDraw,
                            draw.Amount));
                        break;
                    }
                    case ReserveQiCaptureBenefitOperation qi:
                    {
                        var before = resources.TurnReservedQi;
                        resources = resources.AddReservedQi(qi.Amount);
                        orderedFacts.Add(new TurnReservedQiChangedFact(
                            trigger.TriggerId,
                            EventId(trigger, $"reserve_qi_{qi.Amount.ToString(CultureInfo.InvariantCulture)}"),
                            before,
                            resources.TurnReservedQi,
                            qi.Amount));
                        break;
                    }
                    case GainSoulCaptureBenefitOperation soul:
                    {
                        var before = resources.Soul;
                        resources = resources.AddSoul(soul.Amount);
                        orderedFacts.Add(new SoulChangedFact(
                            trigger.TriggerId,
                            EventId(trigger, $"soul_{soul.Amount.ToString(CultureInfo.InvariantCulture)}"),
                            before,
                            resources.Soul,
                            soul.Amount));
                        break;
                    }
                    case GainStandardCaptureSoulOperation standardSoul:
                    {
                        var remainingRewards = Math.Max(
                            0,
                            standardSoul.BattleRewardLimit -
                                resources.StandardCaptureRewardsClaimed);
                        var appliedRewardCount = Math.Min(
                            standardSoul.CapturedWhiteGroupCount,
                            remainingRewards);
                        if (appliedRewardCount == 0)
                        {
                            break;
                        }

                        var soulAmount = checked(
                            appliedRewardCount * standardSoul.SoulPerCapturedGroup);
                        var before = resources.Soul;
                        resources = resources.AddStandardCaptureSoul(
                            appliedRewardCount,
                            standardSoul.SoulPerCapturedGroup);
                        orderedFacts.Add(new SoulChangedFact(
                            trigger.TriggerId,
                            EventId(
                                trigger,
                                $"soul_{soulAmount.ToString(CultureInfo.InvariantCulture)}"),
                            before,
                            resources.Soul,
                            soulAmount));
                        break;
                    }
                    case CreateDeferredChoiceCaptureBenefitOperation deferred:
                    {
                        var addition = resources.AddDeferredChoice(
                            deferred.SourceId,
                            deferred.ChoiceId);
                        resources = addition.State;
                        orderedFacts.Add(new DeferredPlayerChoiceCreatedFact(
                            trigger.TriggerId,
                            EventId(trigger, "deferred_choice"),
                            addition.Choice));
                        break;
                    }
                    case AdvanceSacrificePressureCaptureBenefitOperation:
                    {
                        var transition = CounterattackBoundaryResolver.AdvanceSacrifice(
                            counterattack,
                            captureBatch,
                            policy);
                        counterattack = transition.StateAfterTransition;
                        orderedFacts.AddRange(transition.OrderedFacts);
                        break;
                    }
                    default:
                        throw new InvalidOperationException(
                            $"Unhandled capture benefit operation {operation.GetType().FullName}.");
                }
            }

            if (trigger.FirstUseFlagId is not null)
            {
                resources = resources.ConsumeFirstUse(trigger.FirstUseFlagId);
                orderedFacts.Add(new FirstUseFlagConsumedFact(
                    trigger.TriggerId,
                    trigger.FirstUseFlagId));
            }
        }

        orderedFacts.Add(new CaptureBatchResolvedFact(captureBatch.BatchId, false));
        return new ClosedWindowCaptureBenefitResolution(
            captureBatch,
            sourceResources,
            sourceCounterattack,
            policy,
            resources,
            counterattack,
            false,
            boundTriggers.Select(bound => bound.Trigger).ToArray(),
            orderedFacts.ToArray());
    }

    private static int CompareTriggers(
        BoundCaptureBenefitTrigger left,
        BoundCaptureBenefitTrigger right)
    {
        var stageComparison = CaptureBenefitTrigger.StageRank(left.Trigger.Stage)
            .CompareTo(CaptureBenefitTrigger.StageRank(right.Trigger.Stage));
        if (stageComparison != 0)
        {
            return stageComparison;
        }

        var orderComparison = left.Source.OrderKey.CompareTo(right.Source.OrderKey);
        return orderComparison != 0
            ? orderComparison
            : StringComparer.Ordinal.Compare(
                left.Trigger.TriggerId,
                right.Trigger.TriggerId);
    }

    private static string EventId(CaptureBenefitTrigger trigger, string suffix) =>
        $"{trigger.EventPrefix}:{suffix}";
}

internal sealed record BoundCaptureBenefitTrigger(
    CaptureBenefitTrigger Trigger,
    BoundCaptureBenefitSource Source);
