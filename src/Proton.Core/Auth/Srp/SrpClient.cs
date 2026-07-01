using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;

namespace Proton.Core.Auth.Srp;

/// <summary>
/// Client-side implementation of Proton's modified SRP-6a protocol, ported from
/// ProtonMail/go-srp (srp.go). All integers are exchanged over the wire as 256-byte
/// (2048-bit) little-endian arrays; see <see cref="SrpMath"/> for the byte-order handling.
/// </summary>
public sealed class SrpClient
{
    private const int BitLength = 2048;
    private const int ByteLength = BitLength / 8;

    private static readonly BigInteger Generator = BigInteger.Two;
    private static readonly SecureRandom RandomSource = new();

    /// <summary>
    /// Runs the full client SRP exchange for a login attempt and returns the proofs to send
    /// to POST /auth/v4, along with the server proof expected back in the response.
    /// </summary>
    public SrpProofs GenerateProofs(byte[] modulus, byte[] serverEphemeral, byte[] hashedPassword)
    {
        BigInteger modulusInt = SrpMath.FromLittleEndian(modulus);
        BigInteger serverEphemeralInt = SrpMath.FromLittleEndian(serverEphemeral);

        ValidateModulusParameters(modulusInt, serverEphemeralInt);

        BigInteger modulusMinusOne = modulusInt.Subtract(BigInteger.One);
        BigInteger multiplier = ComputeMultiplier(modulusInt);
        BigInteger hashedPasswordInt = SrpMath.FromLittleEndian(hashedPassword);

        BigInteger clientSecret;
        byte[] clientEphemeralBytes;
        BigInteger scramblingParam;
        while (true)
        {
            clientSecret = GenerateClientSecret(modulusMinusOne);
            BigInteger clientEphemeralInt = Generator.ModPow(clientSecret, modulusInt);
            clientEphemeralBytes = SrpMath.ToLittleEndian(clientEphemeralInt, ByteLength);

            scramblingParam = SrpMath.FromLittleEndian(SrpMath.ExpandHash(clientEphemeralBytes, serverEphemeral));
            if (scramblingParam.SignValue != 0)
            {
                break;
            }
        }

        // base = (B - k * g^x mod N) mod N
        BigInteger gx = Generator.ModPow(hashedPasswordInt, modulusInt);
        BigInteger kgx = multiplier.Multiply(gx).Mod(modulusInt);
        BigInteger baseValue = serverEphemeralInt.Subtract(kgx).Mod(modulusInt);

        // exponent = (u * x + a) mod (N - 1)
        BigInteger exponent = scramblingParam.Multiply(hashedPasswordInt).Add(clientSecret).Mod(modulusMinusOne);

        BigInteger sharedSecretInt = baseValue.ModPow(exponent, modulusInt);
        byte[] sharedSecret = SrpMath.ToLittleEndian(sharedSecretInt, ByteLength);

        byte[] clientProof = SrpMath.ExpandHash(clientEphemeralBytes, serverEphemeral, sharedSecret);
        byte[] serverProof = SrpMath.ExpandHash(clientEphemeralBytes, clientProof, sharedSecret);

        return new SrpProofs
        {
            ClientEphemeral = clientEphemeralBytes,
            ClientProof = clientProof,
            ExpectedServerProof = serverProof,
            SharedSession = sharedSecret,
        };
    }

    /// <summary>Computes a password verifier (g^x mod N) for account creation / password changes.</summary>
    public byte[] GenerateVerifier(byte[] hashedPassword, byte[] modulus)
    {
        BigInteger modulusInt = SrpMath.FromLittleEndian(modulus);
        BigInteger hashedPasswordInt = SrpMath.FromLittleEndian(hashedPassword);
        BigInteger verifier = Generator.ModPow(hashedPasswordInt, modulusInt);
        return SrpMath.ToLittleEndian(verifier, ByteLength);
    }

    private static BigInteger ComputeMultiplier(BigInteger modulusInt)
    {
        byte[] gBytes = SrpMath.ToLittleEndian(Generator, ByteLength);
        byte[] nBytes = SrpMath.ToLittleEndian(modulusInt, ByteLength);
        BigInteger multiplier = SrpMath.FromLittleEndian(SrpMath.ExpandHash(gBytes, nBytes)).Mod(modulusInt);

        BigInteger modulusMinusOne = modulusInt.Subtract(BigInteger.One);
        if (multiplier.CompareTo(BigInteger.One) <= 0 || multiplier.CompareTo(modulusMinusOne) >= 0)
        {
            throw new InvalidOperationException("SRP multiplier is out of bounds.");
        }

        return multiplier;
    }

    private static BigInteger GenerateClientSecret(BigInteger modulusMinusOne)
    {
        BigInteger lowerBound = BigInteger.ValueOf(BitLength * 2L);
        BigInteger upperInclusive = modulusMinusOne.Subtract(BigInteger.One);

        while (true)
        {
            BigInteger candidate = BigIntegers.CreateRandomInRange(BigInteger.Zero, upperInclusive, RandomSource);
            if (candidate.CompareTo(lowerBound) > 0 && candidate.CompareTo(modulusMinusOne) < 0)
            {
                return candidate;
            }
        }
    }

    /// <summary>
    /// Validates the server-supplied modulus and ephemeral, mirroring go-srp's checkParams:
    /// the modulus must be a 2048-bit safe prime that is 3 mod 8 (so that g=2 generates the
    /// full group), and the server ephemeral must lie strictly within (1, N-1).
    /// </summary>
    private static void ValidateModulusParameters(BigInteger modulusInt, BigInteger serverEphemeralInt)
    {
        if (modulusInt.BitLength != BitLength)
        {
            throw new InvalidOperationException("SRP modulus has an incorrect bit length.");
        }

        if (!modulusInt.TestBit(0) || !modulusInt.TestBit(1) || modulusInt.TestBit(2))
        {
            throw new InvalidOperationException("SRP modulus is not congruent to 3 mod 8.");
        }

        BigInteger modulusMinusOne = modulusInt.Subtract(BigInteger.One);
        if (serverEphemeralInt.CompareTo(BigInteger.One) <= 0 || serverEphemeralInt.CompareTo(modulusMinusOne) >= 0)
        {
            throw new InvalidOperationException("SRP server ephemeral is out of bounds.");
        }

        BigInteger halfModulus = modulusInt.ShiftRight(1);
        if (!halfModulus.IsProbablePrime(64))
        {
            throw new InvalidOperationException("SRP modulus is not a safe prime.");
        }

        if (!Generator.ModPow(halfModulus, modulusInt).Equals(modulusMinusOne))
        {
            throw new InvalidOperationException("SRP modulus is not prime (Lucas test failed).");
        }
    }
}
