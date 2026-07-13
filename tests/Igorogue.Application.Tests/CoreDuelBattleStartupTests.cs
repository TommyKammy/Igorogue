using Igorogue.Application.Battle;
using Igorogue.Application.Replay;

namespace Igorogue.Application.Tests;

public sealed class CoreDuelBattleStartupTests
{
    [Fact]
    public void TypedCatalogStartupMatchesTheExistingStandardFixtureExactly()
    {
        var catalog = CoreDuelBattleTestFixture.LoadCatalog();
        var expected = CoreDuelBattleStateMachine.Start(
            CoreDuelBattleTestFixture.InitialSnapshot(),
            catalog,
            ReplayMetadata.Create(
                CoreDuelBattleTestFixture.GameVersion,
                catalog.ContentHash,
                CoreDuelBattleTestFixture.Seed));

        var actual = CoreDuelBattleStartup.Start(
            catalog,
            CoreDuelBattleTestFixture.GameVersion,
            CoreDuelBattleTestFixture.Seed);

        Assert.Equal(
            expected.Session.State.Bootstrap.InitialSnapshot.ToCanonicalText(),
            actual.Session.State.Bootstrap.InitialSnapshot.ToCanonicalText());
        Assert.Equal(expected.Session.State.CanonicalText, actual.Session.State.CanonicalText);
        Assert.Equal(expected.Session.State.Checksum, actual.Session.State.Checksum);
        Assert.Equal(
            expected.Session.CommandLog.CurrentChecksum,
            actual.Session.CommandLog.CurrentChecksum);
        Assert.Equal(
            expected.OrderedFacts.Select(fact => fact.GetType()),
            actual.OrderedFacts.Select(fact => fact.GetType()));
    }

    [Fact]
    public void SameCatalogVersionAndSeedProduceTheSameFreshSession()
    {
        var catalog = CoreDuelBattleTestFixture.LoadCatalog();

        var first = CoreDuelBattleStartup.Start(
            catalog,
            CoreDuelBattleTestFixture.GameVersion,
            CoreDuelBattleTestFixture.Seed);
        var second = CoreDuelBattleStartup.Start(
            catalog,
            CoreDuelBattleTestFixture.GameVersion,
            CoreDuelBattleTestFixture.Seed);

        Assert.Equal(first.Session.State.CanonicalText, second.Session.State.CanonicalText);
        Assert.Equal(first.Session.State.Checksum, second.Session.State.Checksum);
        Assert.Equal(
            first.Session.CommandLog.CurrentChecksum,
            second.Session.CommandLog.CurrentChecksum);
    }

    [Fact]
    public void StartupRejectsMissingCatalogAndInvalidReplayVersion()
    {
        Assert.Throws<ArgumentNullException>(() =>
            CoreDuelBattleStartup.Start(
                null!,
                CoreDuelBattleTestFixture.GameVersion,
                CoreDuelBattleTestFixture.Seed));

        var catalog = CoreDuelBattleTestFixture.LoadCatalog();
        Assert.Throws<ArgumentException>(() =>
            CoreDuelBattleStartup.Start(catalog, "invalid version", CoreDuelBattleTestFixture.Seed));
    }
}
