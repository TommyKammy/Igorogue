using System.Collections.ObjectModel;
using System.Globalization;

using Igorogue.Domain.Board;
using Igorogue.Domain.Determinism;

namespace Igorogue.Domain.Combat;

public sealed class TemporaryLibertyExpirySweepWindow
{
    internal TemporaryLibertyExpirySweepWindow(
        TemporaryLibertyState temporaryLibertiesAfterSweep,
        int enemyTurnIndex,
        bool terminal)
    {
        TemporaryLibertiesAfterSweep = temporaryLibertiesAfterSweep;
        EnemyTurnIndex = enemyTurnIndex;
        Terminal = terminal;
    }

    public int EnemyTurnIndex { get; }

    public bool Terminal { get; }

    internal TemporaryLibertyState TemporaryLibertiesAfterSweep { get; }

    internal TemporaryLibertyExpirySweepWindow ContinueWith(
        TemporaryLibertyState nextState) =>
        new(nextState, EnemyTurnIndex, Terminal);
}

public sealed class TemporaryLibertyExpiryResolution
{
    public const string EncodingVersion = "temporary-liberty-expiry-resolution-v1";

    private readonly ReadOnlyCollection<TemporaryLibertyEffect> expiredEffectView;
    private readonly ReadOnlyCollection<TemporaryLibertyEffect> carrierRemovedEffectView;
    private readonly ReadOnlyCollection<StoneGroup> capturedGroupView;
    private readonly ReadOnlyCollection<StoneRuntimeInstance> capturedStoneInstanceView;
    private readonly ReadOnlyCollection<IBattleFact> factView;

    internal TemporaryLibertyExpiryResolution(
        StoneRuntimeState sourceStones,
        TemporaryLibertyState sourceTemporaryLiberties,
        ContinuousLibertySnapshot continuousLiberties,
        BattleRepetitionHistory sourceHistory,
        int enemyTurnIndex,
        bool isExactNoOp,
        StoneRuntimeState stonesAfterResolution,
        TemporaryLibertyState temporaryLibertiesAfterResolution,
        BattleRepetitionHistory historyAfterResolution,
        TemporaryLibertyEffect[] expiredEffects,
        TemporaryLibertyEffect[] carrierRemovedEffects,
        StoneGroup[] capturedGroups,
        StoneRuntimeInstance[] capturedStoneInstances,
        KingCaptureResult kingCaptureResult,
        TemporaryLibertyCaptureBenefitDisposition benefitDisposition,
        TerritoryAnalysis territoryAfterResolution,
        TemporaryLibertyEffectiveLibertyAnalysis? effectiveLibertiesAfterExpiry,
        bool? topologyFirstSeen,
        StoneTopologyKey? registeredTopologyKey,
        IBattleFact[] orderedFacts)
    {
        SourceStones = sourceStones;
        SourceTemporaryLiberties = sourceTemporaryLiberties;
        ContinuousLiberties = continuousLiberties;
        SourceHistory = sourceHistory;
        EnemyTurnIndex = enemyTurnIndex;
        IsExactNoOp = isExactNoOp;
        StonesAfterResolution = stonesAfterResolution;
        TemporaryLibertiesAfterResolution = temporaryLibertiesAfterResolution;
        HistoryAfterResolution = historyAfterResolution;
        expiredEffectView = Array.AsReadOnly(
            (TemporaryLibertyEffect[])expiredEffects.Clone());
        carrierRemovedEffectView = Array.AsReadOnly(
            (TemporaryLibertyEffect[])carrierRemovedEffects.Clone());
        capturedGroupView = Array.AsReadOnly((StoneGroup[])capturedGroups.Clone());
        capturedStoneInstanceView = Array.AsReadOnly(
            (StoneRuntimeInstance[])capturedStoneInstances.Clone());
        KingCaptureResult = kingCaptureResult;
        BenefitDisposition = benefitDisposition;
        TerritoryAfterResolution = territoryAfterResolution;
        EffectiveLibertiesAfterExpiry = effectiveLibertiesAfterExpiry;
        TopologyFirstSeen = topologyFirstSeen;
        RegisteredTopologyKey = registeredTopologyKey;
        factView = Array.AsReadOnly((IBattleFact[])orderedFacts.Clone());
        SweepWindow = new TemporaryLibertyExpirySweepWindow(
            temporaryLibertiesAfterResolution,
            enemyTurnIndex,
            kingCaptureResult.IsTerminal);
        CanonicalText = CreateCanonicalText();
        Checksum = DeterministicChecksum.Sha256Hex(CanonicalText);
    }

    public StoneRuntimeState SourceStones { get; }

