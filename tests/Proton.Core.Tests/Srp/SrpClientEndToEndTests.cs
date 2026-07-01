using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;
using Proton.Core.Auth.Srp;
using Proton.Core.Crypto;
using Xunit;

namespace Proton.Core.Tests.Srp;

/// <summary>
/// go-srp's own test suite (TestSRPauth) seeds Go's math/rand with a fixed source to get
/// reproducible ephemerals, which we cannot replicate from .NET. Instead, mirroring go-srp's
/// TestE2EFlow, this drives <see cref="SrpClient"/> against a minimal from-scratch reference
/// server (server.go, ported inline below) over Proton's real 2048-bit modulus, so both sides
/// use independent randomness and we're verifying the client's math is self-consistent with the
/// wire protocol rather than replaying a fixed transcript.
/// </summary>
public class SrpClientEndToEndTests
{
    private const string ExpectedModulusBase64 =
        "W2z5HBi8RvsfYzZTS7qBaUxxPhsfHJFZpu3Kd6s1JafNrCCH9rfvPLrfuqocxWPgWDH2R8neK7PkNvjxto9TStuY5z7jAzWRvFWN9cQhAKkdWgy0JY6ywVn22+HFpF4cYesHrqFIKUPDMSSIlWjBVmEJZ/MusD44ZT29xcPrOqeZvwtCffKtGAIjLYPZIEbZKnDM1Dm3q2K/xS5h+xdhjnndhsrkwm9U9oyA2wxzSXFL+pdfj2fOdRwuR5nW0J2NFrq3kJjkRmpO/Genq1UW+TEknIWAb6VzJJJA244K/H8cnSx2+nSNZO3bbo6Ys228ruV9A8m6DhxmS+bihN3ttQ==";

    private const string ValidSignedModulus =
        "-----BEGIN PGP SIGNED MESSAGE-----\n" +
        "Hash: SHA256\n" +
        "\n" +
        ExpectedModulusBase64 + "\n" +
        "-----BEGIN PGP SIGNATURE-----\n" +
        "Version: ProtonMail\n" +
        "Comment: https://protonmail.com\n" +
        "\n" +
        "wl4EARYIABAFAlwB1j0JEDUFhcTpUY8mAAD8CgEAnsFnF4cF0uSHKkXa1GIa\n" +
        "GO86yMV4zDZEZcDSJo0fgr8A/AlupGN9EdHlsrZLmTA1vhIx+rOgxdEff28N\n" +
        "kvNM7qIK\n" +
        "=q6vu\n" +
        "-----END PGP SIGNATURE-----";

    [Fact]
    public void ClientAndReferenceServer_AgreeOnSharedSessionAndProofs()
    {
        byte[] modulus = ModulusVerifier.VerifyAndDecode(ValidSignedModulus);
        byte[] salt = RandomBytes(10);
        byte[] hashedPassword = PasswordHasher.HashPassword(4, "Correct-Horse-Battery-Staple", "testuser", salt, modulus);

        var client = new SrpClient();
        byte[] verifier = client.GenerateVerifier(hashedPassword, modulus);

        var server = new ReferenceSrpServer(modulus, verifier);
        byte[] serverEphemeral = server.GenerateChallenge();

        SrpProofs proofs = client.GenerateProofs(modulus, serverEphemeral, hashedPassword);

        byte[] serverProof = server.VerifyClientProofs(proofs.ClientEphemeral, proofs.ClientProof);

        Assert.Equal(proofs.ExpectedServerProof, serverProof);
        Assert.Equal(server.SharedSession, proofs.SharedSession);
    }

    [Fact]
    public void ClientProof_WithWrongPassword_IsRejectedByReferenceServer()
    {
        byte[] modulus = ModulusVerifier.VerifyAndDecode(ValidSignedModulus);
        byte[] salt = RandomBytes(10);
        byte[] correctHash = PasswordHasher.HashPassword(4, "correct-password", "testuser", salt, modulus);
        byte[] wrongHash = PasswordHasher.HashPassword(4, "wrong-password", "testuser", salt, modulus);

        var client = new SrpClient();
        byte[] verifier = client.GenerateVerifier(correctHash, modulus);

        var server = new ReferenceSrpServer(modulus, verifier);
        byte[] serverEphemeral = server.GenerateChallenge();

        SrpProofs badProofs = client.GenerateProofs(modulus, serverEphemeral, wrongHash);

        Assert.Throws<InvalidOperationException>(
            () => server.VerifyClientProofs(badProofs.ClientEphemeral, badProofs.ClientProof));
    }

