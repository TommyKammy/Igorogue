using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Igorogue.Domain.Determinism;

public static class DeterministicChecksum
{
    public static string Sha256Hex(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return Sha256Hex(Encoding.UTF8.GetBytes(value));
    }

    public static string Sha256Hex(ReadOnlySpan<byte> value)
    {
        Span<byte> hash = stackalloc byte[SHA256.HashSizeInBytes];
        SHA256.HashData(value, hash);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static string Combine(params string[] components)
    {
        ArgumentNullException.ThrowIfNull(components);

        using var stream = new MemoryStream();
        Span<byte> lengthBuffer = stackalloc byte[sizeof(int)];

        foreach (var component in components)
        {
            ArgumentNullException.ThrowIfNull(component);
            var bytes = Encoding.UTF8.GetBytes(component);
            BinaryPrimitives.WriteInt32LittleEndian(lengthBuffer, bytes.Length);
            stream.Write(lengthBuffer);
            stream.Write(bytes);
        }

        return Sha256Hex(stream.ToArray());
    }
}
