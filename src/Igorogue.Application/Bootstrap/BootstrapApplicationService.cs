using Igorogue.Domain.Bootstrap;
using Igorogue.Domain.Determinism;

namespace Igorogue.Application.Bootstrap;

public sealed class BootstrapApplicationService
{
    public BootstrapSmokeResult Run(string contentHash)
    {
        ValidateContentHash(contentHash);

        var state = BootstrapState.CreateDefault();
        var checksum = DeterministicChecksum.Combine(
            "igorogue-bootstrap-v1",
            state.ToCanonicalText(),
            contentHash);

        return new BootstrapSmokeResult(state.ProjectId, contentHash, checksum);
    }

    private static void ValidateContentHash(string contentHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentHash);

        const string prefix = "sha256:";
        if (!contentHash.StartsWith(prefix, StringComparison.Ordinal) ||
            contentHash.Length != prefix.Length + 64 ||
            !contentHash[prefix.Length..].All(Uri.IsHexDigit))
        {
            throw new ArgumentException(
                "Content hash must use the form sha256:<64 lowercase-or-uppercase hex digits>.",
                nameof(contentHash));
        }
    }
}
