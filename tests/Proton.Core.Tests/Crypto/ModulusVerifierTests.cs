using Proton.Core.Crypto;
using Xunit;

namespace Proton.Core.Tests.Crypto;

/// <summary>
/// Vectors copied verbatim from go-srp's srp_test.go (testModulus / testModulusClearSign),
/// signed by Proton's real modulus-signing key. Confirms our BouncyCastle-based clearsign
/// verification (ClearSignVerifier/ModulusVerifier) accepts the same message go-crypto/openpgp
/// accepts, and rejects a tampered one.
/// </summary>
public class ModulusVerifierTests
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
    public void VerifyAndDecode_ValidSignature_ReturnsExpectedModulus()
    {
        byte[] modulus = ModulusVerifier.VerifyAndDecode(ValidSignedModulus);

        Assert.Equal(Convert.FromBase64String(ExpectedModulusBase64), modulus);
        Assert.Equal(256, modulus.Length); // 2048-bit modulus.
    }

    [Fact]
    public void VerifyAndDecode_TamperedSignature_Throws()
    {
        string tampered = ValidSignedModulus.Replace(
            "wl4EARYIABAFAlwB1j0JEDUFhcTpUY8mAAD8CgEAnsFnF4cF0uSHKkXa1GIa",
            "wl4EARYIABAFAlwB1j0JEDUFhcTpUY8mAAD8CgEAnsFnF4cF0uSHKkXaXXXX");

        Assert.Throws<ClearSignVerifier.ClearSignVerificationException>(
            () => ModulusVerifier.VerifyAndDecode(tampered));
    }

    [Fact]
    public void VerifyAndDecode_TamperedContent_Throws()
    {
        string tampered = ValidSignedModulus.Replace(ExpectedModulusBase64, ExpectedModulusBase64[..^2] + "AA");

        Assert.Throws<ClearSignVerifier.ClearSignVerificationException>(
            () => ModulusVerifier.VerifyAndDecode(tampered));
    }
}
