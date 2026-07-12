using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

using Igorogue.Application.Battle;
using Igorogue.Application.Replay;
using Igorogue.Domain.Board;
using Igorogue.Domain.Combat;
using Igorogue.Domain.Facilities;

namespace Igorogue.Application.Tests;

internal static class TemporaryLibertyGoldenFixtureAdapter
{
    internal const string RelativeCatalogPath =
        "tests/golden/v2/temporary_liberty_cases.json";
    internal const string RelativeSourcePath =
        "game_data/fixtures/temporary_liberty_expiry_fixtures.json";
    internal const string SchemaId = "igorogue.temporary-liberty-golden";
    internal const int SchemaVersion = 2;
    internal const string FactProjectionId = "igorogue-tle-battle-fact-v2";
    internal const string StateProjectionId = "headless-battle-state-v2";
    internal const string GameVersion = "v0.2.10";
    internal const string ContentHash =
        "sha256:b411ddf2dfb8e876370d11f2259368b7d898fcfebe8a4e4fb24c30802968ee06";
    internal const long Seed = 42;

    // These two values are accepted player-visible rules without duplicated
    // runtime fields in game_data. They remain test-adapter constants only.
    private const int StandardWhiteCaptureSoul = 1;
    private const int AcceptedStyleSacrificeFirstCaptureDraw = 2;

    private static readonly string[] RuntimeSourcePaths =
    [
        RelativeSourcePath,
        "game_data/balance/system.json",
        "game_data/content/board_conditions.json",
        "game_data/content/cards.json",
        "game_data/content/relics.json",
        "game_data/content/seals.json",
        "game_data/content/styles.json",
    ];

    private static readonly JsonSerializerOptions CatalogJsonOptions = new()
    {
        PropertyNameCaseInsensitive = false,
        ReadCommentHandling = JsonCommentHandling.Disallow,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        WriteIndented = true,
    };

    internal static TemporaryLibertyGoldenCatalog LoadCatalog()
    {
        using var stream = File.OpenRead(RepositoryFilePath(RelativeCatalogPath));
        return JsonSerializer.Deserialize<TemporaryLibertyGoldenCatalog>(
                stream,
                CatalogJsonOptions)
            ?? throw new InvalidDataException(
                "Temporary-liberty golden catalog is empty.");
    }

    internal static IReadOnlyDictionary<string, TemporaryLibertyGoldenSourceCase>
        LoadSourceCases()
    {
        using var stream = File.OpenRead(RepositoryFilePath(RelativeSourcePath));
        using var document = JsonDocument.Parse(stream);
        var root = document.RootElement;
        if (!StringComparer.Ordinal.Equals(
                RequiredString(root, "version", "source/version"),
                "1.0.0"))
        {
            throw new InvalidDataException(
                "Unsupported temporary-liberty source fixture version.");
        }

        var cases = root.GetProperty("cases")
            .EnumerateArray()
            .Select(ParseSourceCase)
            .ToArray();
        if (cases.Length != 15 ||
            cases.Select(item => item.Id).Distinct(StringComparer.Ordinal).Count() !=
            cases.Length)
        {
            throw new InvalidDataException(
                "Temporary-liberty source must contain 15 unique cases.");
        }

        return cases.ToDictionary(item => item.Id, StringComparer.Ordinal);
    }

    internal static TemporaryLibertyGoldenRunResult ExecuteCase(
        string fixtureId,
        bool reverseSetupEnumeration = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fixtureId);
        var sourceCases = LoadSourceCases();
        if (!sourceCases.TryGetValue(fixtureId, out var source))
        {
            throw new KeyNotFoundException(fixtureId);
        }

        var content = LoadRuntimeContent();
        var initialSnapshot = CreateInitialSnapshot(
            source,
            content,
            reverseSetupEnumeration);
        var initialSession = HeadlessBattleStateMachine.Start(
            initialSnapshot,
            ReplayMetadata.Create(GameVersion, ContentHash, Seed));
        AssertAuthoritative(initialSession);

        var session = initialSession;
        var results = new List<BattleCommandResult>();
        var boundaries = new List<TemporaryLibertyGoldenActualBoundary>();

        Execute(new EndPlayerTurnCommand(
            session.State.Checksum,
            session.CommandLog.CurrentChecksum));
        Execute(new ResolveEnemyPassCommand(
            session.State.Checksum,
            session.CommandLog.CurrentChecksum));
        if (!session.State.IsTerminal && session.State.Phase == BattlePhase.EnemyAction)
        {
            Execute(new ResolveEnemyPassCommand(
                session.State.Checksum,
                session.CommandLog.CurrentChecksum));
        }

        var projectedFacts = boundaries
            .SelectMany(boundary => boundary.OrderedFacts)
            .ToArray();
        var stageTrace = results
            .SelectMany(result => result.OrderedFacts)
            .OfType<EnemyTurnBoundaryStageFact>()
            .Select(fact => fact.StageId)
            .ToArray();
        return new TemporaryLibertyGoldenRunResult(
            source,
            initialSnapshot,
            initialSession,
            results.ToArray(),
            session,
            boundaries.ToArray(),
            projectedFacts,
            stageTrace);