    private static byte[] RandomBytes(int byteCount)
    {
        byte[] bytes = new byte[byteCount];
        Random.Shared.NextBytes(bytes);
        return bytes;
    }

    /// <summary>Minimal from-scratch port of go-srp's server.go, for test purposes only.</summary>
    private sealed class ReferenceSrpServer
    {
        private const int BitLength = 2048;
        private const int ByteLength = BitLength / 8;
        private static readonly BigInteger Generator = BigInteger.Two;
        private static readonly SecureRandom RandomSource = new();

        private readonly BigInteger _modulus;
        private readonly BigInteger _verifier;
        private readonly BigInteger _multiplier;
        private readonly BigInteger _serverSecret;
        private BigInteger? _serverEphemeral;

        public byte[]? SharedSession { get; private set; }

        public ReferenceSrpServer(byte[] modulus, byte[] verifier)
        {
            _modulus = SrpMath.FromLittleEndian(modulus);
            _verifier = SrpMath.FromLittleEndian(verifier);
            _multiplier = ComputeMultiplier(_modulus);
            _serverSecret = BigIntegers.CreateRandomInRange(
                BigInteger.Zero, _modulus.Subtract(BigInteger.Two), RandomSource);
        }

        public byte[] GenerateChallenge()
        {
            // B = (k*v + g^b) mod N
            BigInteger kv = _multiplier.Multiply(_verifier).Mod(_modulus);
            BigInteger gb = Generator.ModPow(_serverSecret, _modulus);
            _serverEphemeral = kv.Add(gb).Mod(_modulus);
            return SrpMath.ToLittleEndian(_serverEphemeral, ByteLength);
        }

        public byte[] VerifyClientProofs(byte[] clientEphemeralBytes, byte[] clientProofBytes)
        {
            if (_serverEphemeral is null)
            {
                throw new InvalidOperationException("Call GenerateChallenge first.");
            }

            BigInteger clientEphemeral = SrpMath.FromLittleEndian(clientEphemeralBytes);
            BigInteger modulusMinusOne = _modulus.Subtract(BigInteger.One);
            if (clientEphemeral.CompareTo(BigInteger.One) <= 0 || clientEphemeral.CompareTo(modulusMinusOne) >= 0)
            {
                throw new InvalidOperationException("Client ephemeral is out of bounds.");
            }

            byte[] serverEphemeralBytes = SrpMath.ToLittleEndian(_serverEphemeral, ByteLength);
            BigInteger scramble = SrpMath.FromLittleEndian(SrpMath.ExpandHash(clientEphemeralBytes, serverEphemeralBytes));

            // base = A * v^u mod N ; S = base^b mod N
            BigInteger baseValue = clientEphemeral.Multiply(_verifier.ModPow(scramble, _modulus)).Mod(_modulus);
            BigInteger sharedSecretInt = baseValue.ModPow(_serverSecret, _modulus);
            byte[] sharedSecret = SrpMath.ToLittleEndian(sharedSecretInt, ByteLength);

            byte[] expectedClientProof = SrpMath.ExpandHash(clientEphemeralBytes, serverEphemeralBytes, sharedSecret);
            if (!expectedClientProof.AsSpan().SequenceEqual(clientProofBytes))
            {
                throw new InvalidOperationException("Client SRP proof is invalid.");
            }

            SharedSession = sharedSecret;
            return SrpMath.ExpandHash(clientEphemeralBytes, clientProofBytes, sharedSecret);
        }

        private static BigInteger ComputeMultiplier(BigInteger modulus)
        {
            byte[] gBytes = SrpMath.ToLittleEndian(Generator, ByteLength);
            byte[] nBytes = SrpMath.ToLittleEndian(modulus, ByteLength);
            return SrpMath.FromLittleEndian(SrpMath.ExpandHash(gBytes, nBytes)).Mod(modulus);
        }
    }
}
