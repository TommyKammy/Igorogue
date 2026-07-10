namespace Igorogue.Application.Replay;

public interface ICanonicalCommand
{
    string CommandType { get; }

    int CommandSchemaVersion { get; }

    string ToCanonicalPayload();
}
