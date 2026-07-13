using System.Collections.ObjectModel;
using System.Globalization;

using Igorogue.Domain.Board;
using Igorogue.Domain.Content;
using Igorogue.Domain.Determinism;

namespace Igorogue.Domain.Enemies;

public static class EnemyIntentKindRules
{
    public static string ToIntentId(EnemyIntentKind intentKind) => intentKind switch
    {
        EnemyIntentKind.AdvanceTowardBlackKing => "advance_toward_black_king",
        EnemyIntentKind.CaptureBlackKing => "capture_black_king",
        EnemyIntentKind.CaptureNonKing => "capture_non_king",
        EnemyIntentKind.DefendWhiteKing => "defend_white_king",
        EnemyIntentKind.PressureBlackKing => "pressure_black_king",
        _ => throw new ArgumentOutOfRangeException(
            nameof(intentKind),
            intentKind,
            "Unknown enemy intent."),
    };
}

public sealed class PlannedEnemyIntent
{
    private readonly ReadOnlyCollection<CanonicalPoint> alternatePointView;

    private PlannedEnemyIntent(
        EnemyIntentKind? intentKind,
        EnemyTargetReference? targetReference,
        CanonicalPoint? primaryPoint,
        CanonicalPoint[] alternatePoints,
        bool retargetable,
        string plannedFromStateChecksum)
    {
        if (intentKind is EnemyIntentKind value && !Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(nameof(intentKind), intentKind, "Unknown enemy intent.");
        }

        ArgumentNullException.ThrowIfNull(alternatePoints);
        if (alternatePoints.Length > 2)
        {
            throw new ArgumentException("Enemy plan permits at most two alternate points.", nameof(alternatePoints));
        }

        if (intentKind is null)
        {
            if (targetReference is not null ||
                primaryPoint is not null ||
                alternatePoints.Length != 0 ||
                retargetable)
            {
                throw new ArgumentException("Pass plan cannot retain a target or placement points.");
            }
        }
        else if (targetReference is null || primaryPoint is null)
        {
            throw new ArgumentException("Placement plan requires a target and primary point.");
        }

        if (alternatePoints.Any(point => point is null) ||
            alternatePoints.Distinct().Count() != alternatePoints.Length ||
            (primaryPoint is not null && alternatePoints.Contains(primaryPoint)))
        {
            throw new ArgumentException(
                "Enemy alternate points must be non-null, unique, and exclude the primary point.",
                nameof(alternatePoints));
        }

        PlannedFromStateChecksum = ValidateChecksum(
            plannedFromStateChecksum,
            nameof(plannedFromStateChecksum));
        IntentKind = intentKind;
        TargetReference = targetReference;
        PrimaryPoint = primaryPoint;
        alternatePointView = Array.AsReadOnly((CanonicalPoint[])alternatePoints.Clone());
        Retargetable = retargetable;
        CanonicalText = CreateCanonicalText();
        Checksum = DeterministicChecksum.Sha256Hex(CanonicalText);
    }

    public EnemyIntentKind? IntentKind { get; }

    public bool IsPass => IntentKind is null;

    public string IntentId => IntentKind is EnemyIntentKind kind
        ? EnemyIntentKindRules.ToIntentId(kind)
        : "pass";

    public EnemyTargetReference? TargetReference { get; }

    public CanonicalPoint? PrimaryPoint { get; }

    public IReadOnlyList<CanonicalPoint> AlternatePoints => alternatePointView;

    public bool Retargetable { get; }

    public string PlannedFromStateChecksum { get; }

    public string CanonicalText { get; }

    public string Checksum { get; }

    public static PlannedEnemyIntent Create(
        EnemyIntentKind intentKind,
        EnemyTargetReference targetReference,
        CanonicalPoint primaryPoint,
        IEnumerable<CanonicalPoint> alternatePoints,
        bool retargetable,
        string plannedFromStateChecksum)
    {
        ArgumentNullException.ThrowIfNull(targetReference);
        ArgumentNullException.ThrowIfNull(primaryPoint);
        ArgumentNullException.ThrowIfNull(alternatePoints);
        return new(
            intentKind,
            targetReference,
            primaryPoint,
            alternatePoints.ToArray(),
            retargetable,
            plannedFromStateChecksum);
    }

    public static PlannedEnemyIntent Pass(string plannedFromStateChecksum) =>
        new(null, null, null, [], false, plannedFromStateChecksum);

    public string ToCanonicalText() => CanonicalText;

    private string CreateCanonicalText()
    {
        var lines = new List<string>
        {
            "planned-enemy-intent-v1",
            $"intent_id={IntentId}",
            $"retargetable={(Retargetable ? "1" : "0")}",
            $"planned_from_state_checksum={PlannedFromStateChecksum}",
            $"target={(TargetReference is null ? "none" : Encode(TargetReference.ToCanonicalText()))}",
            $"primary_point={PointText(PrimaryPoint)}",
            $"alternate_count={alternatePointView.Count.ToString(CultureInfo.InvariantCulture)}",
        };
        lines.AddRange(alternatePointView.Select(point => $"alternate_point={PointText(point)}"));
        return string.Join('\n', lines);
    }

    private static string PointText(CanonicalPoint? point) => point is null
        ? "none"
        : $"{point.X.ToString(CultureInfo.InvariantCulture)},{point.Y.ToString(CultureInfo.InvariantCulture)}";

    private static string Encode(string value) =>
        Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(value));

    private static string ValidateChecksum(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        if (value.Length != 64 || value.Any(character => !Uri.IsHexDigit(character)))
        {
            throw new ArgumentException(
                "Planned state checksum must contain exactly 64 hexadecimal digits.",
                parameterName);
        }

        return value.ToLowerInvariant();
    }
}
