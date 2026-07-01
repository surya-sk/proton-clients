using Proton.Core.Crypto;
using Xunit;

namespace Proton.Core.Tests.Crypto;

public class OpenPgpServiceTests
{
    // 2048-bit RSA keeps this test fast; production key generation should use a larger size.
    private const int TestKeyBits = 2048;

    [Fact]
    public void EncryptThenDecrypt_RoundTripsPlaintext()
    {
        var pgp = new OpenPgpService();
        OpenPgpService.PgpKeyPair keyPair = pgp.GenerateKeyPair("Test User <test@example.com>", "correct horse battery staple", TestKeyBits);

        string ciphertext = pgp.Encrypt("Hello, Proton!", keyPair.ArmoredPublicKey);
        string plaintext = pgp.Decrypt(ciphertext, keyPair.ArmoredPrivateKey, "correct horse battery staple");

        Assert.Equal("Hello, Proton!", plaintext);
    }

    [Fact]
    public void Decrypt_WithWrongPassphrase_Throws()
    {
        var pgp = new OpenPgpService();
        OpenPgpService.PgpKeyPair keyPair = pgp.GenerateKeyPair("Test User <test@example.com>", "correct passphrase", TestKeyBits);

        string ciphertext = pgp.Encrypt("secret message", keyPair.ArmoredPublicKey);

        Assert.ThrowsAny<Exception>(() => pgp.Decrypt(ciphertext, keyPair.ArmoredPrivateKey, "wrong passphrase"));
    }

    [Fact]
    public void GenerateKeyPair_ProducesArmoredKeys()
    {
        var pgp = new OpenPgpService();
        OpenPgpService.PgpKeyPair keyPair = pgp.GenerateKeyPair("Test User <test@example.com>", "passphrase", TestKeyBits);

        Assert.Contains("BEGIN PGP PUBLIC KEY BLOCK", keyPair.ArmoredPublicKey);
        Assert.Contains("BEGIN PGP PRIVATE KEY BLOCK", keyPair.ArmoredPrivateKey);
    }
}
