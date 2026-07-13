using System.Reflection;
using System.Xml.Linq;
using Igorogue.Application.Battle;
using Igorogue.Application.Bootstrap;
using Igorogue.Application.Replay;
using Igorogue.Content;
using Igorogue.Domain.Board;
using Igorogue.Domain.Bootstrap;
using Igorogue.Domain.Cards;
using Igorogue.Domain.Combat;
using Igorogue.Domain.Content;
using Igorogue.Domain.Determinism;
using Igorogue.Domain.Enemies;
using Igorogue.Domain.Facilities;

namespace Igorogue.Architecture.Tests;

public sealed class ArchitectureBoundaryTests
{
    [Fact]
    public void DomainAssemblyDoesNotReferenceGodot()
    {
        var references = typeof(BootstrapState).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .ToArray();

        Assert.DoesNotContain(references, name => name.StartsWith("Godot", StringComparison.Ordinal));
    }

    [Fact]
    public void ApplicationAssemblyDoesNotReferenceGodot()
    {
        var references = typeof(BootstrapApplicationService).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .ToArray();

        Assert.DoesNotContain(references, name => name.StartsWith("Godot", StringComparison.Ordinal));
    }

    [Fact]
    public void ContentAssemblyDoesNotReferenceGodot()
    {
        var references = typeof(ContentManifestLoader).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .ToArray();

        Assert.DoesNotContain(references, name => name.StartsWith("Godot", StringComparison.Ordinal));
    }

    [Fact]
    public void ContentAssemblyReferencesOnlyTheApprovedProductionLayer()
    {
        var productionReferences = typeof(CoreDuelContentCatalogLoader).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .Where(name => name.StartsWith("Igorogue.", StringComparison.Ordinal))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(new[] { "Igorogue.Domain" }, productionReferences);
    }

    [Fact]
    public void DomainContentDefinitionsExposeNoHostOrJsonTypes()
    {
        var offenders = typeof(CoreDuelContentCatalog).Assembly
            .GetExportedTypes()
            .Where(type => type.Namespace == typeof(CoreDuelContentCatalog).Namespace)
            .SelectMany(type => PublicSurfaceTypes(type).Select(signatureType => (type, signatureType)))
            .SelectMany(pair => ExpandSignatureType(pair.signatureType)
                .Select(expanded => (pair.type, signatureType: expanded)))
            .Where(pair =>
                pair.signatureType.Namespace?.StartsWith("System.IO", StringComparison.Ordinal) == true ||
                pair.signatureType.Namespace?.StartsWith("System.Text.Json", StringComparison.Ordinal) == true ||
                pair.signatureType.Namespace?.StartsWith("Godot", StringComparison.Ordinal) == true ||
                pair.signatureType.Assembly.GetName().Name is "Igorogue.Application" or "Igorogue.Content")
            .ToArray();

        Assert.Empty(offenders);
    }

    [Fact]
    public void PublicDomainApiDoesNotExposeGodotTypes()
    {
        var offenders = typeof(BootstrapState).Assembly
            .GetExportedTypes()
            .SelectMany(type => PublicSignatureTypes(type).Select(signatureType => (type, signatureType)))
            .Where(pair => pair.signatureType.Namespace?.StartsWith("Godot", StringComparison.Ordinal) == true)
            .ToArray();

        Assert.Empty(offenders);
    }

    [Fact]
    public void RawKingCaptureEvaluatorIsNotPublicDomainApi()
    {
        var evaluatorType = typeof(BootstrapState).Assembly.GetType(
            "Igorogue.Domain.Combat.KingCaptureResultEvaluator");

        Assert.NotNull(evaluatorType);
        Assert.False(evaluatorType.IsPublic);
    }

    [Fact]
    public void TerritoryApiAcceptsOnlyTheStoneLayerBoardSnapshot()
    {
        var analyze = Assert.Single(typeof(TerritoryAnalyzer).GetMethods(
            BindingFlags.Public |
            BindingFlags.Static |
            BindingFlags.DeclaredOnly));

        Assert.Equal(nameof(TerritoryAnalyzer.Analyze), analyze.Name);
        Assert.Equal(typeof(TerritoryAnalysis), analyze.ReturnType);
        Assert.Equal(
            new[] { typeof(BoardState) },
            analyze.GetParameters().Select(parameter => parameter.ParameterType));
        Assert.Empty(typeof(TerritoryRegion).GetConstructors());
        Assert.Empty(typeof(TerritoryAnalysis).GetConstructors());
    }

    [Fact]
    public void FacilityRuntimeAnalysisRequiresAllExactSnapshotInputsExplicitly()
    {
        var analyze = Assert.Single(typeof(FacilityRuntimeAnalyzer).GetMethods(
            BindingFlags.Public |
            BindingFlags.Static |
            BindingFlags.DeclaredOnly));

        Assert.Equal(nameof(FacilityRuntimeAnalyzer.Analyze), analyze.Name);
        Assert.Equal(typeof(FacilityRuntimeAnalysis), analyze.ReturnType);
        Assert.Equal(
            new[]
            {
                typeof(FacilityState),
                typeof(TerritoryAnalysis),
                typeof(FacilityRuntimePolicy),
            },
            analyze.GetParameters().Select(parameter => parameter.ParameterType));
        Assert.Empty(typeof(FacilityState).GetConstructors());
        Assert.Empty(typeof(FacilityRuntimeAnalysis).GetConstructors());
    }

