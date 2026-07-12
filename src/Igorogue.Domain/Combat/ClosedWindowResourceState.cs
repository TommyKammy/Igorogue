using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

namespace Igorogue.Domain.Combat;

public sealed class DeferredPlayerChoice
{
    public DeferredPlayerChoice(
        string sourceId,
        string choiceId,
        long createdSequence)
    {
        SourceId = StableDomainId.Validate(sourceId, nameof(sourceId));
        ChoiceId = StableDomainId.Validate(choiceId, nameof(choiceId));
        if (createdSequence <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(createdSequence),
                createdSequence,
                "Deferred player choice sequence must be positive.");
        }

        CreatedSequence = createdSequence;
    }

    public string SourceId { get; }

    public string ChoiceId { get; }

    public string Id => $"{SourceId}:{ChoiceId}";

    public long CreatedSequence { get; }
}

public sealed class ClosedWindowResourceState
{
    public const string EncodingVersion = "closed-window-resource-state-v1";

    private readonly ReadOnlyCollection<DeferredPlayerChoice> deferredChoiceView;
    private readonly ReadOnlyDictionary<string, bool> firstUseFlagView;

    private ClosedWindowResourceState(
        int turnReservedDraw,
        int turnReservedQi,
        int soul,
        DeferredPlayerChoice[] deferredChoices,
        SortedDictionary<string, bool> firstUseFlags,
        long nextDeferredChoiceSequence)
    {
        TurnReservedDraw = turnReservedDraw;
        TurnReservedQi = turnReservedQi;
        Soul = soul;
        deferredChoiceView = Array.AsReadOnly(
            (DeferredPlayerChoice[])deferredChoices.Clone());
        firstUseFlagView = new ReadOnlyDictionary<string, bool>(
            new SortedDictionary<string, bool>(firstUseFlags, StringComparer.Ordinal));
        NextDeferredChoiceSequence = nextDeferredChoiceSequence;
    }

    public int TurnReservedDraw { get; }

    public int TurnReservedQi { get; }

    public int Soul { get; }

    public IReadOnlyList<DeferredPlayerChoice> DeferredPlayerChoices => deferredChoiceView;

    public IReadOnlyDictionary<string, bool> FirstUseFlags => firstUseFlagView;

    public long NextDeferredChoiceSequence { get; }

    public static ClosedWindowResourceState Create(
        int turnReservedDraw,
        int turnReservedQi,
        int soul,
        IEnumerable<DeferredPlayerChoice> deferredChoices,
        IEnumerable<KeyValuePair<string, bool>> firstUseFlags,
        long nextDeferredChoiceSequence)
    {
        ValidateNonnegative(turnReservedDraw, nameof(turnReservedDraw));
        ValidateNonnegative(turnReservedQi, nameof(turnReservedQi));
        ValidateNonnegative(soul, nameof(soul));
        ArgumentNullException.ThrowIfNull(deferredChoices);
        ArgumentNullException.ThrowIfNull(firstUseFlags);
        if (nextDeferredChoiceSequence <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(nextDeferredChoiceSequence),
                nextDeferredChoiceSequence,
                "Next deferred player choice sequence must be positive.");
        }

        var canonicalChoices = deferredChoices.ToArray();
        foreach (var choice in canonicalChoices)
        {
            ArgumentNullException.ThrowIfNull(choice);
        }

        Array.Sort(
            canonicalChoices,
            (left, right) =>
            {
                var sequenceComparison = left.CreatedSequence.CompareTo(right.CreatedSequence);
                if (sequenceComparison != 0)
                {
                    return sequenceComparison;
                }

                var sourceComparison = StringComparer.Ordinal.Compare(
                    left.SourceId,
                    right.SourceId);
                return sourceComparison != 0
                    ? sourceComparison
                    : StringComparer.Ordinal.Compare(left.ChoiceId, right.ChoiceId);
            });
        if (canonicalChoices
            .GroupBy(choice => choice.CreatedSequence)
            .Any(group => group.Count() > 1))
        {
            throw new ArgumentException(
                "Deferred player choice sequences must be unique.",
                nameof(deferredChoices));
        }

        if (canonicalChoices.Length > 0 &&
            nextDeferredChoiceSequence <= canonicalChoices[^1].CreatedSequence)
        {
            throw new ArgumentOutOfRangeException(
                nameof(nextDeferredChoiceSequence),
                nextDeferredChoiceSequence,
                "Next deferred player choice sequence must exceed every existing choice sequence.");
        }

        var canonicalFlags = new SortedDictionary<string, bool>(StringComparer.Ordinal);
        foreach (var pair in firstUseFlags)
        {
            var flagId = StableDomainId.Validate(pair.Key, nameof(firstUseFlags));
            if (!canonicalFlags.TryAdd(flagId, pair.Value))
            {
                throw new ArgumentException(
                    $"Closed-window resource state contains duplicate first-use flag {flagId}.",
                    nameof(firstUseFlags));
            }
        }

