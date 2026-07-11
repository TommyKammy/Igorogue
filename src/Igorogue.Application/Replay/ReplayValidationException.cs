namespace Igorogue.Application.Replay;

public sealed class ReplayValidationException : IOException
{
    internal ReplayValidationException(
        string reasonId,
        string message,
        int? attemptIndex = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reasonId);
        ReasonId = reasonId;
        AttemptIndex = attemptIndex;
    }

    public string ReasonId { get; }

    public int? AttemptIndex { get; }
}
