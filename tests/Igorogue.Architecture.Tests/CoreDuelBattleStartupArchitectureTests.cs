using System.Reflection;

using Igorogue.Application.Battle;
using Igorogue.Domain.Content;

namespace Igorogue.Architecture.Tests;

public sealed class CoreDuelBattleStartupArchitectureTests
{
    [Fact]
    public void GodotFacingStartupHasOneExactTypedPublicEntryPoint()
    {
        var method = Assert.Single(typeof(CoreDuelBattleStartup).GetMethods(
            BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly));

        Assert.Equal(nameof(CoreDuelBattleStartup.Start), method.Name);
        Assert.Equal(typeof(CoreDuelBattleStartResult), method.ReturnType);
        Assert.Equal(
            [typeof(CoreDuelContentCatalog), typeof(string), typeof(long)],
            method.GetParameters().Select(parameter => parameter.ParameterType));
    }

    [Fact]
    public void TypedBattleSetupRemainsOwnedByDomainContent()
    {
        Assert.Equal(
            "Igorogue.Domain",
            typeof(CoreDuelBattleSetupDefinition).Assembly.GetName().Name);
        Assert.Equal(
            typeof(CoreDuelContentCatalog).Namespace,
            typeof(CoreDuelBattleSetupDefinition).Namespace);
        Assert.DoesNotContain(
            typeof(CoreDuelBattleSetupDefinition).GetProperties(
                BindingFlags.Public | BindingFlags.Instance),
            property => property.PropertyType.Assembly.GetName().Name is
                "Igorogue.Application" or "Igorogue.Content");
    }
}
