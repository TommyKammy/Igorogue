using System.Text.Json;
using System.Text.RegularExpressions;

using Igorogue.Domain.Board;
using Igorogue.Domain.Combat;

namespace Igorogue.Domain.Tests;

internal static partial class TemporaryLibertyCaptureBenefitFixtureData
{
    private const int SacrificeStyleFirstCaptureReservedDraw = 2;

    private static readonly Lazy<CaptureFixtureContentPolicy> ContentPolicy =
        new(LoadContentPolicy);

    internal static TemporaryLibertyCaptureBenefitFixtureExecution Execute(
        TemporaryLibertyFixture fixture,
        bool reverseEnumeration = false)
    {
        var expiryExecution = TemporaryLibertyFixtureData.Execute(
            fixture,
            reverseEnumeration);
        var captureBatch = expiryExecution.Resolution.CaptureBatch
            ?? throw new InvalidDataException(
                $"Fixture {fixture.Id} did not produce a capture batch.");
        var policy = LoadCounterattackPolicy(fixture.Komi);
        var resources = CreateResourceState(fixture, captureBatch);
        var counterattack = CounterattackBoundaryState.Create(
            fixture.StartCounterattackUnits ?? 0,
            pending: false,
            fixture.StartSacrificeRemainder,
            policy);
        var triggers = CreateTriggers(fixture, captureBatch);
        var benefitResolution = ClosedWindowCaptureBenefitResolver.Resolve(
            captureBatch,
            resources,
            counterattack,
            policy,
            reverseEnumeration ? triggers.Reverse() : triggers);

        return new TemporaryLibertyCaptureBenefitFixtureExecution(
            expiryExecution,
            policy,
            benefitResolution);
    }

    private static ClosedWindowResourceState CreateResourceState(
        TemporaryLibertyFixture fixture,
        CaptureBatch captureBatch)
    {
        var flags = new List<KeyValuePair<string, bool>>();
        if (fixture.StyleId is not null &&
            captureBatch.NonKingBlackStoneCount > 0 &&
            ContentPolicy.Value.StyleRules[fixture.StyleId]
                .Contains("first_friendly_capture_bonus", StringComparer.Ordinal))
        {
            flags.Add(new KeyValuePair<string, bool>(
                $"{fixture.StyleId}.first_capture",
                fixture.StyleFirstCaptureUsed));
        }

        foreach (var sealId in fixture.EquippedSeals)
        {
            var effect = ContentPolicy.Value.SealEffects[sealId];
            if (FirstFriendlyCaptureDrawRegex().IsMatch(effect) ||
                FirstCaptureChoiceRegex().IsMatch(effect))
            {
                flags.Add(new KeyValuePair<string, bool>(
                    $"{sealId}.first_capture",
                    fixture.SealFirstCaptureUsed));
            }
        }

        return ClosedWindowResourceState.Empty(flags);
    }

