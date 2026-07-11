using Igorogue.Domain.Facilities;

namespace Igorogue.Domain.Tests;

public sealed class FacilityRuntimePolicyTests
{
    [Fact]
    public void PolicyCanonicalizesBandsAndUsesInjectedIncomeCapacityAndTypeLimits()
    {
        var policy = Policy();

        Assert.Equal(3, policy.TerritoryIncomeDivisor);
        Assert.Equal(new[] { 1, 4, 8, 13 }, policy.CapacityBands.Select(band => band.MinSize));
        Assert.Equal(1, policy.TerritoryIncomeForSize(1));
        Assert.Equal(1, policy.TerritoryIncomeForSize(3));
        Assert.Equal(2, policy.TerritoryIncomeForSize(4));
        Assert.Equal(1, policy.BaseCapacityForSize(3));
        Assert.Equal(2, policy.BaseCapacityForSize(4));
        Assert.Equal(4, policy.BaseCapacityForSize(49));
        Assert.Equal(5, policy.EffectiveCapacityForSize(49, 10));
        Assert.Equal(2, policy.TypeLimitFor("development"));
        Assert.Equal(2, policy.TypeLimitFor("furnace"));
        Assert.Equal(1, policy.TypeLimitFor("market"));

        var bands = Assert.IsAssignableFrom<ICollection<FacilityCapacityBand>>(
            policy.CapacityBands);
        Assert.True(bands.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => bands.Add(new(1, 49, 1)));
        var limits = Assert.IsAssignableFrom<IDictionary<string, int>>(policy.TypeLimits);
        Assert.True(limits.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => limits.Add("other", 2));
    }

    [Fact]
    public void PolicyRejectsInvalidBandsLimitsAndModifiers()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FacilityCapacityBand(1, 3, 0));
        Assert.Throws<ArgumentException>(() => FacilityRuntimePolicy.Create(
            3,
            [new(1, 3, 1), new(5, 49, 2)],
            5,
            Limits()));
        Assert.Throws<ArgumentException>(() => FacilityRuntimePolicy.Create(
            3,
            [new(1, 49, 6)],
            5,
            Limits()));
        Assert.Throws<ArgumentException>(() => FacilityRuntimePolicy.Create(
            3,
            [new(1, 49, 1)],
            5,
            [new KeyValuePair<string, int>("development", 2)]));
        Assert.Throws<ArgumentException>(() => FacilityRuntimePolicy.Create(
            3,
            [new(1, 49, 1)],
            5,
            [
                new KeyValuePair<string, int>("default", 1),
                new KeyValuePair<string, int>("default", 2),
            ]));
        Assert.Throws<ArgumentOutOfRangeException>(() => Policy().EffectiveCapacityForSize(3, -1));
    }

    private static FacilityRuntimePolicy Policy() => FacilityRuntimePolicy.Create(
        3,
        [
            new FacilityCapacityBand(13, 49, 4),
            new FacilityCapacityBand(4, 7, 2),
            new FacilityCapacityBand(1, 3, 1),
            new FacilityCapacityBand(8, 12, 3),
        ],
        5,
        Limits());

    private static KeyValuePair<string, int>[] Limits() =>
    [
        new("furnace", 2),
        new("default", 1),
        new("development", 2),
    ];
}