        void Execute(IBattleCommand command)
        {
            var result = HeadlessBattleStateMachine.Execute(session, command);
            results.Add(result);
            session = result.SessionAfter;
            AssertAuthoritative(session);
            boundaries.Add(new TemporaryLibertyGoldenActualBoundary(
                command.CommandType,
                command.CommandSchemaVersion,
                command.ToCanonicalPayload(),
                result.Accepted,
                result.ReasonId,
                result.StateChecksum,
                result.LogChecksum,
                result.OrderedFacts.Select(ProjectFact).ToArray()));
        }
    }

    internal static TemporaryLibertyGoldenCatalog BuildCurrentCatalog()
    {
        var sources = LoadSourceCases();
        var cases = sources.Values
            .OrderBy(item => item.NumericId)
            .Select(source => ProjectCase(ExecuteCase(source.Id), source))
            .ToArray();
        return new TemporaryLibertyGoldenCatalog
        {
            SchemaId = SchemaId,
            SchemaVersion = SchemaVersion,
            StateProjection = StateProjectionId,
            FactProjection = FactProjectionId,
            GameVersion = GameVersion,
            ContentHash = ContentHash,
            SourceCatalogs = RuntimeSourcePaths.Select(path =>
                new TemporaryLibertyGoldenSourceCatalog
                {
                    Path = path,
                    Sha256 = Sha256ForRepositoryFile(path),
                }).ToArray(),
            Claims = new TemporaryLibertyGoldenClaims
            {
                MomentumEventCount = 0,
                BrilliantEventCount = 0,
                CounterattackFixtureCoverageClaimCount = 0,
            },
            Cases = cases,
        };
    }

    internal static string SerializeCatalog(TemporaryLibertyGoldenCatalog catalog) =>
        JsonSerializer.Serialize(catalog, CatalogJsonOptions) + "\n";

    internal static string CatalogPath() => RepositoryFilePath(RelativeCatalogPath);

    internal static string Sha256ForRepositoryFile(string relativePath)
    {
        using var stream = File.OpenRead(RepositoryFilePath(relativePath));
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    internal static string ProjectFact(IBattleFact fact) => fact switch
    {
        EnemyTurnBoundaryStageFact stage =>
            $"boundary_stage|turn={Invariant(stage.EnemyTurnIndex)}|stage={stage.StageId}",
        EnemyPassedFact passed =>
            $"enemy_passed|turn={Invariant(passed.PlayerTurnIndex)}",
        TemporaryLibertyExpirySweepStartedFact started =>
            $"expiry_started|turn={Invariant(started.EnemyTurnIndex)}",
        TemporaryLibertyExpiredFact expired =>
            $"liberty_expired|id={expired.Effect.EffectInstanceId}|" +
            $"sequence={Invariant(expired.Effect.CreatedSequence)}",
        TemporaryLibertyGroupCapturedFact captured =>
            $"expiry_group_captured|color={ColorId(captured.CapturedGroup.Color)}|" +
            $"by={ColorId(captured.CapturingColor)}|" +
            $"anchor={PointId(captured.CapturedGroup.Anchor)}|" +
            $"stones={string.Join(';', captured.CapturedGroup.StonePoints.Select(PointId))}|" +
            $"king={Flag(captured.ContainsKing)}",
        TemporaryLibertyRemovedFact removed =>
            $"temporary_liberty_removed|id={removed.Effect.EffectInstanceId}|" +
            $"reason={removed.ReasonId}",
        StoneTopologyRegisteredFact topology =>
            $"topology_registered|cells={topology.RegisteredTopologyKey.CanonicalCells}|" +
            $"observations={Invariant(topology.HistoryAfterRegistration.ObservationCount)}|" +
            $"first_seen={Flag(topology.FirstSeen)}|reason={topology.SourceReasonId}",
        TemporaryLibertyKingGateFact king =>
            $"expiry_king_gate|outcome={king.Result.OutcomeId}|" +
            $"reason={king.Result.EndReasonId}|black={Flag(king.Result.BlackKingCaptured)}|" +
            $"white={Flag(king.Result.WhiteKingCaptured)}",
        CaptureBenefitSuppressedFact suppressed =>
            $"capture_benefit_suppressed|reason={suppressed.ReasonId}",
        TemporaryLibertyExpirySweepResolvedFact resolved =>
            $"expiry_resolved|turn={Invariant(resolved.EnemyTurnIndex)}|" +
            $"groups={Invariant(resolved.CapturedGroupCount)}|terminal={Flag(resolved.Terminal)}",
        CaptureBatchStartedFact started =>
            $"capture_batch_started|id={started.CaptureBatch.BatchId}",
        TurnReservedDrawChangedFact draw => AppliedResource(
            "reserved_draw",
            draw.TriggerId,
            draw.EventId,
            draw.AmountBefore,
            draw.AmountAfter,
            draw.Delta),
        TurnReservedQiChangedFact qi => AppliedResource(
            "reserved_qi",
            qi.TriggerId,
            qi.EventId,
            qi.AmountBefore,
            qi.AmountAfter,
            qi.Delta),
        SoulChangedFact soul => AppliedResource(
            "soul",
            soul.TriggerId,
            soul.EventId,
            soul.AmountBefore,
            soul.AmountAfter,
            soul.Delta),
        DeferredPlayerChoiceCreatedFact choice =>
            $"deferred_choice_created|trigger={choice.TriggerId}|event={choice.EventId}|" +
            $"choice={choice.Choice.Id}|sequence={Invariant(choice.Choice.CreatedSequence)}",
        FirstUseFlagConsumedFact firstUse =>
            $"first_use_consumed|trigger={firstUse.TriggerId}|flag={firstUse.FlagId}",
        SacrificeRemainderChangedFact remainder =>
            $"sacrifice_remainder_changed|stones={Invariant(remainder.CapturedNonKingBlackStoneCount)}|" +
            $"before={Invariant(remainder.RemainderBefore)}|after={Invariant(remainder.RemainderAfter)}",
        SacrificeBatchAdvancedFact sacrifice =>
            $"sacrifice_batch_advanced|stones={Invariant(sacrifice.CapturedNonKingBlackStoneCount)}|" +
            $"batches={Invariant(sacrifice.CompletedBatchCount)}|" +
            $"before={Invariant(sacrifice.RemainderBefore)}|after={Invariant(sacrifice.RemainderAfter)}|" +
            $"delta={Invariant(sacrifice.DeltaUnits)}",
        CounterattackAdvancedFact advanced =>
            $"counterattack_advanced|reason={advanced.ReasonId}|" +
            $"before={Invariant(advanced.GaugeUnitsBefore)}|" +
            $"gained={Invariant(advanced.UnitsAfterGainBeforePrime)}|" +
            $"after={Invariant(advanced.GaugeUnitsAfter)}|delta={Invariant(advanced.DeltaUnits)}|" +
            $"pending_before={Flag(advanced.PendingBefore)}|pending_after={Flag(advanced.PendingAfter)}",
        CounterattackPendingConsumedFact consumed =>
            $"counterattack_pending_consumed|reprimed={Flag(consumed.Reprimed)}",
        CounterattackPendingPrimedFact primed =>
            $"counterattack_pending_primed|reason={PrimeReasonId(primed.Reason)}|" +
            $"residual={Invariant(primed.ResidualGaugeUnits)}",
        CaptureBatchResolvedFact resolved =>
            $"capture_batch_resolved|id={resolved.BatchId}|" +
            $"suppressed={Flag(resolved.BenefitsSuppressed)}",
        TerritoryEstablishedFact territory =>
            $"territory_established|actor={ColorId(territory.SourceActor)}|" +
            $"source={TerritorySourceId(territory.SourceKind)}|" +
            $"reason={territory.SourceReasonId}|" +
            $"implicit_momentum_eligible={Flag(territory.ImplicitMomentumEligible)}|" +
            $"points={string.Join(';', territory.ChangedPoints.Select(PointId))}",
        FacilityActivatedFact activated =>
            $"facility_activated|id={activated.Facility.InstanceId}|reason={activated.ReasonId}",
        FacilityDisabledFact disabled =>
            $"facility_disabled|id={disabled.Facility.InstanceId}|reason={disabled.ReasonId}",
        BattleEndedFact ended =>
            $"battle_ended|outcome={OutcomeId(ended.Outcome)}|reason={ended.ReasonId}",
        CommandRejectedFact rejected =>
            $"command_rejected|reason={rejected.ReasonId}",
        _ => throw new InvalidOperationException(
            $"Unhandled TLE golden fact type {fact.GetType().FullName}."),
    };

    private static BattleAuthoritativeInitialSnapshot CreateInitialSnapshot(
        TemporaryLibertyGoldenSourceCase source,
        TemporaryLibertyRuntimeContent content,
        bool reverseSetupEnumeration)
    {
        var geometry = BoardGeometry.Create(content.BoardSize);
        var board = ParseBoard(geometry, source.Board);
        var stones = CreateStoneRuntime(
            board,
            source.StoneOverrides,
            reverseSetupEnumeration);
        var effects = source.Effects
            .Select(effect => effect.ToDomain(stones))
            .ToArray();
        var nextEffectSequence = effects.Length == 0
            ? 1L
            : checked(effects.Max(effect => effect.CreatedSequence) + 1L);
        var temporary = TemporaryLibertyState.Create(
            stones,
            reverseSetupEnumeration ? effects.Reverse() : effects,
            nextEffectSequence);
        var modifiers = source.ContinuousModifiers
            .Select(modifier => modifier.ToDomain(stones))
            .ToArray();
        foreach (var modifier in source.ContinuousModifiers)
        {
            if (!StringComparer.Ordinal.Equals(
                    content.BoardConditionEffect(modifier.SourceId),
                    "adjacent_continuous_virtual_liberty"))
            {
                throw new InvalidDataException(
                    $"Unsupported continuous-liberty source '{modifier.SourceId}'.");
            }
        }

        var continuous = ContinuousLibertySnapshot.Create(
            stones,
            reverseSetupEnumeration ? modifiers.Reverse() : modifiers);

        var history = BattleRepetitionHistory.Start(board);
        if (source.ResultTopologySeenBefore)
        {
            var provisional = TemporaryLibertyExpiryResolver.Resolve(
                stones,
                temporary,
                continuous,
                history,
                source.EnemyTurnIndex);
            if (provisional.CaptureBatch is null)
            {
                throw new InvalidDataException(
                    $"{source.Id} requests seen result history without an expiry capture.");
            }

            history = BattleRepetitionHistory.FromObservedBoards(
                [provisional.BoardAfterResolution, board]);
        }

        var planAndFlags = CreateTriggerPlan(
            source,
            content,
            stones,
            reverseSetupEnumeration);
        var equippedSealKomi = source.EquippedSeals.Sum(content.SealKomi);
        if (source.ExplicitKomi is int explicitKomi &&
            explicitKomi != equippedSealKomi)
        {
            throw new InvalidDataException(
                $"{source.Id} explicit komi {explicitKomi.ToString(CultureInfo.InvariantCulture)} " +
                $"does not match equipped-seal komi {equippedSealKomi.ToString(CultureInfo.InvariantCulture)}.");
        }

        var komi = source.ExplicitKomi ?? equippedSealKomi;
        var counterPolicy = new CounterattackBoundaryPolicy(
            content.CounterattackThresholdUnits,
            checked(
                content.CounterattackNaturalBaseUnits +
                (content.CounterattackNaturalPerKomiUnits * komi)),
            content.SacrificeStonesPerBatch,
            content.SacrificeUnitsPerBatch);
        var startGauge = source.StartCounterattackUnits ?? checked(
            content.CounterattackStartBaseUnits +
            (content.CounterattackStartPerKomiUnits * komi));
        var counterState = CounterattackBoundaryState.Create(
            startGauge,
            source.PendingAtEnemyTurnStart,
            source.StartSacrificeRemainder,
            counterPolicy);
        var facilities = FacilityState.Create(board, [], 1);
        return BattleAuthoritativeInitialSnapshot.Create(
            stones,
            temporary,
            continuous,
            history,
            facilities,
            ClosedWindowResourceState.Empty(planAndFlags.FirstUseFlags),
            planAndFlags.Plan,
            counterState,
            counterPolicy,
            content.RuntimePolicy,
            source.EnemyTurnIndex);
    }

    private static TemporaryLibertyPlanAndFlags CreateTriggerPlan(
        TemporaryLibertyGoldenSourceCase source,
        TemporaryLibertyRuntimeContent content,
        StoneRuntimeState stones,
        bool reverseSetupEnumeration)
    {
        var entries = new List<CaptureBenefitTriggerPlanEntry>();
        var flags = new Dictionary<string, bool>(StringComparer.Ordinal);
        entries.Add(new CaptureBenefitTriggerPlanEntry(
            new CaptureBenefitTrigger(
                CaptureBenefitSource.StandardAccounting("standard_capture", 0),
                "standard_capture.soul",
                ["standard_capture"],
                [new GainSoulCaptureBenefitOperation(StandardWhiteCaptureSoul)],
                null),
            CaptureBenefitTriggerCondition.CapturedWhiteGroup,
            CaptureBenefitTriggerMaterializationMode.GainSoulPerCapturedWhiteGroup));

        if (source.ArmedCaptureChain)
        {
            var arm = content.CaptureChainArm;
            entries.Add(new CaptureBenefitTriggerPlanEntry(
                new CaptureBenefitTrigger(
                    CaptureBenefitSource.SourceOrArmedEffect("capture_chain", 0),
                    "capture_chain.armed",
                    ["capture_chain"],
                    [
                        new ReserveQiCaptureBenefitOperation(arm.Qi),
                        new ReserveDrawCaptureBenefitOperation(arm.Draw),
                    ],
                    null),
                CaptureBenefitTriggerCondition.CapturedWhiteGroup));
        }

        foreach (var stone in stones.Instances)
        {
            var onCaptured = content.OnCapturedOperationsByStoneKind
                .GetValueOrDefault(stone.KindId);
            if (onCaptured is null)
            {
                continue;
            }

            entries.Add(new CaptureBenefitTriggerPlanEntry(
                new CaptureBenefitTrigger(
                    CaptureBenefitSource.CapturedStoneSelf(stone.InstanceId),
                    $"{stone.KindId}.{stone.InstanceId}",
                    [stone.KindId, stone.InstanceId],
                    CreateOperations(onCaptured),
                    null),
                CaptureBenefitTriggerCondition.CapturedSourceStone));
        }

        if (source.StyleId is not null)
        {
            var styleRules = content.StyleRules(source.StyleId);
            if (styleRules.Contains("first_friendly_capture_bonus"))
            {
                const string flagId = "style_sacrifice.first_capture";
                flags.Add(flagId, source.StyleFirstCaptureUsed);
                entries.Add(new CaptureBenefitTriggerPlanEntry(
                    new CaptureBenefitTrigger(
                        CaptureBenefitSource.Style(source.StyleId),
                        "style_sacrifice.first_capture",
                        ["style_sacrifice", "first_capture"],
                        [new ReserveDrawCaptureBenefitOperation(
                            AcceptedStyleSacrificeFirstCaptureDraw)],
                        flagId),
                    CaptureBenefitTriggerCondition.CapturedNonKingBlackStone));
            }

            if (styleRules.Contains("sacrifice_counterattack"))
            {
                entries.Add(new CaptureBenefitTriggerPlanEntry(
                    new CaptureBenefitTrigger(
                        CaptureBenefitSource.Sacrifice(),
                        "sacrifice_pressure.capture",
                        ["sacrifice_pressure"],
                        [new AdvanceSacrificePressureCaptureBenefitOperation()],
                        null),
                    CaptureBenefitTriggerCondition.CapturedNonKingBlackStone));
            }
        }

        for (var slot = 0; slot < source.EquippedSeals.Count; slot++)
        {
            var sealId = source.EquippedSeals[slot];
            var effect = content.SealEffect(sealId);
            if (StringComparer.Ordinal.Equals(
                    effect,
                    "first_friendly_capture_next_draw_2"))
            {
                const string flagId = "seal_sacrifice.first_capture";
                flags.Add(flagId, source.SealFirstCaptureUsed);
                entries.Add(new CaptureBenefitTriggerPlanEntry(
                    new CaptureBenefitTrigger(
                        CaptureBenefitSource.Seal(sealId, slot),
                        "seal_sacrifice.first_capture",
                        ["seal_sacrifice", "first_capture"],
                        [new ReserveDrawCaptureBenefitOperation(
                            ParseTrailingPositiveInt(effect))],
                        flagId),
                    CaptureBenefitTriggerCondition.CapturedNonKingBlackStone));
            }
            else if (StringComparer.Ordinal.Equals(
                         effect,
                         "first_capture_choose_qi_or_draw"))
            {
                var flagId = $"{sealId}.first_capture";
                flags.Add(flagId, false);
                entries.Add(new CaptureBenefitTriggerPlanEntry(
                    new CaptureBenefitTrigger(
                        CaptureBenefitSource.Seal(sealId, slot),
                        $"{sealId}.first_capture",
                        [sealId],
                        [new CreateDeferredChoiceCaptureBenefitOperation(
                            sealId,
                            "qi_or_draw")],
                        flagId),
                    CaptureBenefitTriggerCondition.AnyCapture));
            }
            else
            {
                throw new InvalidDataException(
                    $"Unsupported equipped TLE seal effect '{effect}'.");
            }
        }

        for (var slot = 0; slot < source.EquippedRelics.Count; slot++)
        {
            var relicId = source.EquippedRelics[slot];
            var effect = content.RelicEffect(relicId);
            if (!StringComparer.Ordinal.Equals(
                    effect,
                    "halve_territory_income_capture_gain_qi_2"))
            {
                throw new InvalidDataException(
                    $"Unsupported equipped TLE relic effect '{effect}'.");
            }

            entries.Add(new CaptureBenefitTriggerPlanEntry(
                new CaptureBenefitTrigger(
                    CaptureBenefitSource.Relic(relicId, slot),
                    $"{relicId}.capture",
                    [relicId],
                    [new ReserveQiCaptureBenefitOperation(
                        ParseTrailingPositiveInt(effect))],
                    null),
                CaptureBenefitTriggerCondition.CapturedWhiteGroup));
        }

        IEnumerable<CaptureBenefitTriggerPlanEntry> entryInput = entries;
        IEnumerable<KeyValuePair<string, bool>> flagInput = flags;
        if (reverseSetupEnumeration)
        {
            entryInput = entries.AsEnumerable().Reverse();
            flagInput = flags.Reverse();
        }

        return new TemporaryLibertyPlanAndFlags(
            CaptureBenefitTriggerPlan.CreateConditional(entryInput),
            flagInput.ToArray());
    }

    private static CaptureBenefitOperation[] CreateOperations(
        IReadOnlyList<TemporaryLibertyContentOperation> operations) => operations
        .Select<TemporaryLibertyContentOperation, CaptureBenefitOperation>(operation =>
            operation.Kind switch
            {
                "reserve_draw" => new ReserveDrawCaptureBenefitOperation(operation.Amount),
                "gain_soul" => new GainSoulCaptureBenefitOperation(operation.Amount),
                _ => throw new InvalidDataException(
                    $"Unsupported captured-stone operation '{operation.Kind}'."),
            })
        .ToArray();

    private static TemporaryLibertyRuntimeContent LoadRuntimeContent()
    {
        using var systemDocument = OpenJson("game_data/balance/system.json");
        var system = systemDocument.RootElement;
        var boardSize = system.GetProperty("board_size").GetInt32();
        var bands = system.GetProperty("facility_capacity")
            .EnumerateArray()
            .Select(item => new FacilityCapacityBand(
                item.GetProperty("min").GetInt32(),
                item.GetProperty("max").GetInt32(),
                item.GetProperty("slots").GetInt32()))
            .ToArray();
        var typeLimits = system.GetProperty("facility_type_limits_per_region")
            .EnumerateObject()
            .OrderBy(property => property.Name, StringComparer.Ordinal)
            .Select(property => new KeyValuePair<string, int>(
                property.Name,
                property.Value.GetInt32()))
            .ToArray();
        var runtimePolicy = new BattleRuntimePolicy(
            system.GetProperty("turn_limit").GetInt32(),
            FacilityRuntimePolicy.Create(
                system.GetProperty("territory_income_divisor").GetInt32(),
                bands,
                system.GetProperty("facility_slot_cap").GetInt32(),
                typeLimits));
        var counterattack = system.GetProperty("counterattack");

        var cards = LoadContentEntries("game_data/content/cards.json");
        var onCapturedByKind = new Dictionary<
            string,
            IReadOnlyList<TemporaryLibertyContentOperation>>(StringComparer.Ordinal);
        foreach (var cardId in new[] { "card_lure_stone", "card_blood_stone" })
        {
            var card = RequiredEntry(cards, cardId);
            var kind = AssertPlacedStoneKind(card, cardId);
            var operations = card.GetProperty("on_captured")
                .EnumerateArray()
                .Select(ParseContentOperation)
                .ToArray();
            onCapturedByKind.Add(kind, operations);
        }

        var captureChain = RequiredEntry(cards, "card_capture_chain")
            .GetProperty("effects")
            .EnumerateArray()
            .Single(item => StringComparer.Ordinal.Equals(
                RequiredString(item, "op", "card_capture_chain/effects"),
                "arm_next_capture"));
        var captureChainArm = new TemporaryLibertyCaptureChainArm(
            captureChain.GetProperty("gain_qi").GetInt32(),
            captureChain.GetProperty("draw").GetInt32());

        return new TemporaryLibertyRuntimeContent(
            boardSize,
            runtimePolicy,
            counterattack.GetProperty("threshold_units").GetInt32(),
            counterattack.GetProperty("battle_start").GetProperty("base_units").GetInt32(),
            counterattack.GetProperty("battle_start").GetProperty("per_komi_units").GetInt32(),
            counterattack.GetProperty("enemy_turn_end_gain").GetProperty("base_units").GetInt32(),
            counterattack.GetProperty("enemy_turn_end_gain").GetProperty("per_komi_units").GetInt32(),
            counterattack.GetProperty("sacrifice").GetProperty("non_king_black_stones_per_batch").GetInt32(),
            counterattack.GetProperty("sacrifice").GetProperty("gain_units_per_batch").GetInt32(),
            onCapturedByKind,
            captureChainArm,
            LoadStringArrayEntries("game_data/content/styles.json", "rules"),
            LoadStringEntries("game_data/content/seals.json", "effect"),
            LoadIntEntries("game_data/content/seals.json", "komi"),
            LoadStringEntries("game_data/content/relics.json", "effect"),
            LoadStringEntries("game_data/content/board_conditions.json", "effect"));
    }

    private static IReadOnlyDictionary<string, JsonElement> LoadContentEntries(
        string relativePath)
    {
        using var document = OpenJson(relativePath);
        return document.RootElement
            .EnumerateArray()
            .ToDictionary(
                item => RequiredString(item, "id", $"{relativePath}/id"),
                item => item.Clone(),
                StringComparer.Ordinal);
    }

    private static IReadOnlyDictionary<string, IReadOnlySet<string>>
        LoadStringArrayEntries(string relativePath, string propertyName) =>
        LoadContentEntries(relativePath).ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlySet<string>)pair.Value.GetProperty(propertyName)
                .EnumerateArray()
                .Select(value => value.GetString() ?? throw new InvalidDataException(
                    $"{relativePath}/{pair.Key}/{propertyName} contains null."))
                .ToHashSet(StringComparer.Ordinal),
            StringComparer.Ordinal);

    private static IReadOnlyDictionary<string, string> LoadStringEntries(
        string relativePath,
        string propertyName) => LoadContentEntries(relativePath).ToDictionary(
            pair => pair.Key,
            pair => RequiredString(
                pair.Value,
                propertyName,
            $"{relativePath}/{pair.Key}/{propertyName}"),
            StringComparer.Ordinal);

    private static IReadOnlyDictionary<string, int> LoadIntEntries(
        string relativePath,
        string propertyName) => LoadContentEntries(relativePath).ToDictionary(
            pair => pair.Key,
            pair => pair.Value.GetProperty(propertyName).GetInt32(),
            StringComparer.Ordinal);

    private static string AssertPlacedStoneKind(JsonElement card, string cardId) =>
        RequiredString(
            card.GetProperty("effects")
                .EnumerateArray()
                .Single(item => StringComparer.Ordinal.Equals(
                    RequiredString(item, "op", $"{cardId}/effects/op"),
                    "place_stone")),
            "stone",
            $"{cardId}/effects/place_stone/stone");

    private static TemporaryLibertyContentOperation ParseContentOperation(
        JsonElement element)
    {
        var kind = RequiredString(element, "op", "on_captured/op");
        var amount = kind switch
        {
            "reserve_draw" => element.GetProperty("cards").GetInt32(),
            "gain_soul" => element.GetProperty("value").GetInt32(),
            _ => throw new InvalidDataException(
                $"Unsupported on_captured content operation '{kind}'."),
        };
        return new TemporaryLibertyContentOperation(kind, amount);
    }

    private static TemporaryLibertyGoldenSourceCase ParseSourceCase(JsonElement element)
    {
        var id = RequiredString(element, "id", "source/case/id");
        var numericId = ParseNumericFixtureId(id);
        var board = element.TryGetProperty("board", out var boardElement)
            ? boardElement.EnumerateArray().Select(row =>
                row.GetString() ?? throw new InvalidDataException(
                    $"{id}/board contains null.")).ToArray()
            : Enumerable.Repeat(".......", BoardGeometry.AcceptedSize).ToArray();
        return new TemporaryLibertyGoldenSourceCase(
            id,
            numericId,
            RequiredString(element, "operation", $"{id}/operation"),
            RequiredString(element, "title", $"{id}/title"),
            element.TryGetProperty("enemy_turn_index", out var turn)
                ? turn.GetInt32()
                : numericId,
            board,
            ParseStoneOverrides(element, id),
            ParseEffects(element, id),
            ParseContinuousModifiers(element, id),
            OptionalStrings(element, "equipped_seals", id),
            OptionalStrings(element, "equipped_relics", id),
            element.TryGetProperty("style_id", out var style)
                ? style.GetString()
                : null,
            element.TryGetProperty("armed_capture_chain", out var armed) &&
                armed.GetBoolean(),
            element.TryGetProperty("style_first_capture_used", out var styleUsed) &&
                styleUsed.GetBoolean(),
            element.TryGetProperty("seal_first_capture_used", out var sealUsed) &&
                sealUsed.GetBoolean(),
            element.TryGetProperty("start_sacrifice_remainder", out var remainder)
                ? remainder.GetInt32()
                : 0,
            element.TryGetProperty("start_counterattack_units", out var startUnits)
                ? startUnits.GetInt32()
                : null,
            element.TryGetProperty("komi", out var komi) ? komi.GetInt32() : null,
            element.TryGetProperty("pending_at_enemy_turn_start", out var pending) &&
                pending.GetBoolean(),
            element.TryGetProperty("result_topology_seen_before", out var seen) &&
                seen.GetBoolean());
    }

    private static IReadOnlyList<TemporaryLibertyGoldenStoneOverride>
        ParseStoneOverrides(JsonElement element, string id) =>
        element.TryGetProperty("stone_overrides", out var overrides)
            ? overrides.EnumerateArray().Select(item =>
                new TemporaryLibertyGoldenStoneOverride(
                    ParsePoint(item.GetProperty("point"), $"{id}/stone_overrides/point"),
                    RequiredString(item, "instance_id", $"{id}/stone_overrides/instance_id"),
                    RequiredString(item, "kind", $"{id}/stone_overrides/kind")))
                .ToArray()
            : [];

    private static IReadOnlyList<TemporaryLibertyGoldenEffect> ParseEffects(
        JsonElement element,
        string id) => element.TryGetProperty("effects", out var effects)
        ? effects.EnumerateArray().Select(item =>
            new TemporaryLibertyGoldenEffect(
                RequiredString(item, "id", $"{id}/effects/id"),
                ParsePoint(item.GetProperty("anchor_point"), $"{id}/effects/anchor_point"),
                item.GetProperty("amount").GetInt32(),
                item.GetProperty("created_sequence").GetInt64(),
                item.GetProperty("expires_after_enemy_turn_index").GetInt32(),
                RequiredString(item, "source_id", $"{id}/effects/source_id")))
            .ToArray()
        : [];

    private static IReadOnlyList<TemporaryLibertyGoldenContinuousModifier>
        ParseContinuousModifiers(JsonElement element, string id) =>
        element.TryGetProperty("continuous_modifiers", out var modifiers)
            ? modifiers.EnumerateArray().Select(item =>
                new TemporaryLibertyGoldenContinuousModifier(
                    RequiredString(item, "id", $"{id}/continuous_modifiers/id"),
                    ParsePoint(
                        item.GetProperty("anchor_point"),
                        $"{id}/continuous_modifiers/anchor_point"),
                    item.GetProperty("amount").GetInt32(),
                    RequiredString(
                        item,
                        "source_id",
                        $"{id}/continuous_modifiers/source_id")))
                .ToArray()
            : [];

    private static IReadOnlyList<string> OptionalStrings(
        JsonElement element,
        string propertyName,
        string id) => element.TryGetProperty(propertyName, out var values)
        ? values.EnumerateArray().Select(value =>
            value.GetString() ?? throw new InvalidDataException(
                $"{id}/{propertyName} contains null.")).ToArray()
        : [];

    private static BoardState ParseBoard(
        BoardGeometry geometry,
        IReadOnlyList<string> rows)
    {
        if (rows.Count != geometry.Size || rows.Any(row => row.Length != geometry.Size))
        {
            throw new InvalidDataException("TLE board must be a 7x7 diagram.");
        }

        var stones = new List<BoardStone>();
        for (var row = 0; row < rows.Count; row++)
        {
            var y = geometry.CanonicalYFromDiagramRow(row);
            for (var column = 0; column < rows[row].Length; column++)
            {
                var parsed = rows[row][column] switch
                {
                    '.' => ((StoneColor Color, bool King)?)null,
                    'B' => (StoneColor.Black, false),
                    'K' => (StoneColor.Black, true),
                    'W' => (StoneColor.White, false),
                    'Q' => (StoneColor.White, true),
                    _ => throw new InvalidDataException(
                        $"Unknown TLE board symbol '{rows[row][column]}'."),
                };
                if (parsed is not null)
                {
                    stones.Add(new BoardStone(
                        parsed.Value.Color,
                        parsed.Value.King,
                        geometry.CreateCanonicalPoint(column + 1, y)));
                }
            }
        }

        return BoardState.Create(geometry, stones);
    }

    private static StoneRuntimeState CreateStoneRuntime(
        BoardState board,
        IReadOnlyList<TemporaryLibertyGoldenStoneOverride> overrides,
        bool reverseSetupEnumeration)
    {
        var byPoint = overrides.ToDictionary(
            item => item.Point.ToDomain(board.Geometry),
            item => item);
        var instances = board.OccupiedStones
            .Select((stone, index) =>
            {
                byPoint.TryGetValue(stone.Point, out var itemOverride);
                return new StoneRuntimeInstance(
                    itemOverride?.InstanceId ??
                        $"stone_{(index + 1).ToString("D2", CultureInfo.InvariantCulture)}",
                    stone,
                    itemOverride?.KindId ?? (stone.IsKing ? "king" : "standard"),
                    index + 1L,
                    []);
            })
            .ToArray();
        if (byPoint.Keys.Any(point => board.StoneAt(point) is null))
        {
            throw new InvalidDataException("TLE stone override targets an empty point.");
        }

        return StoneRuntimeState.Create(
            board,
            reverseSetupEnumeration ? instances.Reverse() : instances,
            instances.Length + 1L);
    }

    private static TemporaryLibertyGoldenCase ProjectCase(
        TemporaryLibertyGoldenRunResult run,
        TemporaryLibertyGoldenSourceCase source)
    {
        var runtime = run.FinalSession.State.AuthoritativeRuntime
            ?? throw new InvalidOperationException("TLE golden final state is not authoritative.");
        return new TemporaryLibertyGoldenCase
        {
            Id = source.Id,
            Title = source.Title,
            SourceFixtureId = source.Id,
            SourceOperation = source.Operation,
            Adapter = source.Operation == "phase_order"
                ? "phase_boundary_adapter"
                : "expiry_sweep_adapter",
            Seed = Seed,
            Initial = new TemporaryLibertyGoldenInitial
            {
                SnapshotChecksum = run.InitialSnapshot.Checksum,
                StateChecksum = run.InitialSession.State.Checksum,
                LogChecksum = run.InitialSession.CommandLog.CurrentChecksum,
            },
            Commands = run.Boundaries.Select(boundary =>
                new TemporaryLibertyGoldenCommand
                {
                    Type = boundary.CommandType,
                    SchemaVersion = boundary.CommandSchemaVersion,
                    CanonicalPayload = boundary.CanonicalPayload,
                    Accepted = boundary.Accepted,
                    Reason = boundary.Reason,
                    StateChecksum = boundary.StateChecksum,
                    LogChecksum = boundary.LogChecksum,
                    OrderedFacts = boundary.OrderedFacts,
                }).ToArray(),
            StageTrace = run.StageTrace,
            Final = new TemporaryLibertyGoldenFinal
            {
                StateChecksum = run.FinalSession.State.Checksum,
                LogChecksum = run.FinalSession.CommandLog.CurrentChecksum,
                TemporaryEffectIds = runtime.TemporaryLibertyState.Effects
                    .Select(effect => effect.EffectInstanceId).ToArray(),
                TopologyObservationCount =
                    run.FinalSession.State.RepetitionHistory.ObservationCount,
                ReservedDraw = runtime.ClosedWindowResources.TurnReservedDraw,
                ReservedQi = runtime.ClosedWindowResources.TurnReservedQi,
                Soul = runtime.ClosedWindowResources.Soul,
                DeferredChoices = runtime.ClosedWindowResources.DeferredPlayerChoices
                    .Select(choice => choice.Id).ToArray(),
                FirstUseFlags = runtime.ClosedWindowResources.FirstUseFlags
                    .Select(pair => $"{pair.Key}={Flag(pair.Value)}").ToArray(),
                CounterattackGaugeUnits = runtime.CounterattackState.GaugeUnits,
                CounterattackPending = runtime.CounterattackState.Pending,
                SacrificeRemainder = runtime.CounterattackState.SacrificeStoneRemainder,
            },
            Terminal = new TemporaryLibertyGoldenTerminal
            {
                IsTerminal = run.FinalSession.State.IsTerminal,
                Outcome = run.FinalSession.State.OutcomeId,
                EndReason = run.FinalSession.State.EndReasonId,
            },
            MomentumEventCount = 0,
            BrilliantEventCount = 0,
            CounterattackFixtureCoverageClaimCount = 0,
        };
    }

    private static void AssertAuthoritative(HeadlessBattleSession session)
    {
        if (session.State.AuthoritativeRuntime is null ||
            !StringComparer.Ordinal.Equals(
                session.State.StateProjectionId,
                StateProjectionId))
        {
            throw new InvalidOperationException(
                "TLE golden execution left the authoritative v2 state projection.");
        }
    }

    private static JsonDocument OpenJson(string relativePath) =>
        JsonDocument.Parse(File.ReadAllBytes(RepositoryFilePath(relativePath)));

    private static JsonElement RequiredEntry(
        IReadOnlyDictionary<string, JsonElement> entries,
        string id) => entries.TryGetValue(id, out var entry)
        ? entry
        : throw new InvalidDataException($"Required content entry '{id}' is missing.");

    private static string RepositoryFilePath(string relativePath)
    {
        var root = FindRepositoryRoot();
        var fullPath = Path.GetFullPath(Path.Combine(root.FullName, relativePath));
        var relative = Path.GetRelativePath(root.FullName, fullPath);
        if (Path.IsPathRooted(relative) ||
            relative.Equals("..", StringComparison.Ordinal) ||
            relative.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"TLE golden path escapes the repository: '{relativePath}'.");
        }

        return fullPath;
    }

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
            "Could not find Igorogue.sln from the test output path.");
    }

    private static TemporaryLibertyGoldenPoint ParsePoint(
        JsonElement element,
        string label)
    {
        var values = element.EnumerateArray().Select(value => value.GetInt32()).ToArray();
        return values.Length == 2
            ? new TemporaryLibertyGoldenPoint(values[0], values[1])
            : throw new InvalidDataException($"{label} must have two coordinates.");
    }

    private static string RequiredString(
        JsonElement element,
        string propertyName,
        string label) => element.GetProperty(propertyName).GetString()
        ?? throw new InvalidDataException($"{label} is null.");

    private static int ParseNumericFixtureId(string id)
    {
        if (!id.StartsWith("TLE-", StringComparison.Ordinal) ||
            !int.TryParse(id.AsSpan(4), NumberStyles.None, CultureInfo.InvariantCulture, out var value) ||
            value is < 1 or > 15)
        {
            throw new InvalidDataException($"Invalid TLE fixture ID '{id}'.");
        }

        return value;
    }

    private static int ParseTrailingPositiveInt(string value)
    {
        var separator = value.LastIndexOf('_');
        if (separator < 0 ||
            !int.TryParse(
                value.AsSpan(separator + 1),
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var amount) ||
            amount <= 0)
        {
            throw new InvalidDataException(
                $"Content effect '{value}' has no positive trailing amount.");
        }

        return amount;
    }

    private static string AppliedResource(
        string resource,
        string triggerId,
        string eventId,
        int before,
        int after,
        int delta) =>
        $"{resource}_changed|trigger={triggerId}|event={eventId}|" +
        $"before={Invariant(before)}|after={Invariant(after)}|delta={Invariant(delta)}";

    private static string PrimeReasonId(CounterattackPrimeReason reason) => reason switch
    {
        CounterattackPrimeReason.ThresholdCrossed => "threshold_crossed",
        CounterattackPrimeReason.OverflowAfterPendingConsumption =>
            "overflow_after_pending_consumption",
        _ => throw new InvalidOperationException("Unknown counterattack prime reason."),
    };

    private static string ColorId(StoneColor color) => color switch
    {
        StoneColor.Black => "black",
        StoneColor.White => "white",
        _ => throw new InvalidOperationException("Unknown stone color."),
    };

    private static string TerritorySourceId(
        TerritoryEstablishmentSourceKind sourceKind) => sourceKind switch
    {
        TerritoryEstablishmentSourceKind.Placement => "placement",
        TerritoryEstablishmentSourceKind.TemporaryLibertyExpiry =>
            "temporary_liberty_expiry",
        _ => throw new InvalidOperationException(
            "Unknown territory-establishment source kind."),
    };

    private static string OutcomeId(BattleOutcome outcome) => outcome switch
    {
        BattleOutcome.Ongoing => "ongoing",
        BattleOutcome.PlayerVictory => "win",
        BattleOutcome.PlayerDefeat => "loss",
        _ => throw new InvalidOperationException("Unknown battle outcome."),
    };

    private static string PointId(CanonicalPoint point) =>
        $"{Invariant(point.X)},{Invariant(point.Y)}";

    private static string Invariant(int value) =>
        value.ToString(CultureInfo.InvariantCulture);

    private static string Invariant(long value) =>
        value.ToString(CultureInfo.InvariantCulture);

    private static int Flag(bool value) => value ? 1 : 0;
}

