using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Crypto.Generators;

namespace Proton.Core.Auth.Srp;

/// <summary>
/// Ports go-srp's <c>HashPassword</c> family (hash.go): derives the SRP "x" exponent from a
/// user's password. All current (post-2018) Proton accounts use auth version 4, which salts
/// with 10 random bytes from the API plus the literal ASCII string "proton", bcrypts the
/// result, and feeds the bcrypt string plus the modulus through <see cref="SrpMath.ExpandHash"/>.
/// Versions 0-2 are legacy fallbacks kept for compatibility with old auth-info responses.
/// </summary>
public static class PasswordHasher
{
    private const int BcryptCost = 10;
    private const string BcryptVersion = "2y";

    public static byte[] HashPassword(int authVersion, string password, string username, byte[]? salt, byte[] modulus)
    {
        return authVersion switch
        {
            4 or 3 => HashVersion3(password, salt ?? throw new ArgumentException("Salt is required for auth version 3/4.", nameof(salt)), modulus),
            2 => HashVersion1(password, CleanUserName(username), modulus),
            1 => HashVersion1(password, username, modulus),
            0 => HashVersion0(password, username, modulus),
            _ => throw new NotSupportedException($"Unsupported SRP auth version {authVersion}."),
        };
    }

    /// <summary>Hashes a Proton mailbox (decryption) password: bcrypt(password, salt) with no expandHash step.</summary>
    public static byte[] HashMailboxPassword(string password, byte[] salt16)
    {
        string bcrypted = BcryptHash(password, salt16);
        return Encoding.ASCII.GetBytes(bcrypted);
    }

    private static byte[] HashVersion3(string password, byte[] salt, byte[] modulus)
    {
        byte[] salted = Combine(salt, Encoding.ASCII.GetBytes("proton"));
        string bcrypted = BcryptHash(password, salted);
        return SrpMath.ExpandHash(Encoding.ASCII.GetBytes(bcrypted), modulus);
    }

    private static byte[] HashVersion1(string password, string userName, byte[] modulus)
    {
        byte[] md5 = MD5.HashData(Encoding.UTF8.GetBytes(userName.ToLowerInvariant()));
        string hexSalt = Convert.ToHexString(md5).ToLowerInvariant();
        byte[] saltBytes = ProtonBase64.Decode(hexSalt, 16);
        string bcrypted = BcryptHash(password, saltBytes);
        return SrpMath.ExpandHash(Encoding.ASCII.GetBytes(bcrypted), modulus);
    }

    private static byte[] HashVersion0(string password, string userName, byte[] modulus)
    {
        byte[] userAndPassword = Combine(
            Encoding.UTF8.GetBytes(userName.ToLowerInvariant()),
            Encoding.UTF8.GetBytes(password));
        byte[] prehashed = SHA512.HashData(userAndPassword);
        string base64Hash = Convert.ToBase64String(prehashed);
        return HashVersion1(base64Hash, userName, modulus);
    }

    private static string CleanUserName(string userName)
    {
        return userName.Replace("-", string.Empty).Replace(".", string.Empty).Replace("_", string.Empty).ToLowerInvariant();
    }

    private static string BcryptHash(string password, byte[] salt16)
    {
        if (salt16.Length != 16)
        {
            throw new ArgumentException("Bcrypt salt must be exactly 16 bytes.", nameof(salt16));
        }

        return OpenBsdBCrypt.Generate(BcryptVersion, password.ToCharArray(), salt16, BcryptCost);
    }

    private static byte[] Combine(byte[] first, byte[] second)
    {
        byte[] result = new byte[first.Length + second.Length];
        Buffer.BlockCopy(first, 0, result, 0, first.Length);
        Buffer.BlockCopy(second, 0, result, first.Length, second.Length);
        return result;
    }
}
