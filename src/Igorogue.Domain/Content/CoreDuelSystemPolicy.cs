namespace Igorogue.Domain.Content;

public sealed class CoreDuelSystemPolicy
{
    private CoreDuelSystemPolicy(int baseQi, int baseDraw)
    {
        BaseQi = baseQi;
        BaseDraw = baseDraw;
    }

    public int BaseQi { get; }

    public int BaseDraw { get; }

    public static CoreDuelSystemPolicy Create(int baseQi, int baseDraw)
    {
        if (baseQi <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(baseQi),
                baseQi,
                "Core Duel base qi must be positive.");
        }

        if (baseDraw <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(baseDraw),
                baseDraw,
                "Core Duel base draw must be positive.");
        }

        return new CoreDuelSystemPolicy(baseQi, baseDraw);
    }
}