    private static IReadOnlyList<CaptureBenefitTrigger> CreateTriggers(
        TemporaryLibertyFixture fixture,
        CaptureBatch captureBatch)
    {
        var triggers = new List<CaptureBenefitTrigger>();
        var whiteGroupCount = captureBatch.CapturedGroups.Count(group =>
            group.Color == StoneColor.White &&
            group.CapturingColor == StoneColor.Black);
        if (whiteGroupCount > 0)
        {
            triggers.Add(new CaptureBenefitTrigger(
                CaptureBenefitSource.StandardAccounting("standard_capture", 0),
                "standard.capture",
                ["standard_capture"],
                [new GainSoulCaptureBenefitOperation(whiteGroupCount)],
                firstUseFlagId: null));
        }

        if (whiteGroupCount > 0 && fixture.ArmedCaptureChain)
        {
            triggers.Add(new CaptureBenefitTrigger(
                CaptureBenefitSource.SourceOrArmedEffect("capture_chain", 0),
                "armed.capture_chain",
                ["capture_chain"],
                ContentPolicy.Value.CaptureChainOperations,
                firstUseFlagId: null));
        }

        for (var groupIndex = 0; groupIndex < captureBatch.CapturedGroups.Count; groupIndex++)
        {
            var group = captureBatch.CapturedGroups[groupIndex];
            for (var stoneIndex = 0; stoneIndex < group.StoneInstances.Count; stoneIndex++)
            {
                var stone = group.StoneInstances[stoneIndex];
                if (!ContentPolicy.Value.StoneOnCapturedOperations.TryGetValue(
                        stone.KindId,
                        out var operations))
                {
                    continue;
                }

                triggers.Add(new CaptureBenefitTrigger(
                    CaptureBenefitSource.CapturedStoneSelf(stone.InstanceId),
                    $"stone.{stone.InstanceId}",
                    [stone.KindId, stone.InstanceId],
                    operations,
                    firstUseFlagId: null));
            }
        }

        if (fixture.StyleId is not null &&
            captureBatch.NonKingBlackStoneCount > 0 &&
            ContentPolicy.Value.StyleRules[fixture.StyleId]
                .Contains("first_friendly_capture_bonus", StringComparer.Ordinal))
        {
            triggers.Add(new CaptureBenefitTrigger(
                CaptureBenefitSource.Style(fixture.StyleId),
                $"style.{fixture.StyleId}.first_capture",
                [fixture.StyleId, "first_capture"],
                [
                    new ReserveDrawCaptureBenefitOperation(
                        SacrificeStyleFirstCaptureReservedDraw),
                ],
                $"{fixture.StyleId}.first_capture"));
        }

        for (var slotIndex = 0; slotIndex < fixture.EquippedSeals.Count; slotIndex++)
        {
            var sealId = fixture.EquippedSeals[slotIndex];
            var effect = ContentPolicy.Value.SealEffects[sealId];
            var friendlyDraw = FirstFriendlyCaptureDrawRegex().Match(effect);
            if (friendlyDraw.Success && captureBatch.NonKingBlackStoneCount > 0)
            {
                triggers.Add(new CaptureBenefitTrigger(
                    CaptureBenefitSource.Seal(sealId, slotIndex),
                    $"seal.{sealId}.first_capture",
                    [sealId, "first_capture"],
                    [
                        new ReserveDrawCaptureBenefitOperation(
                            int.Parse(
                                friendlyDraw.Groups["amount"].Value,
                                System.Globalization.CultureInfo.InvariantCulture)),
                    ],
                    $"{sealId}.first_capture"));
                continue;
            }

            var choice = FirstCaptureChoiceRegex().Match(effect);
            if (choice.Success && captureBatch.CapturedGroups.Count > 0)
            {
                triggers.Add(new CaptureBenefitTrigger(
                    CaptureBenefitSource.Seal(sealId, slotIndex),
                    $"seal.{sealId}.first_capture",
                    [sealId],
                    [
                        new CreateDeferredChoiceCaptureBenefitOperation(
                            sealId,
                            choice.Groups["choice"].Value),
                    ],
                    $"{sealId}.first_capture"));
            }
        }

        for (var slotIndex = 0; slotIndex < fixture.EquippedRelics.Count; slotIndex++)
        {
            var relicId = fixture.EquippedRelics[slotIndex];
            var effect = ContentPolicy.Value.RelicEffects[relicId];
            var qi = CaptureGainQiRegex().Match(effect);
            if (!qi.Success || whiteGroupCount == 0)
            {
                continue;
            }

            triggers.Add(new CaptureBenefitTrigger(
                CaptureBenefitSource.Relic(relicId, slotIndex),
                $"relic.{relicId}.capture",
                [relicId],
                [
                    new ReserveQiCaptureBenefitOperation(
                        int.Parse(
                            qi.Groups["amount"].Value,
                            System.Globalization.CultureInfo.InvariantCulture)),
                ],
                firstUseFlagId: null));
        }

        if (fixture.StyleId is not null &&
            captureBatch.NonKingBlackStoneCount > 0 &&
            ContentPolicy.Value.StyleRules[fixture.StyleId]
                .Contains("sacrifice_counterattack", StringComparer.Ordinal))
        {
            triggers.Add(new CaptureBenefitTrigger(
                CaptureBenefitSource.Sacrifice(),
                "sacrifice.pressure",
                ["sacrifice"],
                [new AdvanceSacrificePressureCaptureBenefitOperation()],
                firstUseFlagId: null));
        }

        return triggers;
    }

    private static CounterattackBoundaryPolicy LoadCounterattackPolicy(int komi)
    {
        var path = Path.Combine(
            FindRepositoryRoot().FullName,
            "game_data",
            "balance",
            "system.json");
        using var stream = File.OpenRead(path);
        using var document = JsonDocument.Parse(stream);
        var counterattack = document.RootElement.GetProperty("counterattack");
        var natural = counterattack.GetProperty("enemy_turn_end_gain");
        var sacrifice = counterattack.GetProperty("sacrifice");
        var naturalGain = checked(
            natural.GetProperty("base_units").GetInt32() +
            (natural.GetProperty("per_komi_units").GetInt32() * komi));
        return new CounterattackBoundaryPolicy(
            counterattack.GetProperty("threshold_units").GetInt32(),
            naturalGain,
            sacrifice.GetProperty("non_king_black_stones_per_batch").GetInt32(),
            sacrifice.GetProperty("gain_units_per_batch").GetInt32());
    }

