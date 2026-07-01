namespace Proton.Core.Crypto;

/// <summary>
/// Verifies the SRP modulus the API returns from /auth/v4/info, which is always sent as an
/// OpenPGP clear-signed message so a compromised transport cannot substitute a malicious
/// modulus. The signing key below is Proton's fixed "srp.modulus" key, copied verbatim from
/// ProtonMail/go-srp (srp.go's <c>modulusPubkey</c>) - it is public infrastructure, not a secret.
/// </summary>
public static class ModulusVerifier
{
    private const string ModulusSigningPublicKey =
        "-----BEGIN PGP PUBLIC KEY BLOCK-----\r\n\r\n" +
        "xjMEXAHLgxYJKwYBBAHaRw8BAQdAFurWXXwjTemqjD7CXjXVyKf0of7n9Ctm\r\n" +
        "L8v9enkzggHNEnByb3RvbkBzcnAubW9kdWx1c8J3BBAWCgApBQJcAcuDBgsJ\r\n" +
        "BwgDAgkQNQWFxOlRjyYEFQgKAgMWAgECGQECGwMCHgEAAPGRAP9sauJsW12U\r\n" +
        "MnTQUZpsbJb53d0Wv55mZIIiJL2XulpWPQD/V6NglBd96lZKBmInSXX/kXat\r\n" +
        "Sv+y0io+LR8i2+jV+AbOOARcAcuDEgorBgEEAZdVAQUBAQdAeJHUz1c9+KfE\r\n" +
        "kSIgcBRE3WuXC4oj5a2/U3oASExGDW4DAQgHwmEEGBYIABMFAlwBy4MJEDUF\r\n" +
        "hcTpUY8mAhsMAAD/XQD8DxNI6E78meodQI+wLsrKLeHn32iLvUqJbVDhfWSU\r\n" +
        "WO4BAMcm1u02t4VKw++ttECPt+HUgPUq5pqQWe5Q2cW4TMsE\r\n" +
        "=Y4Mw\r\n" +
        "-----END PGP PUBLIC KEY BLOCK-----";

    /// <summary>
    /// Verifies the clear-signed modulus and returns the raw modulus bytes, decoded from the
    /// base64 cleartext payload.
    /// </summary>
    public static byte[] VerifyAndDecode(string signedModulus)
    {
        string base64Modulus = ClearSignVerifier.VerifyAndExtract(signedModulus, ModulusSigningPublicKey).Trim();
        return Convert.FromBase64String(base64Modulus);
    }
}