    [Fact]
    public void FacilityPlacementIntegrationAcceptsOnlyAnAcceptedPlacementCommit()
    {
        var apply = Assert.Single(typeof(FacilityPlacementIntegrator).GetMethods(
            BindingFlags.Public |
            BindingFlags.Static |
            BindingFlags.DeclaredOnly));

        Assert.Equal(nameof(FacilityPlacementIntegrator.Apply), apply.Name);
        Assert.Equal(typeof(FacilityPlacementCommit), apply.ReturnType);
        Assert.Equal(
            new[] { typeof(FacilityState), typeof(LegalPlacementCommit) },
            apply.GetParameters().Select(parameter => parameter.ParameterType));
        Assert.Empty(typeof(FacilityPlacementCommit).GetConstructors());
        Assert.DoesNotContain(
            typeof(FacilityPlacementCommit).GetProperties(
                BindingFlags.Public | BindingFlags.Instance),
            property => property.PropertyType == typeof(LegalPlacementCommit));
        Assert.True(typeof(ICommittedPlacementFact).IsAssignableFrom(
            typeof(PlacementCaptureFact)));
        Assert.True(typeof(ICommittedPlacementFact).IsAssignableFrom(
            typeof(FacilityDestroyedFact)));
        Assert.True(typeof(ICommittedPlacementFact).IsAssignableFrom(
            typeof(StoneTopologyRegisteredFact)));
        Assert.True(typeof(ICommittedPlacementFact).IsAssignableFrom(
            typeof(KingCaptureEvaluatedFact)));
    }

    [Fact]
    public void BattleFactSeamIncludesPlacementFacilityTerritoryAndLifecycleFacts()
    {
        var factTypes = new[]
        {
            typeof(ICommittedPlacementFact),
            typeof(PlacementCaptureFact),
            typeof(StonePlacedFact),
            typeof(GroupCapturedFact),
            typeof(StoneTopologyRegisteredFact),
            typeof(KingCaptureEvaluatedFact),
            typeof(FacilityFact),
            typeof(FacilityBuiltFact),
            typeof(FacilityActivatedFact),
            typeof(FacilityDisabledFact),
            typeof(FacilityDestroyedFact),
            typeof(TerritoryEstablishedFact),
            typeof(CommandRejectedFact),
            typeof(EnemyPassedFact),
            typeof(BattleEndedFact),
            typeof(TemporaryLibertyGrantedFact),
            typeof(TemporaryLibertyRemovedFact),
            typeof(TemporaryLibertyExpirySweepStartedFact),
            typeof(TemporaryLibertyExpiredFact),
            typeof(TemporaryLibertyGroupCapturedFact),
            typeof(TemporaryLibertyKingGateFact),
            typeof(CaptureBenefitSuppressedFact),
            typeof(TemporaryLibertyExpirySweepResolvedFact),
            typeof(CaptureBatchStartedFact),
            typeof(QiChangedFact),
            typeof(CardDrawnFact),
            typeof(TurnReservedDrawChangedFact),
            typeof(TurnReservedQiChangedFact),
            typeof(SoulChangedFact),
            typeof(DeferredPlayerChoiceCreatedFact),
            typeof(FirstUseFlagConsumedFact),
            typeof(SacrificeRemainderChangedFact),
            typeof(SacrificeBatchAdvancedFact),
            typeof(CounterattackAdvancedFact),
            typeof(CounterattackPendingPrimedFact),
            typeof(CounterattackPendingConsumedFact),
            typeof(CaptureBatchResolvedFact),
            typeof(EnemyIntentPlannedFact),
            typeof(EnemyIntentRetargetedFact),
            typeof(EnemyActionStartedFact),
            typeof(EnemyActionResolvedFact),
        };

        Assert.All(factTypes, type =>
            Assert.True(
                typeof(IBattleFact).IsAssignableFrom(type),
                $"{type.FullName} must cross the battle fact seam."));
    }

    [Fact]
    public void TerritoryDeltaResolutionRequiresBoundAnalysesAndReturnsANonForgeableFact()
    {
        var resolve = RequirePublicStaticMethod(
            typeof(TerritoryDeltaResolver),
            nameof(TerritoryDeltaResolver.Resolve),
            typeof(TerritoryAnalysis),
            typeof(TerritoryAnalysis),
            typeof(FacilityPlacementCommit),
            typeof(StoneColor));

        Assert.Equal(typeof(TerritoryEstablishedFact), resolve.ReturnType);
        var resolveAfterExpiry = RequirePublicStaticMethod(
            typeof(TerritoryDeltaResolver),
            nameof(TerritoryDeltaResolver.ResolveAfterExpiry),
            typeof(TerritoryAnalysis),
            typeof(TemporaryLibertyExpiryResolution));
        Assert.Equal(typeof(TerritoryEstablishedFact), resolveAfterExpiry.ReturnType);
        Assert.Empty(typeof(TerritoryEstablishedFact).GetConstructors(
            BindingFlags.Public | BindingFlags.Instance));
    }

