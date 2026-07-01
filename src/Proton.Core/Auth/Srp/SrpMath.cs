using System.Security.Cryptography;
using Org.BouncyCastle.Math;

namespace Proton.Core.Auth.Srp;

/// <summary>
/// Low level numeric helpers for Proton's SRP variant.
/// Proton transmits all SRP integers (modulus, ephemerals, salts) as fixed-width
/// little-endian byte arrays, which is the opposite of RFC 5054's big-endian convention -
/// every conversion here must reverse the byte order to match the server.
/// </summary>
internal static class SrpMath
{
    public static BigInteger FromLittleEndian(byte[] littleEndianBytes)
    {
        byte[] bigEndian = (byte[])littleEndianBytes.Clone();
        Array.Reverse(bigEndian);
        return new BigInteger(1, bigEndian);
    }

    public static byte[] ToLittleEndian(BigInteger value, int byteLength)
    {
        byte[] unsigned = value.ToByteArrayUnsigned();
        if (unsigned.Length > byteLength)
        {
            throw new InvalidOperationException(
                $"Value requires {unsigned.Length} bytes but only {byteLength} are available.");
        }

        byte[] bigEndian = new byte[byteLength];
        Array.Copy(unsigned, 0, bigEndian, byteLength - unsigned.Length, unsigned.Length);
        Array.Reverse(bigEndian);
        return bigEndian;
    }

    /// <summary>
    /// Proton's "expandHash": four SHA-512 digests of (data || suffix) for suffix 0..3,
    /// concatenated into a 256-byte value used both as the SRP hashed-password exponent
    /// and as the hash for the multiplier/scrambler/proof computations.
    /// </summary>
    public static byte[] ExpandHash(params byte[][] parts)
    {
        int totalLength = 0;
        foreach (byte[] part in parts)
        {
            totalLength += part.Length;
        }

        byte[] data = new byte[totalLength];
        int offset = 0;
        foreach (byte[] part in parts)
        {
            Buffer.BlockCopy(part, 0, data, offset, part.Length);
            offset += part.Length;
        }

        byte[] result = new byte[256];
        byte[] withSuffix = new byte[data.Length + 1];
        Buffer.BlockCopy(data, 0, withSuffix, 0, data.Length);

        using SHA512 sha512 = SHA512.Create();
        for (byte suffix = 0; suffix < 4; suffix++)
        {
            withSuffix[data.Length] = suffix;
            byte[] hash = sha512.ComputeHash(withSuffix);
            Buffer.BlockCopy(hash, 0, result, suffix * 64, 64);
        }

        return result;
    }
}
