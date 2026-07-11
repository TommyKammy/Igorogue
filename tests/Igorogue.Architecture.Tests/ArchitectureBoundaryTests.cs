using System.Reflection;
using System.Xml.Linq;
using Igorogue.Application.Battle;
using Igorogue.Application.Bootstrap;
using Igorogue.Application.Replay;
using Igorogue.Content;
using Igorogue.Domain.Board;
using Igorogue.Domain.Bootstrap;
using Igorogue.Domain.Combat;
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
        Assert.Empty(typeof(TerritoryEstablishedFact).GetConstructors(
            BindingFlags.Public | BindingFlags.Instance));
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
            Array.Empty<string>());
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