    [Fact]
    public void TemporaryLibertyResolutionRequiresExactDomainSnapshotsAndCannotForgeCommits()
    {
        var resolve = RequirePublicStaticMethod(
            typeof(TemporaryLibertyExpiryResolver),
            nameof(TemporaryLibertyExpiryResolver.Resolve),
            typeof(StoneRuntimeState),
            typeof(TemporaryLibertyState),
            typeof(ContinuousLibertySnapshot),
            typeof(BattleRepetitionHistory),
            typeof(int));

        Assert.Equal(typeof(TemporaryLibertyExpiryResolution), resolve.ReturnType);
        Assert.Empty(typeof(StoneRuntimeState).GetConstructors());
        Assert.Empty(typeof(TemporaryLibertyState).GetConstructors());
        Assert.Empty(typeof(ContinuousLibertySnapshot).GetConstructors());
        Assert.Empty(typeof(TemporaryLibertyExpiryResolution).GetConstructors());
        Assert.Empty(typeof(TemporaryLibertyGrantResolution).GetConstructors());
        var integratePlacement = RequirePublicStaticMethod(
            typeof(StoneRuntimePlacementIntegrator),
            nameof(StoneRuntimePlacementIntegrator.Apply),
            typeof(StoneRuntimeState),
            typeof(TemporaryLibertyState),
            typeof(LegalPlacementCommit),
            typeof(StoneRuntimePlacementDescriptor),
            typeof(TemporaryLibertyEffectiveLibertyAnalysis),
            typeof(TemporaryLibertyEffectiveLibertyAnalysis));
        Assert.Equal(typeof(StoneRuntimePlacementCommit), integratePlacement.ReturnType);
        Assert.Empty(typeof(StoneRuntimePlacementCommit).GetConstructors());
        Assert.DoesNotContain(
            typeof(StoneRuntimePlacementCommit).GetProperties(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static),
            property => property.PropertyType == typeof(LegalPlacementCommit));
        Assert.DoesNotContain(
            typeof(StoneRuntimePlacementCommit).Assembly.GetExportedTypes(),
            type => type.Name.Contains(
                "TemporaryLibertyCarrierRemoval",
                StringComparison.Ordinal));
        Assert.DoesNotContain(
            typeof(BattleRepetitionHistory).GetMethods(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static),
            method => method.Name.Contains("Mandatory", StringComparison.Ordinal));
        Assert.DoesNotContain(
            typeof(TemporaryLibertyState).GetMethods(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static),
            method => method.Name.Contains("BeginExpirySweep", StringComparison.Ordinal));
        var grant = RequirePublicStaticMethod(
            typeof(TemporaryLibertyGrantResolver),
            nameof(TemporaryLibertyGrantResolver.Grant),
            typeof(TemporaryLibertyState),
            typeof(CanonicalPoint),
            typeof(string),
            typeof(int),
            typeof(string),
            typeof(int));
        Assert.Equal(typeof(TemporaryLibertyGrantResolution), grant.ReturnType);
        Assert.Single(
            typeof(TemporaryLibertyGrantResolver).GetMethods(
                BindingFlags.Public | BindingFlags.Static),
            method => method.Name == nameof(TemporaryLibertyGrantResolver.Grant));
        var grantAfterSweep = RequirePublicStaticMethod(
            typeof(TemporaryLibertyGrantResolver),
            nameof(TemporaryLibertyGrantResolver.GrantAfterExpirySweepStarted),
            typeof(TemporaryLibertyState),
            typeof(CanonicalPoint),
            typeof(string),
            typeof(int),
            typeof(string),
            typeof(TemporaryLibertyExpirySweepWindow));
        Assert.Equal(typeof(TemporaryLibertyGrantResolution), grantAfterSweep.ReturnType);
        Assert.Empty(typeof(TemporaryLibertyExpirySweepWindow).GetConstructors());
    }

    [Fact]
    public void ClosedWindowCapturePipelineUsesTypedInputsAndNonForgeableResults()
    {
        var createBatch = RequirePublicStaticMethod(
            typeof(CaptureBatch),
            nameof(CaptureBatch.Create),
            typeof(string),
            typeof(string),
            typeof(CaptureBoundary),
            typeof(int?),
            typeof(CapturingWindow),
            typeof(StoneRuntimeState),
            typeof(IEnumerable<StoneGroup>));
        Assert.Equal(typeof(CaptureBatch), createBatch.ReturnType);
        var resolve = RequirePublicStaticMethod(
            typeof(ClosedWindowCaptureBenefitResolver),
            nameof(ClosedWindowCaptureBenefitResolver.Resolve),
            typeof(CaptureBatch),
            typeof(ClosedWindowResourceState),
            typeof(CounterattackBoundaryState),
            typeof(CounterattackBoundaryPolicy),
            typeof(IEnumerable<CaptureBenefitTrigger>));
        Assert.Equal(typeof(ClosedWindowCaptureBenefitResolution), resolve.ReturnType);
        var resolvePlacement = RequirePublicStaticMethod(
            typeof(ClosedWindowCaptureBenefitResolver),
            nameof(ClosedWindowCaptureBenefitResolver.ResolvePlacement),
            typeof(CaptureBatch),
            typeof(ClosedWindowResourceState),
            typeof(CounterattackBoundaryState),
            typeof(CounterattackBoundaryPolicy),
            typeof(IEnumerable<CaptureBenefitTrigger>));
        Assert.Equal(
            typeof(ClosedWindowCaptureBenefitResolution),
            resolvePlacement.ReturnType);
        var createConditionalPlan = RequirePublicStaticMethod(
            typeof(CaptureBenefitTriggerPlan),
            nameof(CaptureBenefitTriggerPlan.CreateConditional),
            typeof(IEnumerable<CaptureBenefitTriggerPlanEntry>));
        Assert.Equal(typeof(CaptureBenefitTriggerPlan), createConditionalPlan.ReturnType);
        var selectFor = RequirePublicInstanceMethod(
            typeof(CaptureBenefitTriggerPlan),
            nameof(CaptureBenefitTriggerPlan.SelectFor),
            typeof(CaptureBatch));
        Assert.Equal(typeof(IReadOnlyList<CaptureBenefitTrigger>), selectFor.ReturnType);
        var triggerConstructor = Assert.Single(typeof(CaptureBenefitTrigger).GetConstructors());
        Assert.Equal(
            new[]
            {
                typeof(CaptureBenefitSource),
                typeof(string),
                typeof(IEnumerable<string>),
                typeof(IEnumerable<CaptureBenefitOperation>),
                typeof(string),
            },
            triggerConstructor.GetParameters().Select(parameter => parameter.ParameterType));
        Assert.DoesNotContain(
            typeof(CaptureBatch).Assembly.GetExportedTypes(),
            type => type.Name == "CaptureBenefitOrderKey");

        Assert.Empty(typeof(CaptureBatch).GetConstructors());
        Assert.Empty(typeof(CapturedGroup).GetConstructors());
        Assert.Empty(typeof(CaptureBenefitSource).GetConstructors());
        Assert.Empty(typeof(ClosedWindowResourceState).GetConstructors());
        Assert.Empty(typeof(ClosedWindowCaptureBenefitResolution).GetConstructors());
        Assert.Empty(typeof(CounterattackBoundaryState).GetConstructors());
        Assert.Empty(typeof(CounterattackBoundaryTransition).GetConstructors());
        Assert.Empty(typeof(CounterattackPendingAtStartSnapshot).GetConstructors());
        var createCounterattack = RequirePublicStaticMethod(
            typeof(CounterattackBoundaryState),
            nameof(CounterattackBoundaryState.Create),
            typeof(int),
            typeof(bool),
            typeof(int),
            typeof(CounterattackBoundaryPolicy));
        Assert.Equal(typeof(CounterattackBoundaryState), createCounterattack.ReturnType);
        Assert.All(
            new[]
            {
                typeof(CaptureBatchStartedFact),
                typeof(TurnReservedDrawChangedFact),
                typeof(TurnReservedQiChangedFact),
                typeof(SoulChangedFact),
                typeof(DeferredPlayerChoiceCreatedFact),
                typeof(FirstUseFlagConsumedFact),
                typeof(SacrificeRemainderChangedFact),
                typeof(SacrificeBatchAdvancedFact),
                typeof(CounterattackAdvancedFact),
                typeof(CounterattackPendingPrimedFact),
                typeof(CounterattackPendingConsumedFact),
                typeof(CaptureBatchResolvedFact),
            },
            type => Assert.Empty(type.GetConstructors()));
    }

