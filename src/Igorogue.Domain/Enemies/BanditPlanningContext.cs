using Igorogue.Domain.Board;
using Igorogue.Domain.Combat;
using Igorogue.Domain.Facilities;

namespace Igorogue.Domain.Enemies;

public sealed class BanditPlanningContext
{
    public BanditPlanningContext(
        StoneRuntimeState stoneRuntimeState,
        TemporaryLibertyState temporaryLibertyState,
        ContinuousLibertySnapshot continuousLibertySnapshot,
        BattleRepetitionHistory repetitionHistory,
        FacilityRuntimeAnalysis facilityRuntimeAnalysis)
    {
        ArgumentNullException.ThrowIfNull(stoneRuntimeState);
        ArgumentNullException.ThrowIfNull(temporaryLibertyState);
        ArgumentNullException.ThrowIfNull(continuousLibertySnapshot);
        ArgumentNullException.ThrowIfNull(repetitionHistory);
        ArgumentNullException.ThrowIfNull(facilityRuntimeAnalysis);
        if (!ReferenceEquals(temporaryLibertyState.SourceStones, stoneRuntimeState) ||
            !ReferenceEquals(continuousLibertySnapshot.SourceStones, stoneRuntimeState))
        {
            throw new ArgumentException(
                "Bandit liberty state must belong to the exact stone runtime.",
                nameof(stoneRuntimeState));
        }

        if (!ReferenceEquals(facilityRuntimeAnalysis.SourceBoard, stoneRuntimeState.SourceBoard))
        {
            throw new ArgumentException(
                "Bandit facility state must belong to the exact board snapshot.",
                nameof(facilityRuntimeAnalysis));
        }

        if (!repetitionHistory.Current.Equals(
                StoneTopologyKey.FromBoard(stoneRuntimeState.SourceBoard)))
        {
            throw new ArgumentException(
                "Bandit repetition history must end at the source board topology.",
                nameof(repetitionHistory));
        }

        StoneRuntimeState = stoneRuntimeState;
        TemporaryLibertyState = temporaryLibertyState;
        ContinuousLibertySnapshot = continuousLibertySnapshot;
        RepetitionHistory = repetitionHistory;
        FacilityRuntimeAnalysis = facilityRuntimeAnalysis;
        GroupAnalysis = StoneGroupAnalyzer.Analyze(stoneRuntimeState.SourceBoard);
        EffectiveLibertyAnalysis = TemporaryLibertyEffectiveLibertyAnalyzer.Analyze(
            stoneRuntimeState,
            temporaryLibertyState,
            continuousLibertySnapshot,
            GroupAnalysis);
        BlackKingGroup = FindKingGroup(StoneColor.Black);
        WhiteKingGroup = FindKingGroup(StoneColor.White);
    }

    public StoneRuntimeState StoneRuntimeState { get; }

    public TemporaryLibertyState TemporaryLibertyState { get; }

    public ContinuousLibertySnapshot ContinuousLibertySnapshot { get; }

    public BattleRepetitionHistory RepetitionHistory { get; }

    public FacilityRuntimeAnalysis FacilityRuntimeAnalysis { get; }

    public FacilityState FacilityState => FacilityRuntimeAnalysis.FacilityState;

    public TerritoryAnalysis TerritoryAnalysis => FacilityRuntimeAnalysis.TerritoryAnalysis;

    public BoardState Board => StoneRuntimeState.SourceBoard;

    public StoneGroupAnalysis GroupAnalysis { get; }

    public TemporaryLibertyEffectiveLibertyAnalysis EffectiveLibertyAnalysis { get; }

    public StoneGroup BlackKingGroup { get; }

    public StoneGroup WhiteKingGroup { get; }

    public int EffectiveLibertiesFor(StoneGroup group) =>
        EffectiveLibertyAnalysis.BreakdownFor(group).EffectiveLibertyCount;

    private StoneGroup FindKingGroup(StoneColor color)
    {
        var kings = Board.OccupiedStones
            .Where(stone => stone.Color == color && stone.IsKing)
            .ToArray();
        if (kings.Length != 1)
        {
            throw new ArgumentException(
                $"Bandit planning requires exactly one {color} king stone.",
                nameof(StoneRuntimeState));
        }

        return GroupAnalysis.GroupAt(kings[0].Point)
            ?? throw new InvalidOperationException("A king stone must belong to a current group.");
    }
}
