using System.Security.Cryptography;

namespace RaceManager.Application.Security;

public static class PasswordSecurity
{
    private const string Algorithm = "pbkdf2-sha256";
    private const int Iterations = 120_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public static string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        return $"{Algorithm}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string storedValue, out bool requiresUpgrade)
    {
        requiresUpgrade = false;
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedValue)) return false;

        var parts = storedValue.Split('$');
        if (parts.Length != 4 || !parts[0].Equals(Algorithm, StringComparison.Ordinal))
        {
            requiresUpgrade = CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.UTF8.GetBytes(password),
                System.Text.Encoding.UTF8.GetBytes(storedValue));
            return requiresUpgrade;
        }

        if (!int.TryParse(parts[1], out var iterations) || iterations < 1) return false;
        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
            requiresUpgrade = iterations < Iterations;
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
