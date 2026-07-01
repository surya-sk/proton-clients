using System.Linq;
using System.Text;
using Org.BouncyCastle.Bcpg;
using Org.BouncyCastle.Bcpg.OpenPgp;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities.IO;

namespace Proton.Core.Crypto;

/// <summary>
/// General-purpose OpenPGP (RFC 4880) operations built on BouncyCastle, following the
/// canonical patterns from BouncyCastle's own PGP examples (RsaKeyRingGenerator,
/// KeyBasedFileProcessor). This handles armored key generation plus integrity-checked
/// encrypt/decrypt of arbitrary payloads.
///
/// Proton's own address/mailbox keys additionally carry Proton-specific token wrapping and
/// (for newer keys) Curve25519/Argon2 locking handled by gopenpgp - that mailbox-key
/// integration belongs to the Mail-specific crypto layer built on top of this service, not
/// here.
/// </summary>
public sealed class OpenPgpService
{
    private const int DefaultRsaKeyBits = 4096;
    private static readonly SecureRandom Random = new();

    public sealed class PgpKeyPair
    {
        public required string ArmoredPublicKey { get; init; }
        public required string ArmoredPrivateKey { get; init; }
    }

    public PgpKeyPair GenerateKeyPair(string identity, string passphrase, int rsaKeyBits = DefaultRsaKeyBits)
    {
        IAsymmetricCipherKeyPairGenerator keyPairGenerator = GeneratorUtilities.GetKeyPairGenerator("RSA");
        keyPairGenerator.Init(new RsaKeyGenerationParameters(BigInteger.ValueOf(0x10001), Random, rsaKeyBits, 80));
        AsymmetricCipherKeyPair keyPair = keyPairGenerator.GenerateKeyPair();

        var secretKey = new PgpSecretKey(
            PgpSignature.DefaultCertification,
            PublicKeyAlgorithmTag.RsaGeneral,
            keyPair.Public,
            keyPair.Private,
            DateTime.UtcNow,
            identity,
            SymmetricKeyAlgorithmTag.Aes256,
            passphrase.ToCharArray(),
            null,
            null,
            Random);

        using var secretOut = new MemoryStream();
        using (var armoredSecretOut = new ArmoredOutputStream(secretOut))
        {
            secretKey.Encode(armoredSecretOut);
        }

        using var publicOut = new MemoryStream();
        using (var armoredPublicOut = new ArmoredOutputStream(publicOut))
        {
            secretKey.PublicKey.Encode(armoredPublicOut);
        }

        return new PgpKeyPair
        {
            ArmoredPublicKey = Encoding.ASCII.GetString(publicOut.ToArray()),
            ArmoredPrivateKey = Encoding.ASCII.GetString(secretOut.ToArray()),
        };
    }

    /// <summary>Encrypts and armors <paramref name="plaintext"/> for the given recipient public key.</summary>
    public string Encrypt(string plaintext, string armoredPublicKey)
    {
        PgpPublicKey encryptionKey = ReadFirstEncryptionKey(armoredPublicKey);
        byte[] payload = Encoding.UTF8.GetBytes(plaintext);
        byte[] compressed = CompressAsLiteralData(payload);

        using var output = new MemoryStream();
        using (var armoredOut = new ArmoredOutputStream(output))
        {
            var encryptedDataGenerator = new PgpEncryptedDataGenerator(
                SymmetricKeyAlgorithmTag.Aes256, withIntegrityPacket: true, Random);
            encryptedDataGenerator.AddMethod(encryptionKey);

            using Stream encryptedOut = encryptedDataGenerator.Open(armoredOut, compressed.Length);
            encryptedOut.Write(compressed, 0, compressed.Length);
        }

        return Encoding.ASCII.GetString(output.ToArray());
    }

    /// <summary>Decrypts an armored OpenPGP message using the given private key and passphrase.</summary>
    public string Decrypt(string armoredCiphertext, string armoredPrivateKey, string passphrase)
    {
        using Stream decoderStream = PgpUtilities.GetDecoderStream(
            new MemoryStream(Encoding.UTF8.GetBytes(armoredCiphertext)));
        var factory = new PgpObjectFactory(decoderStream);

        PgpObject firstObject = factory.NextPgpObject();
        PgpEncryptedDataList encryptedDataList = firstObject is PgpEncryptedDataList list
            ? list
            : (PgpEncryptedDataList)factory.NextPgpObject();

        var secretKeyRingBundle = new PgpSecretKeyRingBundle(
            PgpUtilities.GetDecoderStream(new MemoryStream(Encoding.UTF8.GetBytes(armoredPrivateKey))));

        PgpPrivateKey? privateKey = null;
        PgpPublicKeyEncryptedData? encryptedData = null;
        foreach (PgpPublicKeyEncryptedData candidate in encryptedDataList.GetEncryptedDataObjects().Cast<PgpPublicKeyEncryptedData>())
        {
            PgpSecretKey? secretKey = secretKeyRingBundle.GetSecretKey(candidate.KeyId);
            if (secretKey is null)
            {
                continue;
            }

            privateKey = secretKey.ExtractPrivateKey(passphrase.ToCharArray());
            encryptedData = candidate;
            break;
        }

        if (privateKey is null || encryptedData is null)
        {
            throw new InvalidOperationException("No matching private key found for this message.");
        }

        using Stream clearStream = encryptedData.GetDataStream(privateKey);
        var plainFactory = new PgpObjectFactory(clearStream);
        PgpObject message = plainFactory.NextPgpObject();

        if (message is PgpCompressedData compressedData)
        {
            var decompressedFactory = new PgpObjectFactory(compressedData.GetDataStream());
            message = decompressedFactory.NextPgpObject();
        }

        if (message is not PgpLiteralData literalData)
        {
            throw new InvalidOperationException("Encrypted message did not contain literal data.");
        }

        using var resultStream = new MemoryStream();
        using (Stream literalStream = literalData.GetInputStream())
        {
            Streams.PipeAll(literalStream, resultStream);
        }

        if (encryptedData.IsIntegrityProtected() && !encryptedData.Verify())
        {
            throw new InvalidOperationException("Message failed integrity verification.");
        }

        return Encoding.UTF8.GetString(resultStream.ToArray());
    }

    private static byte[] CompressAsLiteralData(byte[] payload)
    {
        using var compressedOut = new MemoryStream();
        var compressor = new PgpCompressedDataGenerator(CompressionAlgorithmTag.Zip);
        using (Stream literalOut = compressor.Open(compressedOut))
        {
            var literalDataGenerator = new PgpLiteralDataGenerator();
            using Stream payloadOut = literalDataGenerator.Open(
                literalOut, PgpLiteralData.Binary, "", payload.Length, DateTime.UtcNow);
            payloadOut.Write(payload, 0, payload.Length);
        }

        return compressedOut.ToArray();
    }

    private static PgpPublicKey ReadFirstEncryptionKey(string armoredPublicKey)
    {
        using Stream decoderStream = PgpUtilities.GetDecoderStream(
            new MemoryStream(Encoding.UTF8.GetBytes(armoredPublicKey)));
        var bundle = new PgpPublicKeyRingBundle(decoderStream);

        foreach (PgpPublicKeyRing keyRing in bundle.GetKeyRings().Cast<PgpPublicKeyRing>())
        {
            foreach (PgpPublicKey key in keyRing.GetPublicKeys().Cast<PgpPublicKey>())
            {
                if (key.IsEncryptionKey)
                {
                    return key;
                }
            }
        }

        throw new InvalidOperationException("No encryption-capable key found in the provided public key.");
    }
}