internal sealed record TemporaryLibertyGoldenRunResult(
    TemporaryLibertyGoldenSourceCase Source,
    BattleAuthoritativeInitialSnapshot InitialSnapshot,
    HeadlessBattleSession InitialSession,
    IReadOnlyList<BattleCommandResult> CommandResults,
    HeadlessBattleSession FinalSession,
    IReadOnlyList<TemporaryLibertyGoldenActualBoundary> Boundaries,
    IReadOnlyList<string> ProjectedFacts,
    IReadOnlyList<string> StageTrace);

internal sealed record TemporaryLibertyGoldenActualBoundary(
    string CommandType,
    int CommandSchemaVersion,
    string CanonicalPayload,
    bool Accepted,
    string Reason,
    string StateChecksum,
    string LogChecksum,
    IReadOnlyList<string> OrderedFacts);

internal sealed record TemporaryLibertyGoldenSourceCase(
    string Id,
    int NumericId,
    string Operation,
    string Title,
    int EnemyTurnIndex,
    IReadOnlyList<string> Board,
    IReadOnlyList<TemporaryLibertyGoldenStoneOverride> StoneOverrides,
    IReadOnlyList<TemporaryLibertyGoldenEffect> Effects,
    IReadOnlyList<TemporaryLibertyGoldenContinuousModifier> ContinuousModifiers,
    IReadOnlyList<string> EquippedSeals,
    IReadOnlyList<string> EquippedRelics,
    string? StyleId,
    bool ArmedCaptureChain,
    bool StyleFirstCaptureUsed,
    bool SealFirstCaptureUsed,
    int StartSacrificeRemainder,
    int? StartCounterattackUnits,
    int? ExplicitKomi,
    bool PendingAtEnemyTurnStart,
    bool ResultTopologySeenBefore);

