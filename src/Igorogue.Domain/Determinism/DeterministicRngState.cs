using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Igorogue.Domain.Determinism;

public sealed record DeterministicRngState
{
    // This version covers both the SplitMix64 step and the SHA-256 stream-derivation contract.
    // Changing either requires a new version and new compatibility vectors.
    public const string AlgorithmVersion = "splitmix64-v1";

    private const ulong Increment = 0x9E3779B97F4A7C15UL;
    private static readonly byte[] StreamDomain = Encoding.ASCII.GetBytes("igorogue:rng-stream:v1\0");

    private DeterministicRngState(
        long initialSeed,
        RngStream stream,
        ulong internalState,
        ulong drawCount)
    {
        InitialSeed = initialSeed;
        Stream = stream;
        InternalState = internalState;
        DrawCount = drawCount;
    }

    public long InitialSeed { get; }

    public RngStream Stream { get; }

    public ulong InternalState { get; }

    public ulong DrawCount { get; }

    public static DeterministicRngState Create(long initialSeed, RngStream stream)
    {
        var internalState = DeriveStreamState(initialSeed, stream);
        return new DeterministicRngState(
            initialSeed,
            stream,
            internalState,
            0);
    }

    public RngDraw Next()
    {
        var nextInternalState = unchecked(InternalState + Increment);
        var value = nextInternalState;
        value = unchecked((value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL);
        value = unchecked((value ^ (value >> 27)) * 0x94D049BB133111EBUL);
        value ^= value >> 31;

        var nextState = new DeterministicRngState(
            InitialSeed,
            Stream,
            nextInternalState,
            checked(DrawCount + 1));

        return new RngDraw(Stream, DrawCount, value, nextState);
    }

    public RngIndexDraw NextIndex(int exclusiveUpperBound)
    {
        if (exclusiveUpperBound <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(exclusiveUpperBound),
                exclusiveUpperBound,
                "Upper bound must be positive.");
        }

        var bound = (ulong)exclusiveUpperBound;
        var rejectionThreshold = unchecked(0UL - bound) % bound;
        var state = this;
        RngDraw draw;
        do
        {
            draw = state.Next();
            state = draw.NextState;
        }
        while (draw.Value < rejectionThreshold);

        return new RngIndexDraw(
            Stream,
            DrawCount,
            state.DrawCount - DrawCount,
            (int)(draw.Value % bound),
            state);
    }

    public string ToCanonicalText() =>
        $"algorithm={AlgorithmVersion}\n" +
        $"seed={InitialSeed.ToString(CultureInfo.InvariantCulture)}\n" +
        $"stream={StreamId(Stream)}\n" +
        $"state={InternalState:x16}\n" +
        $"draws={DrawCount.ToString(CultureInfo.InvariantCulture)}\n";

    private static ulong DeriveStreamState(long initialSeed, RngStream stream)
    {
        var streamId = StreamByte(stream);
        Span<byte> input = stackalloc byte[StreamDomain.Length + sizeof(ulong) + sizeof(byte)];
        StreamDomain.CopyTo(input);
        BinaryPrimitives.WriteUInt64BigEndian(
            input.Slice(StreamDomain.Length, sizeof(ulong)),
            unchecked((ulong)initialSeed));
        input[^1] = streamId;

        Span<byte> hash = stackalloc byte[SHA256.HashSizeInBytes];
        SHA256.HashData(input, hash);
        return BinaryPrimitives.ReadUInt64BigEndian(hash);
    }

    private static byte StreamByte(RngStream stream) => stream switch
    {
        RngStream.Gameplay => 1,
        RngStream.Reward => 2,
        RngStream.Cosmetic => 3,
        _ => throw new ArgumentOutOfRangeException(nameof(stream), stream, "Unknown RNG stream."),
    };

    private static string StreamId(RngStream stream) => stream switch
    {
        RngStream.Gameplay => "gameplay",
        RngStream.Reward => "reward",
        RngStream.Cosmetic => "cosmetic",
        _ => throw new ArgumentOutOfRangeException(nameof(stream), stream, "Unknown RNG stream."),
    };
}

public sealed record RngDraw(
    RngStream Stream,
    ulong DrawIndex,
    ulong Value,
    DeterministicRngState NextState);

public sealed record RngIndexDraw(
    RngStream Stream,
    ulong FirstDrawIndex,
    ulong DrawsConsumed,
    int Value,
    DeterministicRngState NextState);