    private static CaptureFixtureContentPolicy LoadContentPolicy()
    {
        var root = FindRepositoryRoot();
        using var cards = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root.FullName,
            "game_data",
            "content",
            "cards.json")));
        var stoneOnCaptured = new Dictionary<string, CaptureBenefitOperation[]>(
            StringComparer.Ordinal);
        CaptureBenefitOperation[]? captureChain = null;
        foreach (var card in cards.RootElement.EnumerateArray())
        {
            var effects = card.GetProperty("effects");
            var placement = effects.EnumerateArray().FirstOrDefault(effect =>
                StringComparer.Ordinal.Equals(
                    effect.GetProperty("op").GetString(),
                    "place_stone"));
            if (placement.ValueKind != JsonValueKind.Undefined &&
                card.TryGetProperty("on_captured", out var onCaptured))
            {
                var kindId = placement.GetProperty("stone").GetString()
                    ?? throw new InvalidDataException("Placed stone kind cannot be null.");
                stoneOnCaptured.Add(
                    kindId,
                    onCaptured.EnumerateArray().Select(ParseTypedOperation).ToArray());
            }

            var armed = effects.EnumerateArray().FirstOrDefault(effect =>
                StringComparer.Ordinal.Equals(
                    effect.GetProperty("op").GetString(),
                    "arm_next_capture"));
            if (armed.ValueKind != JsonValueKind.Undefined)
            {
                captureChain =
                [
                    new ReserveQiCaptureBenefitOperation(
                        armed.GetProperty("gain_qi").GetInt32()),
                    new ReserveDrawCaptureBenefitOperation(
                        armed.GetProperty("draw").GetInt32()),
                ];
            }
        }

        using var styles = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root.FullName,
            "game_data",
            "content",
            "styles.json")));
        var styleRules = styles.RootElement
            .EnumerateArray()
            .ToDictionary(
                style => RequiredString(style, "id"),
                style => style.GetProperty("rules")
                    .EnumerateArray()
                    .Select(rule => rule.GetString()
                        ?? throw new InvalidDataException("Style rule cannot be null."))
                    .ToArray(),
                StringComparer.Ordinal);

        var sealEffects = LoadEffectMap(root, "seals.json");
        var relicEffects = LoadEffectMap(root, "relics.json");
        return new CaptureFixtureContentPolicy(
            stoneOnCaptured,
            captureChain ?? throw new InvalidDataException(
                "Capture-chain card operation is missing."),
            styleRules,
            sealEffects,
            relicEffects);
    }

    private static Dictionary<string, string> LoadEffectMap(
        DirectoryInfo root,
        string fileName)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root.FullName,
            "game_data",
            "content",
            fileName)));
        return document.RootElement
            .EnumerateArray()
            .ToDictionary(
                item => RequiredString(item, "id"),
                item => RequiredString(item, "effect"),
                StringComparer.Ordinal);
    }

    private static CaptureBenefitOperation ParseTypedOperation(JsonElement operation)
    {
        var operationId = RequiredString(operation, "op");
        return operationId switch
        {
            "reserve_draw" => new ReserveDrawCaptureBenefitOperation(
                operation.GetProperty("cards").GetInt32()),
            "gain_soul" => new GainSoulCaptureBenefitOperation(
                operation.GetProperty("value").GetInt32()),
            _ => throw new InvalidDataException(
                $"Unsupported captured-stone fixture operation {operationId}."),
        };
    }

    private static string RequiredString(JsonElement element, string propertyName) =>
        element.GetProperty(propertyName).GetString()
        ?? throw new InvalidDataException($"{propertyName} cannot be null.");

    private static DirectoryInfo FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Igorogue.sln")))
            {
                return directory;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not find Igorogue.sln from test output path.");
    }

    [GeneratedRegex("^first_friendly_capture_next_draw_(?<amount>[0-9]+)$")]
    private static partial Regex FirstFriendlyCaptureDrawRegex();

    [GeneratedRegex("^first_capture_choose_(?<choice>[a-z0-9_]+)$")]
    private static partial Regex FirstCaptureChoiceRegex();

    [GeneratedRegex("capture_gain_qi_(?<amount>[0-9]+)$")]
    private static partial Regex CaptureGainQiRegex();
}

internal sealed record CaptureFixtureContentPolicy(
    IReadOnlyDictionary<string, CaptureBenefitOperation[]> StoneOnCapturedOperations,
    CaptureBenefitOperation[] CaptureChainOperations,
    IReadOnlyDictionary<string, string[]> StyleRules,
    IReadOnlyDictionary<string, string> SealEffects,
    IReadOnlyDictionary<string, string> RelicEffects);

internal sealed record TemporaryLibertyCaptureBenefitFixtureExecution(
    TemporaryLibertyFixtureExecution ExpiryExecution,
    CounterattackBoundaryPolicy Policy,
    ClosedWindowCaptureBenefitResolution BenefitResolution);