    [Fact]
    public void CounterattackBoundaryExposesOnlyTheTask0028Operations()
    {
        var methods = typeof(CounterattackBoundaryResolver)
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .OrderBy(method => method.Name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            [
                nameof(CounterattackBoundaryResolver.AdvanceEnemyTurnEnd),
                nameof(CounterattackBoundaryResolver.AdvanceSacrifice),
                nameof(CounterattackBoundaryResolver.ConsumeAndReprimeOnce),
                nameof(CounterattackBoundaryResolver.SnapshotPendingAtEnemyTurnStart),
            ],
            methods.Select(method => method.Name));
        Assert.All(
            methods.Where(method =>
                method.Name != nameof(
                    CounterattackBoundaryResolver.SnapshotPendingAtEnemyTurnStart)),
            method => Assert.Equal(
                typeof(CounterattackBoundaryTransition),
                method.ReturnType));
        Assert.Equal(
            typeof(CounterattackPendingAtStartSnapshot),
            RequirePublicStaticMethod(
                typeof(CounterattackBoundaryResolver),
                nameof(CounterattackBoundaryResolver.SnapshotPendingAtEnemyTurnStart),
                typeof(CounterattackBoundaryState),
                typeof(CounterattackBoundaryPolicy)).ReturnType);
    }

    [Fact]
    public void Task0028CombatKernelDoesNotInterpretContentIdsOrClaimDeferredSystems()
    {
        var root = FindRepositoryRoot();
        var task0028Root = Path.Combine(
            root.FullName,
            "src",
            "Igorogue.Domain",
            "Combat");
        var sourceText = string.Join(
            '\n',
            Directory.EnumerateFiles(task0028Root, "*.cs", SearchOption.AllDirectories)
                .OrderBy(path => path, StringComparer.Ordinal)
                .Select(File.ReadAllText));

        Assert.All(
            new[]
            {
                "style_sacrifice",
                "seal_bone",
                "stone_lure",
                "stone_blood",
                "lure",
                "blood",
                "relic_hungry_furnace",
                "seal_sacrifice",
                "capture_chain",
                "MOM-",
                "CTR-",
                "CounterattackIntent",
                "Overextension",
                "Brilliant",
            },
            forbidden => Assert.DoesNotContain(
                forbidden,
                sourceText,
                StringComparison.Ordinal));
    }

    [Fact]
    public void Task0033UsesAnExplicitRecipeAndRemainsOutsideBattleReplayV2()
    {
        var createDeck = RequirePublicStaticMethod(
            typeof(BattleDeckState),
            nameof(BattleDeckState.CreateShuffled),
            typeof(IEnumerable<BattleCardInstance>),
            typeof(AuthoritativeRngState));
        var startBattle = RequirePublicStaticMethod(
            typeof(CoreDuelCardTurnKernel),
            nameof(CoreDuelCardTurnKernel.StartBattle),
            typeof(IEnumerable<BattleCardInstance>),
            typeof(AuthoritativeRngState),
            typeof(CoreDuelSystemPolicy),
            typeof(ClosedWindowResourceState),
            typeof(IEnumerable<KeyValuePair<string, bool>>));

        Assert.Equal(typeof(BattleDeckInitialization), createDeck.ReturnType);
        Assert.Equal(typeof(CoreDuelCardTurnState), startBattle.ReturnType);
        Assert.Empty(typeof(BattleDeckState).GetConstructors());
        Assert.Empty(typeof(CoreDuelCardTurnState).GetConstructors());
        Assert.Empty(typeof(CoreDuelCardTurnTransition).GetConstructors());

        var replaySurface = typeof(BattleReplayDocumentV2).Assembly
            .GetExportedTypes()
            .Where(type => type.Namespace == typeof(BattleReplayDocumentV2).Namespace)
            .SelectMany(PublicSurfaceTypes)
            .SelectMany(ExpandSignatureType)
            .ToArray();
        Assert.DoesNotContain(typeof(CoreDuelCardTurnState), replaySurface);
        Assert.DoesNotContain(
            typeof(BattleState).GetProperties(
                BindingFlags.Public | BindingFlags.Instance),
            property => property.PropertyType == typeof(CoreDuelCardTurnState));
    }

