using System.Security.Cryptography;
using System.Text;

namespace IdentityService.Extension;

public static class StringSecureEquals
{
    public static bool SecureEquals(string a, string b)
    {
        var aBytes = Encoding.UTF8.GetBytes(a);
        var bBytes = Encoding.UTF8.GetBytes(b);

        return CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
    }
}