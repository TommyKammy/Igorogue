using Igorogue.Domain.Determinism;

namespace Igorogue.Domain.Tests;

public sealed class DeterministicRngTests
{
    [Fact]
    public void GameplayStreamMatchesVersionedGoldenSequence()
    {
        var state = DeterministicRngState.Create(42, RngStream.Gameplay);
        var expected = new[]
        {
            0x0D63DF1A615C7127UL,
            0xB3D67C1DD9431D2FUL,
            0x67254F1D2AB2345EUL,
        };

        Assert.Equal(0x2C8FE816DABE845DUL, state.InternalState);

        for (var index = 0; index < expected.Length; index++)
        {
            var draw = state.Next();
            Assert.Equal(RngStream.Gameplay, draw.Stream);
            Assert.Equal((ulong)index, draw.DrawIndex);
            Assert.Equal(expected[index], draw.Value);
            state = draw.NextState;
        }

        Assert.Equal(3UL, state.DrawCount);
        Assert.Equal(0x07365543589DF89CUL, state.InternalState);
    }

    [Fact]
    public void NamedStreamsAreIndependentForTheSameSeed()
    {
        var gameplay = DeterministicRngState.Create(42, RngStream.Gameplay);
        var reward = DeterministicRngState.Create(42, RngStream.Reward);
        var cosmetic = DeterministicRngState.Create(42, RngStream.Cosmetic);

        Assert.Equal(0x45700FA212BB5513UL, reward.InternalState);
        Assert.Equal(0x1880477793BC3EAAUL, cosmetic.InternalState);
        Assert.Equal(0x0D63DF1A615C7127UL, gameplay.Next().Value);
        Assert.Equal(0x3F04E68406F4A9F6UL, reward.Next().Value);
        Assert.Equal(0x16F8AB1934FA0B8EUL, cosmetic.Next().Value);

        var cosmeticAfterTwoDraws = cosmetic.Next().NextState.Next().NextState;
        Assert.Equal(gameplay.Next(), DeterministicRngState.Create(42, RngStream.Gameplay).Next());
        Assert.Equal(2UL, cosmeticAfterTwoDraws.DrawCount);
        Assert.Equal(0UL, gameplay.DrawCount);
    }

    [Fact]
    public void NextReturnsNewStateWithoutMutatingPreviousState()
    {
        var original = DeterministicRngState.Create(-17, RngStream.Reward);
        var canonicalBefore = original.ToCanonicalText();

        var draw = original.Next();

        Assert.Equal(canonicalBefore, original.ToCanonicalText());
        Assert.Equal(0UL, original.DrawCount);
        Assert.Equal(1UL, draw.NextState.DrawCount);
        Assert.NotEqual(original.InternalState, draw.NextState.InternalState);
    }

    [Fact]
    public void CanonicalTextIncludesAlgorithmSeedStreamAndPosition()
    {
        var state = DeterministicRngState.Create(42, RngStream.Gameplay).Next().NextState;

        Assert.Equal(
            "algorithm=splitmix64-v1\n" +
            "seed=42\n" +
            "stream=gameplay\n" +
            "state=cac761d05a090072\n" +
            "draws=1\n",
            state.ToCanonicalText());
    }

    [Fact]
    public void NegativeSeedUsesSignedTwosComplementBigEndianEncoding()
    {
        var state = DeterministicRngState.Create(-1, RngStream.Gameplay);

        Assert.Equal(0xB67360AB35983C0DUL, state.InternalState);
        Assert.Equal(0x39E4C1F2CFE44A6FUL, state.Next().Value);
    }

    [Fact]
    public void BoundedDrawIsRepeatableAndWithinRange()
    {
        var first = DeterministicRngState.Create(42, RngStream.Gameplay);
        var second = DeterministicRngState.Create(42, RngStream.Gameplay);

        var boundOne = first.NextIndex(1);
        Assert.Equal(0, boundOne.Value);
        Assert.Equal(1UL, boundOne.DrawsConsumed);
        Assert.Equal(3, first.NextIndex(7).Value);

        for (var index = 0; index < 128; index++)
        {
            var firstDraw = first.NextIndex(7);
            var secondDraw = second.NextIndex(7);
            Assert.InRange(firstDraw.Value, 0, 6);
            Assert.Equal(firstDraw, secondDraw);
            first = firstDraw.NextState;
            second = secondDraw.NextState;
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void InvalidBoundIsRejectedWithoutConsumingState(int invalidBound)
    {
        var state = DeterministicRngState.Create(42, RngStream.Gameplay);
        var canonicalBefore = state.ToCanonicalText();

        Assert.Throws<ArgumentOutOfRangeException>(() => state.NextIndex(invalidBound));

        Assert.Equal(canonicalBefore, state.ToCanonicalText());
        Assert.Equal(0UL, state.DrawCount);
    }

    [Fact]
    public void AuthoritativeStreamsAdvanceIndependently()
    {
        var original = AuthoritativeRngState.Create(42);

        var gameplayDraw = original.NextGameplay();
        var rewardDraw = original.NextReward();

        Assert.Equal(original.Reward, gameplayDraw.NextState.Reward);
        Assert.Equal(original.Gameplay, rewardDraw.NextState.Gameplay);
        Assert.Equal(1UL, gameplayDraw.NextState.Gameplay.DrawCount);
        Assert.Equal(1UL, rewardDraw.NextState.Reward.DrawCount);
    }

    [Fact]
    public void CosmeticDrawsCannotChangeAuthoritativeStateOrChecksum()
    {
        var authoritative = AuthoritativeRngState.Create(42);
        var cosmetic = DeterministicRngState.Create(42, RngStream.Cosmetic);
        var canonicalBefore = authoritative.ToCanonicalText();
        var checksumBefore = DeterministicChecksum.Sha256Hex(canonicalBefore);

        for (var index = 0; index < 100; index++)
        {
            cosmetic = cosmetic.Next().NextState;
        }

        Assert.Equal(100UL, cosmetic.DrawCount);
        Assert.Equal(canonicalBefore, authoritative.ToCanonicalText());
        Assert.Equal(checksumBefore, DeterministicChecksum.Sha256Hex(authoritative.ToCanonicalText()));
    }

    [Fact]
    public void LengthPrefixedChecksumKeepsComponentBoundariesDistinct()
    {
        Assert.NotEqual(
            DeterministicChecksum.Combine("ab", "c"),
            DeterministicChecksum.Combine("a", "bc"));
    }

    [Fact]
    public void UnknownStreamIsRejectedBeforeStateCreation()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => DeterministicRngState.Create(42, (RngStream)99));
    }
}