internal sealed record TemporaryLibertyGoldenPoint(int X, int Y)
{
    internal CanonicalPoint ToDomain(BoardGeometry geometry) =>
        geometry.CreateCanonicalPoint(X, Y);
}

internal sealed record TemporaryLibertyGoldenStoneOverride(
    TemporaryLibertyGoldenPoint Point,
    string InstanceId,
    string KindId);

internal sealed record TemporaryLibertyGoldenEffect(
    string Id,
    TemporaryLibertyGoldenPoint AnchorPoint,
    int Amount,
    long CreatedSequence,
    int ExpiresAfterEnemyTurnIndex,
    string SourceId)
{
    internal TemporaryLibertyEffect ToDomain(StoneRuntimeState stones)
    {
        var anchor = stones.InstanceAt(AnchorPoint.ToDomain(stones.SourceBoard.Geometry))
            ?? throw new InvalidDataException($"Effect {Id} has no anchor stone.");
        return new TemporaryLibertyEffect(
            Id,
            Amount,
            anchor.Color,
            anchor.InstanceId,
            SourceId,
            CreatedSequence,
            ExpiresAfterEnemyTurnIndex);
    }
}

internal sealed record TemporaryLibertyGoldenContinuousModifier(
    string Id,
    TemporaryLibertyGoldenPoint AnchorPoint,
    int Amount,
    string SourceId)
{
    internal ContinuousLibertyModifier ToDomain(StoneRuntimeState stones)
    {
        var anchor = stones.InstanceAt(AnchorPoint.ToDomain(stones.SourceBoard.Geometry))
            ?? throw new InvalidDataException($"Modifier {Id} has no anchor stone.");
        return new ContinuousLibertyModifier(
            Id,
            Amount,
            anchor.Color,
            anchor.InstanceId,
            SourceId);
    }
}

