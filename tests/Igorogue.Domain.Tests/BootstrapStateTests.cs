using Igorogue.Domain.Bootstrap;
using Igorogue.Domain.Determinism;

namespace Igorogue.Domain.Tests;

public sealed class BootstrapStateTests
{
    [Fact]
    public void DefaultStateProducesStableChecksum()
    {
        var state = BootstrapState.CreateDefault();
        var first = DeterministicChecksum.Combine("bootstrap", state.ToCanonicalText());
        var second = DeterministicChecksum.Combine("bootstrap", state.ToCanonicalText());

        Assert.Equal(first, second);
        Assert.Equal(64, first.Length);
    }

    [Fact]
    public void ComponentBoundariesAffectChecksum()
    {
        var first = DeterministicChecksum.Combine("ab", "c");
        var second = DeterministicChecksum.Combine("a", "bc");

        Assert.NotEqual(first, second);
    }
}