    public TemporaryLibertyState SourceTemporaryLiberties { get; }

    public ContinuousLibertySnapshot ContinuousLiberties { get; }

    public BattleRepetitionHistory SourceHistory { get; }

    public int EnemyTurnIndex { get; }

    public bool IsExactNoOp { get; }

    public StoneRuntimeState StonesAfterResolution { get; }

    public BoardState BoardAfterResolution => StonesAfterResolution.SourceBoard;

    public TemporaryLibertyState TemporaryLibertiesAfterResolution { get; }

    public BattleRepetitionHistory HistoryAfterResolution { get; }

    public IReadOnlyList<TemporaryLibertyEffect> ExpiredEffects => expiredEffectView;

    public IReadOnlyList<TemporaryLibertyEffect> CarrierRemovedEffects =>
        carrierRemovedEffectView;

    public IReadOnlyList<StoneGroup> CapturedGroups => capturedGroupView;

    public IReadOnlyList<StoneRuntimeInstance> CapturedStoneInstances =>
        capturedStoneInstanceView;

    public KingCaptureResult KingCaptureResult { get; }

    public TemporaryLibertyCaptureBenefitDisposition BenefitDisposition { get; }

    public bool BenefitsSuppressed =>
        BenefitDisposition ==
        TemporaryLibertyCaptureBenefitDisposition.SuppressedByTerminalKingCapture;

    public bool CanProcessCaptureBenefits =>
        BenefitDisposition ==
        TemporaryLibertyCaptureBenefitDisposition.PendingNonTerminalPipeline;

    public TerritoryAnalysis TerritoryAfterResolution { get; }

    public TemporaryLibertyEffectiveLibertyAnalysis? EffectiveLibertiesAfterExpiry { get; }

    public bool? TopologyFirstSeen { get; }

    public StoneTopologyKey? RegisteredTopologyKey { get; }

    public IReadOnlyList<IBattleFact> OrderedFacts => factView;

    public TemporaryLibertyExpirySweepWindow SweepWindow { get; }

    public string CanonicalText { get; }

    public string Checksum { get; }

    private string CreateCanonicalText()
    {
        var lines = new List<string>
        {
            EncodingVersion,
            $"enemy_turn_index={EnemyTurnIndex.ToString(CultureInfo.InvariantCulture)}",
            $"exact_no_op={(IsExactNoOp ? 1 : 0).ToString(CultureInfo.InvariantCulture)}",
            $"source_topology={StoneTopologyKey.FromBoard(SourceStones.SourceBoard).ToCanonicalText()}",
            $"result_topology={StoneTopologyKey.FromBoard(BoardAfterResolution).ToCanonicalText()}",
            $"benefit_disposition={BenefitDispositionId(BenefitDisposition)}",
            $"battle_result={KingCaptureResult.OutcomeId}",
            $"battle_reason={KingCaptureResult.EndReasonId}",
            $"topology_first_seen={TopologyFirstSeen switch { true => "true", false => "false", null => "none" }}",
            $"registered_topology={RegisteredTopologyKey?.ToCanonicalText() ?? "none"}",
            $"source_stones_checksum={DeterministicChecksum.Sha256Hex(SourceStones.ToCanonicalText())}",
            $"source_temporary_checksum={DeterministicChecksum.Sha256Hex(SourceTemporaryLiberties.ToCanonicalText())}",
            $"expired_count={expiredEffectView.Count.ToString(CultureInfo.InvariantCulture)}",
        };
        foreach (var effect in expiredEffectView)
        {
            lines.Add(
                $"expired_effect={TemporaryLibertyState.EncodeStableText(effect.EffectInstanceId)}");
        }

        lines.Add(
            $"carrier_removed_count={carrierRemovedEffectView.Count.ToString(CultureInfo.InvariantCulture)}");
        foreach (var effect in carrierRemovedEffectView)
        {
            lines.Add(
                $"carrier_removed_effect={TemporaryLibertyState.EncodeStableText(effect.EffectInstanceId)}");
        }

        lines.Add($"captured_group_count={capturedGroupView.Count.ToString(CultureInfo.InvariantCulture)}");
        foreach (var group in capturedGroupView)
        {
            lines.Add(
                $"captured_group={TemporaryLibertyState.ColorId(group.Color)}:{group.Anchor.X.ToString(CultureInfo.InvariantCulture)},{group.Anchor.Y.ToString(CultureInfo.InvariantCulture)}:{string.Join(';', group.StonePoints.Select(point => $"{point.X.ToString(CultureInfo.InvariantCulture)},{point.Y.ToString(CultureInfo.InvariantCulture)}"))}");
        }

        lines.Add(
            $"captured_stone_instance_count={capturedStoneInstanceView.Count.ToString(CultureInfo.InvariantCulture)}");
        foreach (var instance in capturedStoneInstanceView)
        {
            lines.Add(
                $"captured_stone_instance={TemporaryLibertyState.EncodeStableText(instance.InstanceId)}:" +
                $"{TemporaryLibertyState.EncodeStableText(instance.KindId)}:" +
                $"{instance.Point.X.ToString(CultureInfo.InvariantCulture)},{instance.Point.Y.ToString(CultureInfo.InvariantCulture)}:" +
                $"sequence={instance.CreatedSequence.ToString(CultureInfo.InvariantCulture)}");
        }

        lines.Add($"fact_count={factView.Count.ToString(CultureInfo.InvariantCulture)}");
        foreach (var fact in factView)
        {
            lines.Add($"fact={ProjectFact(fact)}");
        }

        lines.Add($"stones_checksum={DeterministicChecksum.Sha256Hex(StonesAfterResolution.ToCanonicalText())}");
        lines.Add(
            $"temporary_checksum={DeterministicChecksum.Sha256Hex(TemporaryLibertiesAfterResolution.ToCanonicalText())}");
        lines.Add(
            $"continuous_checksum={DeterministicChecksum.Sha256Hex(ContinuousLiberties.ToCanonicalText())}");
        lines.Add(
            $"history_checksum={DeterministicChecksum.Sha256Hex(HistoryAfterResolution.ToCanonicalText())}");
        if (EffectiveLibertiesAfterExpiry is not null)
        {
            lines.Add(
                $"effective_checksum={DeterministicChecksum.Sha256Hex(EffectiveLibertiesAfterExpiry.ToCanonicalText())}");
        }

        return string.Join('\n', lines);
    }

