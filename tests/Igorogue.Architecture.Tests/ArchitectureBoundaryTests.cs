using System.Reflection;
using System.Xml.Linq;
using Igorogue.Application.Bootstrap;
using Igorogue.Content;
using Igorogue.Domain.Bootstrap;

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