    [Fact]
    public void Task0033ProductionKernelDoesNotSelectAContentRecipe()
    {
        var root = FindRepositoryRoot();
        var sourcePaths = Directory
            .EnumerateFiles(
                Path.Combine(root.FullName, "src", "Igorogue.Domain", "Cards"),
                "*.cs",
                SearchOption.AllDirectories)
            .Append(Path.Combine(
                root.FullName,
                "src",
                "Igorogue.Application",
                "Battle",
                "CoreDuelCardTurnState.cs"));
        var sourceText = string.Join(
            '\n',
            sourcePaths.OrderBy(path => path, StringComparer.Ordinal).Select(File.ReadAllText));

        Assert.All(
            new[]
            {
                "card_basic_stone",
                "card_extend",
                "card_contact",
                "card_reinforce",
                "card_development",
                "card_lure_stone",
                "card_one_space_jump",
                "card_edge_crawl",
                "card_infiltrate",
                "card_blood_stone",
                "card_seed_stone",
                "card_furnace",
                "card_market",
                "card_capture_chain",
                "card_large_framework",
            },
            contentId => Assert.DoesNotContain(contentId, sourceText, StringComparison.Ordinal));
        Assert.DoesNotContain("game_data/", sourceText, StringComparison.Ordinal);
    }

    [Fact]
    public void Task0034Through0036PlayCardUsesAStandaloneCommandSessionAndSharedBattleFacts()
    {
        var start = RequirePublicStaticMethod(
            typeof(CoreDuelCardPlayStateMachine),
            nameof(CoreDuelCardPlayStateMachine.Start),
            typeof(BattleAuthoritativeInitialSnapshot),
            typeof(CoreDuelCardTurnState),
            typeof(StarterStoneCardPlayCatalog),
            typeof(StarterReinforceCardPlayDefinition),
            typeof(ReplayMetadata));
        var execute = RequirePublicStaticMethod(
            typeof(CoreDuelCardPlayStateMachine),
            nameof(CoreDuelCardPlayStateMachine.Execute),
            typeof(CoreDuelCardPlaySession),
            typeof(PlayCardCommand));

        Assert.Equal(typeof(CoreDuelCardPlaySession), start.ReturnType);
        Assert.Equal(typeof(CoreDuelCardPlayResult), execute.ReturnType);
        Assert.True(typeof(IBattleCommand).IsAssignableFrom(typeof(PlayCardCommand)));
        Assert.True(typeof(IBattleFact).IsAssignableFrom(typeof(QiChangedFact)));
        Assert.True(typeof(IBattleFact).IsAssignableFrom(typeof(CardDrawnFact)));
        Assert.True(typeof(IBattleFact).IsAssignableFrom(typeof(TemporaryLibertyGrantedFact)));
        Assert.Contains(
            typeof(CoreDuelCardPlayState).GetProperties(),
            property => property.PropertyType == typeof(StarterStoneCardPlayCatalog));
        Assert.Contains(
            typeof(CoreDuelCardPlayState).GetProperties(),
            property => property.PropertyType == typeof(StarterReinforceCardPlayDefinition));
        Assert.Contains(
            typeof(CoreDuelCardPlayState).GetProperties(),
            property => property.PropertyType == typeof(BattleAuthoritativeRuntimeState));
        Assert.DoesNotContain(
            typeof(PlayCardCommand).GetProperties(),
            property => property.PropertyType == typeof(StarterStoneCardPlayDefinition) ||
                property.PropertyType == typeof(StarterStoneCardPlayCatalog));
        Assert.Empty(typeof(CoreDuelCardPlayState).GetConstructors());
        Assert.Empty(typeof(CoreDuelCardPlaySession).GetConstructors());
        Assert.Empty(typeof(CoreDuelCardPlayResult).GetConstructors());
        Assert.Empty(typeof(QiChangedFact).GetConstructors());
        Assert.Empty(typeof(CardDrawnFact).GetConstructors());
        Assert.Empty(typeof(StarterStoneCardPlayDefinition).GetConstructors());
        Assert.Empty(typeof(StarterStoneCardPlayCatalog).GetConstructors());
        Assert.Empty(typeof(StarterStoneCardPlayEvaluation).GetConstructors());
        Assert.Empty(typeof(StarterReinforceCardPlayDefinition).GetConstructors());
        Assert.Empty(typeof(StarterReinforceCardPlayEvaluation).GetConstructors());
        Assert.Empty(typeof(ReservedDrawCardEffectResolution).GetConstructors());

        var applicationAssembly = typeof(CoreDuelCardPlayStateMachine).Assembly;
        Assert.NotNull(applicationAssembly.GetType(
            "Igorogue.Application.Battle.AuthorizedStonePlacementPipeline"));
        Assert.False(applicationAssembly.GetType(
            "Igorogue.Application.Battle.AuthorizedStonePlacementPipeline")!.IsPublic);
        Assert.NotNull(applicationAssembly.GetType(
            "Igorogue.Application.Battle.AuthorizedRuntimeStonePlacementPipeline"));
        Assert.False(applicationAssembly.GetType(
            "Igorogue.Application.Battle.AuthorizedRuntimeStonePlacementPipeline")!.IsPublic);
        Assert.NotNull(applicationAssembly.GetType(
            "Igorogue.Application.Battle.StarterStonePlacementEffectAnalysis"));
        Assert.False(applicationAssembly.GetType(
            "Igorogue.Application.Battle.StarterStonePlacementEffectAnalysis")!.IsPublic);
    }