        return new ClosedWindowResourceState(
            turnReservedDraw,
            turnReservedQi,
            soul,
            canonicalChoices,
            canonicalFlags,
            nextDeferredChoiceSequence);
    }

    public static ClosedWindowResourceState Empty(
        IEnumerable<KeyValuePair<string, bool>> firstUseFlags) =>
        Create(0, 0, 0, [], firstUseFlags, 1);

    public bool IsFirstUseConsumed(string flagId)
    {
        var stableFlagId = StableDomainId.Validate(flagId, nameof(flagId));
        return firstUseFlagView.TryGetValue(stableFlagId, out var consumed)
            ? consumed
            : throw new KeyNotFoundException(
                $"Closed-window resource state does not declare first-use flag {stableFlagId}.");
    }

    public string ToCanonicalText()
    {
        var lines = new List<string>
        {
            EncodingVersion,
            $"turn_reserved_draw={TurnReservedDraw.ToString(CultureInfo.InvariantCulture)}",
            $"turn_reserved_qi={TurnReservedQi.ToString(CultureInfo.InvariantCulture)}",
            $"soul={Soul.ToString(CultureInfo.InvariantCulture)}",
            $"next_deferred_choice_sequence={NextDeferredChoiceSequence.ToString(CultureInfo.InvariantCulture)}",
            $"deferred_choice_count={deferredChoiceView.Count.ToString(CultureInfo.InvariantCulture)}",
        };

        foreach (var choice in deferredChoiceView)
        {
            lines.Add(
                $"deferred_choice={EncodeStableText(choice.SourceId)}:" +
                $"{EncodeStableText(choice.ChoiceId)}:" +
                $"{choice.CreatedSequence.ToString(CultureInfo.InvariantCulture)}");
        }

        lines.Add(
            $"first_use_flag_count={firstUseFlagView.Count.ToString(CultureInfo.InvariantCulture)}");
        foreach (var pair in firstUseFlagView)
        {
            lines.Add(
                $"first_use_flag={EncodeStableText(pair.Key)}:{(pair.Value ? "1" : "0")}");
        }

        return string.Join('\n', lines);
    }

    internal ClosedWindowResourceState AddReservedDraw(int amount) =>
        Recreate(turnReservedDraw: checked(TurnReservedDraw + amount));

    internal ClosedWindowResourceState AddReservedQi(int amount) =>
        Recreate(turnReservedQi: checked(TurnReservedQi + amount));

    internal ClosedWindowResourceState AddSoul(int amount) =>
        Recreate(soul: checked(Soul + amount));

    internal (ClosedWindowResourceState State, DeferredPlayerChoice Choice)
        AddDeferredChoice(string sourceId, string choiceId)
    {
        var choice = new DeferredPlayerChoice(
            sourceId,
            choiceId,
            NextDeferredChoiceSequence);
        var choices = deferredChoiceView.Append(choice).ToArray();
        var state = new ClosedWindowResourceState(
            TurnReservedDraw,
            TurnReservedQi,
            Soul,
            choices,
            CopyFirstUseFlags(),
            checked(NextDeferredChoiceSequence + 1));
        return (state, choice);
    }

    internal ClosedWindowResourceState ConsumeFirstUse(string flagId)
    {
        var stableFlagId = StableDomainId.Validate(flagId, nameof(flagId));
        var flags = CopyFirstUseFlags();
        if (!flags.ContainsKey(stableFlagId))
        {
            throw new KeyNotFoundException(
                $"Closed-window resource state does not declare first-use flag {stableFlagId}.");
        }

        flags[stableFlagId] = true;
        return new ClosedWindowResourceState(
            TurnReservedDraw,
            TurnReservedQi,
            Soul,
            deferredChoiceView.ToArray(),
            flags,
            NextDeferredChoiceSequence);
    }

    private ClosedWindowResourceState Recreate(
        int? turnReservedDraw = null,
        int? turnReservedQi = null,
        int? soul = null) =>
        new(
            turnReservedDraw ?? TurnReservedDraw,
            turnReservedQi ?? TurnReservedQi,
            soul ?? Soul,
            deferredChoiceView.ToArray(),
            CopyFirstUseFlags(),
            NextDeferredChoiceSequence);

    private SortedDictionary<string, bool> CopyFirstUseFlags() =>
        new(firstUseFlagView, StringComparer.Ordinal);

    private static void ValidateNonnegative(int value, string parameterName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "Closed-window resources cannot be negative.");
        }
    }

    private static string EncodeStableText(string value) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
}