internal sealed record TemporaryLibertyContentOperation(string Kind, int Amount);

internal sealed record TemporaryLibertyCaptureChainArm(int Qi, int Draw);

internal sealed record TemporaryLibertyPlanAndFlags(
    CaptureBenefitTriggerPlan Plan,
    IReadOnlyList<KeyValuePair<string, bool>> FirstUseFlags);

internal sealed record TemporaryLibertyRuntimeContent(
    int BoardSize,
    BattleRuntimePolicy RuntimePolicy,
    int CounterattackThresholdUnits,
    int CounterattackStartBaseUnits,
    int CounterattackStartPerKomiUnits,
    int CounterattackNaturalBaseUnits,
    int CounterattackNaturalPerKomiUnits,
    int SacrificeStonesPerBatch,
    int SacrificeUnitsPerBatch,
    IReadOnlyDictionary<string, IReadOnlyList<TemporaryLibertyContentOperation>>
        OnCapturedOperationsByStoneKind,
    TemporaryLibertyCaptureChainArm CaptureChainArm,
    IReadOnlyDictionary<string, IReadOnlySet<string>> StyleRulesById,
    IReadOnlyDictionary<string, string> SealEffectsById,
    IReadOnlyDictionary<string, int> SealKomiById,
    IReadOnlyDictionary<string, string> RelicEffectsById,
    IReadOnlyDictionary<string, string> BoardConditionEffectsById)
{
    internal IReadOnlySet<string> StyleRules(string id) =>
        StyleRulesById.TryGetValue(id, out var value)
            ? value
            : throw new InvalidDataException($"Unknown TLE style '{id}'.");

    internal string SealEffect(string id) =>
        SealEffectsById.TryGetValue(id, out var value)
            ? value
            : throw new InvalidDataException($"Unknown TLE seal '{id}'.");

    internal int SealKomi(string id) =>
        SealKomiById.TryGetValue(id, out var value)
            ? value
            : throw new InvalidDataException($"Unknown TLE seal '{id}'.");

    internal string RelicEffect(string id) =>
        RelicEffectsById.TryGetValue(id, out var value)
            ? value
            : throw new InvalidDataException($"Unknown TLE relic '{id}'.");

    internal string BoardConditionEffect(string id) =>
        BoardConditionEffectsById.TryGetValue(id, out var value)
            ? value
            : throw new InvalidDataException($"Unknown TLE board condition '{id}'.");
}