    [Fact]
    public void Task0034Through0036DoNotConnectCardPlayToExistingBattleHeadlessOrReplayProjections()
    {
        var forbiddenTypes = new[]
        {
            typeof(CoreDuelCardPlayState),
            typeof(StarterStoneCardPlayCatalog),
            typeof(StarterStoneCardPlayDefinition),
            typeof(StarterReinforceCardPlayDefinition),
        };
        var replaySurface = typeof(BattleReplayDocumentV2).Assembly
            .GetExportedTypes()
            .Where(type => type.Namespace == typeof(BattleReplayDocumentV2).Namespace)
            .SelectMany(PublicSurfaceTypes)
            .SelectMany(ExpandSignatureType)
            .ToArray();

        Assert.All(forbiddenTypes, forbiddenType =>
        {
            Assert.DoesNotContain(forbiddenType, replaySurface);
            Assert.DoesNotContain(
                typeof(BattleState).GetProperties(
                    BindingFlags.Public | BindingFlags.Instance),
                property => property.PropertyType == forbiddenType);
            Assert.DoesNotContain(
                typeof(HeadlessBattleSession).GetProperties(
                    BindingFlags.Public | BindingFlags.Instance),
                property => property.PropertyType == forbiddenType);
        });

        var root = FindRepositoryRoot();
        var productionSourceText = string.Join(
            '\n',
            new[]
            {
                Path.Combine(
                    root.FullName,
                    "src",
                    "Igorogue.Domain",
                    "Cards",
                    "StarterStoneCardPlayDefinition.cs"),
                Path.Combine(
                    root.FullName,
                    "src",
                    "Igorogue.Application",
                    "Battle",
                    "CoreDuelCardPlayState.cs"),
            }.Select(File.ReadAllText));
        Assert.All(
            new[]
            {
                "card_basic_stone",
                "card_extend",
                "card_contact",
                "card_lure_stone",
            },
            contentId => Assert.DoesNotContain(
                contentId,
                productionSourceText,
                StringComparison.Ordinal));
        Assert.DoesNotContain(
            "game_data/",
            productionSourceText,
            StringComparison.Ordinal);

        var existingBattleAndReplaySourceText = string.Join(
            '\n',
            Directory.EnumerateFiles(
                    Path.Combine(root.FullName, "src", "Igorogue.Application", "Replay"),
                    "*.cs",
                    SearchOption.AllDirectories)
                .Concat(Directory.EnumerateFiles(
                    Path.Combine(root.FullName, "src", "Igorogue.Application", "Battle"),
                    "HeadlessBattle*.cs",
                    SearchOption.TopDirectoryOnly))
                .OrderBy(path => path, StringComparer.Ordinal)
                .Select(File.ReadAllText));
        Assert.All(
            new[]
            {
                nameof(CoreDuelCardPlayState),
                nameof(StarterStoneCardPlayCatalog),
                nameof(StarterStoneCardPlayDefinition),
            },
            typeName => Assert.DoesNotContain(
                typeName,
                existingBattleAndReplaySourceText,
                StringComparison.Ordinal));
    }

    [Fact]
    public void Task0037BanditUsesAHighLevelDetachedCommandWithoutChangingReplayV2()
    {
        var start = RequirePublicStaticMethod(
            typeof(BanditEnemyTurnStateMachine),
            nameof(BanditEnemyTurnStateMachine.Start),
            typeof(BattleAuthoritativeInitialSnapshot),
            typeof(EnemyContentDefinition),
            typeof(ReplayMetadata));
        var execute = RequirePublicStaticMethod(
            typeof(BanditEnemyTurnStateMachine),
            nameof(BanditEnemyTurnStateMachine.Execute),
            typeof(BanditEnemyTurnSession),
            typeof(IBattleCommand));

        Assert.Equal(typeof(BanditEnemyTurnStartResult), start.ReturnType);
        Assert.Equal(typeof(BanditEnemyTurnResult), execute.ReturnType);
        Assert.True(typeof(IBattleCommand).IsAssignableFrom(
            typeof(ResolveBanditEnemyActionCommand)));
        Assert.DoesNotContain(
            typeof(ResolveBanditEnemyActionCommand).GetProperties(
                BindingFlags.Public | BindingFlags.Instance),
            property => property.PropertyType == typeof(CanonicalPoint) ||
                property.PropertyType == typeof(StoneColor) ||
                property.PropertyType == typeof(PlacementAccessMode));
        Assert.Empty(typeof(BanditEnemyTurnState).GetConstructors());
        Assert.Empty(typeof(BanditEnemyTurnSession).GetConstructors());
        Assert.Empty(typeof(BanditEnemyTurnResult).GetConstructors());

        var replaySurface = typeof(BattleReplayDocumentV2).Assembly
            .GetExportedTypes()
            .Where(type => type.Namespace == typeof(BattleReplayDocumentV2).Namespace)
            .SelectMany(PublicSurfaceTypes)
            .SelectMany(ExpandSignatureType)
            .ToArray();
        Assert.DoesNotContain(typeof(BanditEnemyTurnState), replaySurface);
        Assert.DoesNotContain(typeof(PlannedEnemyIntent), replaySurface);
        Assert.DoesNotContain(
            typeof(BattleState).GetProperties(
                BindingFlags.Public | BindingFlags.Instance),
            property => property.PropertyType == typeof(BanditEnemyTurnState) ||
                property.PropertyType == typeof(PlannedEnemyIntent));
        Assert.Equal("headless-battle-state-v2", BattleState.AuthoritativeEncodingVersion);
        Assert.Equal(2, BattleReplaySerializerV2.SchemaVersion);
    }

    [Fact]
    public void Task0037EnemyKernelFactsStayInPureDomain()
    {
        Assert.All(
            new[]
            {
                typeof(EnemyIntentPlannedFact),
                typeof(EnemyIntentRetargetedFact),
                typeof(EnemyActionStartedFact),
                typeof(EnemyActionResolvedFact),
            },
            type => Assert.True(typeof(IBattleFact).IsAssignableFrom(type)));

        var domainReferences = typeof(BanditIntentPlanner).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .ToArray();
        Assert.DoesNotContain(
            domainReferences,
            name => name.StartsWith("Igorogue.Application", StringComparison.Ordinal));
        Assert.DoesNotContain(
            domainReferences,
            name => name.StartsWith("Godot", StringComparison.Ordinal));
    }

