using System.Security.Cryptography;
using System.Text;

namespace SmartRx.Data;

public static class SimpleHasher
{
    public static string CreateSalt(int size = 16)
    {
        var bytes = RandomNumberGenerator.GetBytes(size);
        return Convert.ToBase64String(bytes);
    }

    public static string Hash(string input, string salt)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input + salt);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    public static bool Verify(string input, string salt, string hash) => Hash(input, salt) == hash;
}