internal sealed record TemporaryLibertyGoldenCatalog
{
    [JsonPropertyName("schema_id")]
    public required string SchemaId { get; init; }

    [JsonPropertyName("schema_version")]
    public required int SchemaVersion { get; init; }

    [JsonPropertyName("state_projection")]
    public required string StateProjection { get; init; }

    [JsonPropertyName("fact_projection")]
    public required string FactProjection { get; init; }

    [JsonPropertyName("game_version")]
    public required string GameVersion { get; init; }

    [JsonPropertyName("content_hash")]
    public required string ContentHash { get; init; }

    [JsonPropertyName("source_catalogs")]
    public required IReadOnlyList<TemporaryLibertyGoldenSourceCatalog> SourceCatalogs
    { get; init; }

    [JsonPropertyName("claims")]
    public required TemporaryLibertyGoldenClaims Claims { get; init; }

    [JsonPropertyName("cases")]
    public required IReadOnlyList<TemporaryLibertyGoldenCase> Cases { get; init; }
}

internal sealed record TemporaryLibertyGoldenSourceCatalog
{
    [JsonPropertyName("path")]
    public required string Path { get; init; }

    [JsonPropertyName("sha256")]
    public required string Sha256 { get; init; }
}

internal sealed record TemporaryLibertyGoldenClaims
{
    [JsonPropertyName("momentum_event_count")]
    public required int MomentumEventCount { get; init; }

