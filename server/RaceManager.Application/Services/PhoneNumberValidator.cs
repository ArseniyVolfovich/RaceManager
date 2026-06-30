using System.Text.RegularExpressions;

namespace RaceManager.Application.Services;

internal static partial class PhoneNumberValidator
{
    public const string FormatHint = "+375 25 123 45 67 или +7 900 123-45-67";

    [GeneratedRegex(@"^(?:\+375\s(?:24|25|29|33|44)\s\d{3}(?:\s\d{2}\s\d{2}|-\d{2}-\d{2}|\d{4})|\+7\s9\d{2}\s\d{3}(?:\s\d{2}\s\d{2}|-\d{2}-\d{2}|\d{4}))$", RegexOptions.CultureInvariant)]
    private static partial Regex SupportedPhonePattern();

    public static bool IsValid(string? value) =>
        !string.IsNullOrWhiteSpace(value) && SupportedPhonePattern().IsMatch(value.Trim());

    public static void EnsureValid(string? value)
    {
        if (!IsValid(value))
        {
            throw new InvalidOperationException($"Введите телефон в формате {FormatHint}.");
        }
    }
}
