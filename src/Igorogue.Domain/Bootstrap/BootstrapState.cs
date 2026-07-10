namespace Igorogue.Domain.Bootstrap;

public sealed record BootstrapState(
    int SchemaVersion,
    string ProjectId,
    string RulesStatus)
{
    public static BootstrapState CreateDefault() =>
        new(1, "igorogue", "rules-kernel-not-yet-implemented");

    public string ToCanonicalText() =>
        $"schema={SchemaVersion}\nproject={ProjectId}\nrules={RulesStatus}\n";
}