    [JsonPropertyName("brilliant_event_count")]
    public required int BrilliantEventCount { get; init; }

    [JsonPropertyName("counterattack_fixture_coverage_claim_count")]
    public required int CounterattackFixtureCoverageClaimCount { get; init; }
}

internal sealed record TemporaryLibertyGoldenCase
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("source_fixture_id")]
    public required string SourceFixtureId { get; init; }

    [JsonPropertyName("source_operation")]
    public required string SourceOperation { get; init; }

    [JsonPropertyName("adapter")]
    public required string Adapter { get; init; }

    [JsonPropertyName("seed")]
    public required long Seed { get; init; }

    [JsonPropertyName("initial")]
    public required TemporaryLibertyGoldenInitial Initial { get; init; }

    [JsonPropertyName("commands")]
    public required IReadOnlyList<TemporaryLibertyGoldenCommand> Commands { get; init; }

    [JsonPropertyName("stage_trace")]
    public required IReadOnlyList<string> StageTrace { get; init; }

    [JsonPropertyName("final")]
    public required TemporaryLibertyGoldenFinal Final { get; init; }

    [JsonPropertyName("terminal")]
    public required TemporaryLibertyGoldenTerminal Terminal { get; init; }

    [JsonPropertyName("momentum_event_count")]
    public required int MomentumEventCount { get; init; }

    [JsonPropertyName("brilliant_event_count")]
    public required int BrilliantEventCount { get; init; }

    [JsonPropertyName("counterattack_fixture_coverage_claim_count")]
    public required int CounterattackFixtureCoverageClaimCount { get; init; }
}