    [Fact]
    public void FacilityAfterPlacementResolutionRequiresThePlacementCommit()
    {
        var reassociate = RequirePublicStaticMethod(
            typeof(FacilityOperatingTransitionResolver),
            nameof(FacilityOperatingTransitionResolver.ReassociateAfterPlacement),
            typeof(FacilityRuntimeAnalysis),
            typeof(FacilityPlacementCommit),
            typeof(TerritoryAnalysis));

        Assert.Equal(typeof(FacilityOperatingTransition), reassociate.ReturnType);
    }

    [Fact]
    public void HeadlessBattleStateIsCreatedAndAdvancedOnlyThroughTheStateMachine()
    {
        var start = RequirePublicStaticMethod(
            typeof(HeadlessBattleStateMachine),
            nameof(HeadlessBattleStateMachine.Start),
            typeof(BoardState),
            typeof(FacilityState),
            typeof(BattleRuntimePolicy),
            typeof(ReplayMetadata));
        var execute = RequirePublicStaticMethod(
            typeof(HeadlessBattleStateMachine),
            nameof(HeadlessBattleStateMachine.Execute),
            typeof(HeadlessBattleSession),
            typeof(IBattleCommand));

        Assert.Equal(typeof(HeadlessBattleSession), start.ReturnType);
        Assert.Equal(typeof(BattleCommandResult), execute.ReturnType);
        Assert.All(
            new[]
            {
                typeof(BattleState),
                typeof(HeadlessBattleSession),
                typeof(BattleCommandResult),
            },
            type => Assert.Empty(type.GetConstructors(
                BindingFlags.Public | BindingFlags.Instance)));
    }

    [Fact]
    public void AuthoritativeBattleV2UsesAnExactInitialSnapshotAndSeparateReplaySchema()
    {
        var start = RequirePublicStaticMethod(
            typeof(HeadlessBattleStateMachine),
            nameof(HeadlessBattleStateMachine.Start),
            typeof(BattleAuthoritativeInitialSnapshot),
            typeof(ReplayMetadata));

        Assert.Equal(typeof(HeadlessBattleSession), start.ReturnType);
        Assert.Empty(typeof(BattleAuthoritativeInitialSnapshot).GetConstructors());
        Assert.Empty(typeof(BattleAuthoritativeRuntimeState).GetConstructors());
        Assert.Empty(typeof(EnemyTurnBoundaryStageFact).GetConstructors());
        Assert.Equal("headless-battle-state-v1", BattleState.EncodingVersion);
        Assert.Equal(
            "headless-battle-state-v2",
            BattleState.AuthoritativeEncodingVersion);
        Assert.Equal(1, BattleReplaySerializer.SchemaVersion);
        Assert.Equal(2, BattleReplaySerializerV2.SchemaVersion);
        Assert.Equal(BattleReplaySerializer.SchemaId, BattleReplaySerializerV2.SchemaId);
    }

    [Fact]
    public void HeadlessBattlePublicSurfaceDoesNotLeakHostOrAmbientRuntimeApis()
    {
        var battleTypes = typeof(BattleState).Assembly
            .GetExportedTypes()
            .Where(type => type.Namespace == typeof(BattleState).Namespace)
            .Concat(
            [
                typeof(IBattleFact),
                typeof(TerritoryEstablishedFact),
                typeof(TerritoryDeltaResolver),
                typeof(FacilityOperatingTransitionResolver),
                typeof(StoneRuntimeState),
                typeof(TemporaryLibertyState),
                typeof(ContinuousLibertySnapshot),
                typeof(TemporaryLibertyExpiryResolver),
                typeof(TemporaryLibertyExpiryResolution),
                typeof(StoneRuntimePlacementDescriptor),
                typeof(StoneRuntimePlacementCommit),
                typeof(StoneRuntimePlacementIntegrator),
                typeof(CaptureBatch),
                typeof(CaptureBenefitSource),
                typeof(CaptureBenefitTrigger),
                typeof(CaptureBenefitTriggerPlanEntry),
                typeof(CaptureBenefitTriggerPlan),
                typeof(CaptureBenefitOperation),
                typeof(ClosedWindowResourceState),
                typeof(ClosedWindowCaptureBenefitResolver),
                typeof(ClosedWindowCaptureBenefitResolution),
                typeof(CounterattackBoundaryState),
                typeof(CounterattackBoundaryPolicy),
                typeof(CounterattackBoundaryResolver),
                typeof(CounterattackBoundaryTransition),
                typeof(CounterattackPendingAtStartSnapshot),
            ])
            .Distinct()
            .ToArray();

        var offenders = battleTypes
            .SelectMany(type => PublicSurfaceTypes(type)
                .SelectMany(ExpandSignatureType)
                .Select(signatureType => (type, signatureType)))
            .Where(pair => IsHostOrAmbientRuntimeType(pair.signatureType))
            .ToArray();

        Assert.Empty(offenders);
    }

