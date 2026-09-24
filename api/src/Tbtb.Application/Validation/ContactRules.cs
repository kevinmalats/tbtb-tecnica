using System.Globalization;
using System.Text.RegularExpressions;

namespace Tbtb.Application.Validation;

public static partial class ContactRules
{
    public static readonly IReadOnlySet<string> Channels = new HashSet<string>(StringComparer.Ordinal) { "LLAMADA", "WHATSAPP", "CORREO" };
    public static readonly IReadOnlySet<string> Results = new HashSet<string>(StringComparer.Ordinal) { "CONTACTADO", "SIN_RESPUESTA", "FALLIDO" };

    public static bool IsChannel(string? value) => value is not null && Channels.Contains(value);
    public static bool IsResult(string? value) => value is not null && Results.Contains(value);

    public static bool TryInstant(string? value, out DateTimeOffset instant)
    {
        instant = default;
        return value is not null
            && InstantPattern().IsMatch(value)
            && DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out instant);
    }

    public static bool TryVersion(string? value, out byte[] version)
    {
        try
        {
            version = Convert.FromBase64String(value ?? string.Empty);
            return version.Length == 8;
        }
        catch (FormatException)
        {
            version = [];
            return false;
        }
    }

    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d{1,3})?(Z|[+-]\d{2}:\d{2})$", RegexOptions.CultureInvariant)]
    private static partial Regex InstantPattern();
}