internal sealed record TemporaryLibertyGoldenInitial
{
    [JsonPropertyName("snapshot_checksum")]
    public required string SnapshotChecksum { get; init; }

    [JsonPropertyName("state_checksum")]
    public required string StateChecksum { get; init; }

    [JsonPropertyName("log_checksum")]
    public required string LogChecksum { get; init; }
}

internal sealed record TemporaryLibertyGoldenCommand
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("schema_version")]
    public required int SchemaVersion { get; init; }

    [JsonPropertyName("canonical_payload")]
    public required string CanonicalPayload { get; init; }

    [JsonPropertyName("accepted")]
    public required bool Accepted { get; init; }

    [JsonPropertyName("reason")]
    public required string Reason { get; init; }

    [JsonPropertyName("state_checksum")]
    public required string StateChecksum { get; init; }

    [JsonPropertyName("log_checksum")]
    public required string LogChecksum { get; init; }

    [JsonPropertyName("ordered_facts")]
    public required IReadOnlyList<string> OrderedFacts { get; init; }
}

internal sealed record TemporaryLibertyGoldenFinal
{
    [JsonPropertyName("state_checksum")]
    public required string StateChecksum { get; init; }

    [JsonPropertyName("log_checksum")]
    public required string LogChecksum { get; init; }

    [JsonPropertyName("temporary_effect_ids")]
    public required IReadOnlyList<string> TemporaryEffectIds { get; init; }

    [JsonPropertyName("topology_observation_count")]
    public required int TopologyObservationCount { get; init; }

    [JsonPropertyName("reserved_draw")]
    public required int ReservedDraw { get; init; }

    [JsonPropertyName("reserved_qi")]
    public required int ReservedQi { get; init; }

    [JsonPropertyName("soul")]
    public required int Soul { get; init; }

    [JsonPropertyName("deferred_choices")]
    public required IReadOnlyList<string> DeferredChoices { get; init; }

    [JsonPropertyName("first_use_flags")]
    public required IReadOnlyList<string> FirstUseFlags { get; init; }

    [JsonPropertyName("counterattack_gauge_units")]
    public required int CounterattackGaugeUnits { get; init; }

    [JsonPropertyName("counterattack_pending")]
    public required bool CounterattackPending { get; init; }

    [JsonPropertyName("sacrifice_remainder")]
    public required int SacrificeRemainder { get; init; }
}

internal sealed record TemporaryLibertyGoldenTerminal
{
    [JsonPropertyName("is_terminal")]
    public required bool IsTerminal { get; init; }

    [JsonPropertyName("outcome")]
    public required string Outcome { get; init; }

    [JsonPropertyName("end_reason")]
    public required string EndReason { get; init; }
}
