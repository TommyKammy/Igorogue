using System.Globalization;

namespace Igorogue.Domain.Determinism;

public sealed record AuthoritativeRngState
{
    // Cosmetic randomness is intentionally absent: it must never affect authoritative state.
    private AuthoritativeRngState(
        long initialSeed,
        DeterministicRngState gameplay,
        DeterministicRngState reward)
    {
        InitialSeed = initialSeed;
        Gameplay = gameplay;
        Reward = reward;
    }

    public long InitialSeed { get; }

    public DeterministicRngState Gameplay { get; }

    public DeterministicRngState Reward { get; }

    public static AuthoritativeRngState Create(long initialSeed) =>
        new(
            initialSeed,
            DeterministicRngState.Create(initialSeed, RngStream.Gameplay),
            DeterministicRngState.Create(initialSeed, RngStream.Reward));

    public AuthoritativeRngDraw NextGameplay()
    {
        var draw = Gameplay.Next();
        return new AuthoritativeRngDraw(
            draw.Value,
            draw.DrawIndex,
            new AuthoritativeRngState(InitialSeed, draw.NextState, Reward));
    }

    public AuthoritativeRngDraw NextReward()
    {
        var draw = Reward.Next();
        return new AuthoritativeRngDraw(
            draw.Value,
            draw.DrawIndex,
            new AuthoritativeRngState(InitialSeed, Gameplay, draw.NextState));
    }

    public AuthoritativeRngIndexDraw NextGameplayIndex(int exclusiveUpperBound)
    {
        var draw = Gameplay.NextIndex(exclusiveUpperBound);
        return new AuthoritativeRngIndexDraw(
            draw.Value,
            draw.FirstDrawIndex,
            draw.DrawsConsumed,
            new AuthoritativeRngState(InitialSeed, draw.NextState, Reward));
    }

    public AuthoritativeRngIndexDraw NextRewardIndex(int exclusiveUpperBound)
    {
        var draw = Reward.NextIndex(exclusiveUpperBound);
        return new AuthoritativeRngIndexDraw(
            draw.Value,
            draw.FirstDrawIndex,
            draw.DrawsConsumed,
            new AuthoritativeRngState(InitialSeed, Gameplay, draw.NextState));
    }

    public string ToCanonicalText() =>
        $"rng_algorithm={DeterministicRngState.AlgorithmVersion}\n" +
        $"initial_seed={InitialSeed.ToString(CultureInfo.InvariantCulture)}\n" +
        $"gameplay_state={Gameplay.InternalState:x16}\n" +
        $"gameplay_draws={Gameplay.DrawCount.ToString(CultureInfo.InvariantCulture)}\n" +
        $"reward_state={Reward.InternalState:x16}\n" +
        $"reward_draws={Reward.DrawCount.ToString(CultureInfo.InvariantCulture)}\n";
}

public sealed record AuthoritativeRngDraw(
    ulong Value,
    ulong DrawIndex,
    AuthoritativeRngState NextState);

public sealed record AuthoritativeRngIndexDraw(
    int Value,
    ulong FirstDrawIndex,
    ulong DrawsConsumed,
    AuthoritativeRngState NextState);