    [Fact]
    public void FacilityEvaluationsCommitsTransitionsAndFactsCannotBeForged()
    {
        var nonForgeableTypes = new[]
        {
            typeof(FacilityRuntimePolicy),
            typeof(FacilityOperatingState),
            typeof(FacilityRegionRuntimeAnalysis),
            typeof(FacilityBuildEvaluation),
            typeof(FacilityBuildCommit),
            typeof(FacilityOperatingTransition),
            typeof(FacilityFact),
            typeof(FacilityBuiltFact),
            typeof(FacilityActivatedFact),
            typeof(FacilityDisabledFact),
            typeof(FacilityDestroyedFact),
            typeof(StoneTopologyRegisteredFact),
            typeof(KingCaptureEvaluatedFact),
            typeof(TemporaryLibertyGrantedFact),
            typeof(TemporaryLibertyRemovedFact),
            typeof(TemporaryLibertyExpirySweepStartedFact),
            typeof(TemporaryLibertyExpiredFact),
            typeof(TemporaryLibertyGroupCapturedFact),
            typeof(TemporaryLibertyKingGateFact),
            typeof(CaptureBenefitSuppressedFact),
            typeof(TemporaryLibertyExpirySweepResolvedFact),
            typeof(CaptureBatchStartedFact),
            typeof(TurnReservedDrawChangedFact),
            typeof(TurnReservedQiChangedFact),
            typeof(SoulChangedFact),
            typeof(DeferredPlayerChoiceCreatedFact),
            typeof(FirstUseFlagConsumedFact),
            typeof(SacrificeRemainderChangedFact),
            typeof(SacrificeBatchAdvancedFact),
            typeof(CounterattackAdvancedFact),
            typeof(CounterattackPendingPrimedFact),
            typeof(CounterattackPendingConsumedFact),
            typeof(CaptureBatchResolvedFact),
            typeof(EnemyTurnBoundaryStageFact),
        };

        Assert.All(nonForgeableTypes, type => Assert.Empty(type.GetConstructors()));
    }

    [Fact]
    public void ProjectReferenceGraphMatchesAcceptedBoundary()
    {
        var root = FindRepositoryRoot();

        AssertProjectReferences(
            Path.Combine(root.FullName, "src/Igorogue.Domain/Igorogue.Domain.csproj"),
            Array.Empty<string>());
        AssertProjectReferences(
            Path.Combine(root.FullName, "src/Igorogue.Application/Igorogue.Application.csproj"),
            new[] { "src/Igorogue.Domain/Igorogue.Domain.csproj" });
        AssertProjectReferences(
            Path.Combine(root.FullName, "src/Igorogue.Content/Igorogue.Content.csproj"),
            new[] { "src/Igorogue.Domain/Igorogue.Domain.csproj" });
        AssertProjectReferences(
            Path.Combine(root.FullName, "tools/Igorogue.Sim.Cli/Igorogue.Sim.Cli.csproj"),
            new[]
            {
                "src/Igorogue.Application/Igorogue.Application.csproj",
                "src/Igorogue.Content/Igorogue.Content.csproj",
            });
    }

    private static IEnumerable<Type> PublicSignatureTypes(Type type)
    {
        yield return type;

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
        {
            yield return property.PropertyType;
        }

        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
        {
            yield return method.ReturnType;
            foreach (var parameter in method.GetParameters())
            {
                yield return parameter.ParameterType;
            }
        }
    }

    private static IEnumerable<Type> PublicSurfaceTypes(Type type)
    {
        foreach (var signatureType in PublicSignatureTypes(type))
        {
            yield return signatureType;
        }

        foreach (var constructor in type.GetConstructors(
                     BindingFlags.Public | BindingFlags.Instance))
        {
            foreach (var parameter in constructor.GetParameters())
            {
                yield return parameter.ParameterType;
            }
        }
    }

    private static IEnumerable<Type> ExpandSignatureType(Type type)
    {
        yield return type;

        if (type.HasElementType && type.GetElementType() is { } elementType)
        {
            foreach (var nestedType in ExpandSignatureType(elementType))
            {
                yield return nestedType;
            }
        }

        foreach (var genericArgument in type.GetGenericArguments())
        {
            foreach (var nestedType in ExpandSignatureType(genericArgument))
            {
                yield return nestedType;
            }
        }
    }

    private static bool IsHostOrAmbientRuntimeType(Type type)
    {
        var typeNamespace = type.Namespace ?? string.Empty;
        var fullName = type.FullName ?? string.Empty;
        return typeNamespace.StartsWith("Godot", StringComparison.Ordinal) ||
            typeNamespace.StartsWith("System.IO", StringComparison.Ordinal) ||
            typeNamespace.StartsWith("System.Diagnostics", StringComparison.Ordinal) ||
            type == typeof(DateTime) ||
            type == typeof(DateTimeOffset) ||
            fullName == "System.TimeProvider" ||
            type == typeof(Random) ||
            fullName == "System.Security.Cryptography.RandomNumberGenerator";
    }

    private static MethodInfo RequirePublicStaticMethod(
        Type declaringType,
        string methodName,
        params Type[] parameterTypes)
    {
        var method = declaringType.GetMethod(
            methodName,
            BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly,
            binder: null,
            types: parameterTypes,
            modifiers: null);

        Assert.NotNull(method);
        return method;
    }

    private static MethodInfo RequirePublicInstanceMethod(
        Type declaringType,
        string methodName,
        params Type[] parameterTypes)
    {
        var method = declaringType.GetMethod(
            methodName,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly,
            binder: null,
            types: parameterTypes,
            modifiers: null);

        Assert.NotNull(method);
        return method;
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

        throw new DirectoryNotFoundException("Could not find Igorogue.sln from test output path.");
    }

    private static void AssertProjectReferences(
        string projectPath,
        IReadOnlyCollection<string> expectedRelativePaths)
    {
        var root = FindRepositoryRoot();
        var document = XDocument.Load(projectPath);
        var projectDirectory = Path.GetDirectoryName(projectPath)
            ?? throw new InvalidOperationException("Project path has no directory.");

        var actual = document
            .Descendants("ProjectReference")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => Path.GetFullPath(Path.Combine(projectDirectory, value!)))
            .Select(fullPath => Path.GetRelativePath(root.FullName, fullPath).Replace('\\', '/'))
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            expectedRelativePaths.OrderBy(value => value, StringComparer.Ordinal).ToArray(),
            actual);
    }
}