    private static string ProjectFact(IBattleFact fact) => fact switch
    {
        TemporaryLibertyExpirySweepStartedFact started =>
            $"sweep_started:{started.EnemyTurnIndex.ToString(CultureInfo.InvariantCulture)}",
        TemporaryLibertyExpiredFact expired =>
            $"expired:{TemporaryLibertyState.EncodeStableText(expired.Effect.EffectInstanceId)}",
        TemporaryLibertyGroupCapturedFact captured =>
            $"group_captured:{TemporaryLibertyState.ColorId(captured.CapturedGroup.Color)}:" +
            $"{captured.CapturedGroup.Anchor.X.ToString(CultureInfo.InvariantCulture)}," +
            $"{captured.CapturedGroup.Anchor.Y.ToString(CultureInfo.InvariantCulture)}:" +
            $"{captured.CapturedGroup.Stones.Count.ToString(CultureInfo.InvariantCulture)}",
        TemporaryLibertyRemovedFact removed =>
            $"removed:{TemporaryLibertyState.EncodeStableText(removed.Effect.EffectInstanceId)}:{removed.ReasonId}",
        StoneTopologyRegisteredFact topology =>
            $"topology:{topology.RegisteredTopologyKey.ToCanonicalText()}:" +
            $"first_seen={(topology.FirstSeen ? "true" : "false")}:" +
            $"reason={topology.SourceReasonId}",
        TemporaryLibertyKingGateFact king =>
            $"king_gate:{king.Result.ToCanonicalText()}",
        CaptureBenefitSuppressedFact suppressed =>
            $"benefit_suppressed:{suppressed.ReasonId}",
        TemporaryLibertyExpirySweepResolvedFact resolved =>
            $"sweep_resolved:{resolved.EnemyTurnIndex.ToString(CultureInfo.InvariantCulture)}:" +
            $"groups={resolved.CapturedGroupCount.ToString(CultureInfo.InvariantCulture)}:" +
            $"terminal={(resolved.Terminal ? 1 : 0).ToString(CultureInfo.InvariantCulture)}",
        _ => throw new InvalidOperationException(
            $"Unhandled temporary-liberty fact projection {fact.GetType().FullName}."),
    };

    private static string BenefitDispositionId(
        TemporaryLibertyCaptureBenefitDisposition disposition) => disposition switch
    {
        TemporaryLibertyCaptureBenefitDisposition.NotApplicable => "not_applicable",
        TemporaryLibertyCaptureBenefitDisposition.PendingNonTerminalPipeline =>
            "pending_nonterminal_pipeline",
        TemporaryLibertyCaptureBenefitDisposition.SuppressedByTerminalKingCapture =>
            "suppressed_terminal_king_capture",
        _ => throw new InvalidOperationException("Unknown capture benefit disposition."),
    };
}
