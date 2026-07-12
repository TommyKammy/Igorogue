namespace Igorogue.Domain;

internal static class StableDomainId
{
    internal static string Validate(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        if (value.Any(character =>
                !char.IsAsciiLetterOrDigit(character) &&
                character is not '.' and not '_' and not '-'))
        {
            throw new ArgumentException(
                "Stable IDs may contain only ASCII letters, digits, '.', '_', or '-'.",
                parameterName);
        }

        return value;
    }
}
