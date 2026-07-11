using System.Collections.ObjectModel;

using Igorogue.Domain.Board;

namespace Igorogue.Domain.Facilities;

public sealed class FacilityInstance
{
    private readonly ReadOnlyCollection<string> explicitDisableSourceView;

    public FacilityInstance(
        string instanceId,
        string contentId,
        StoneColor owner,
        CanonicalPoint point,
        long buildSequence)
        : this(instanceId, contentId, owner, point, buildSequence, [])
    {
    }

    public FacilityInstance(
        string instanceId,
        string contentId,
        StoneColor owner,
        CanonicalPoint point,
        long buildSequence,
        IEnumerable<string> explicitDisableSources)
    {
        InstanceId = ValidateStableId(instanceId, nameof(instanceId));
        ContentId = ValidateStableId(contentId, nameof(contentId));
        if (owner is not StoneColor.Black and not StoneColor.White)
        {
            throw new ArgumentOutOfRangeException(nameof(owner), owner, "Unknown facility owner color.");
        }

        ArgumentNullException.ThrowIfNull(point);
        if (buildSequence <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(buildSequence),
                buildSequence,
                "Facility build sequence must be positive.");
        }

        ArgumentNullException.ThrowIfNull(explicitDisableSources);
        var canonicalSources = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var source in explicitDisableSources)
        {
            canonicalSources.Add(ValidateStableId(source, nameof(explicitDisableSources)));
        }

        Owner = owner;
        Point = point;
        BuildSequence = buildSequence;
        explicitDisableSourceView = Array.AsReadOnly(canonicalSources.ToArray());
    }

    public string InstanceId { get; }

    public string ContentId { get; }

    public StoneColor Owner { get; }

    public CanonicalPoint Point { get; }

    public long BuildSequence { get; }

    public IReadOnlyList<string> ExplicitDisableSources => explicitDisableSourceView;

    public bool IsExplicitlyDisabled => explicitDisableSourceView.Count > 0;

    internal static string ValidateStableId(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        if (value.Any(character =>
                !char.IsAsciiLetterOrDigit(character) && character is not '.' and not '_' and not '-'))
        {
            throw new ArgumentException(
                "Stable IDs may contain only ASCII letters, digits, '.', '_', or '-'.",
                parameterName);
        }

        return value;
    }
}
