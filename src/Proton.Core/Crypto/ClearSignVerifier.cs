using System.Text;
using Org.BouncyCastle.Bcpg;
using Org.BouncyCastle.Bcpg.OpenPgp;

namespace Proton.Core.Crypto;

/// <summary>
/// Verifies OpenPGP clear-signed ("-----BEGIN PGP SIGNED MESSAGE-----") text against a known
/// public key and returns the signed cleartext. Ported from BouncyCastle's own
/// ClearSignedFileProcessor example, adapted to work in-memory against a fixed public key
/// instead of files - this is what go-srp's <c>readClearSignedMessage</c> does for the SRP
/// modulus, using go-crypto/openpgp/clearsign underneath.
/// </summary>
public static class ClearSignVerifier
{
    public sealed class ClearSignVerificationException : Exception
    {
        public ClearSignVerificationException(string message) : base(message)
        {
        }
    }

    /// <summary>Returns the verified cleartext, or throws if the signature is missing/invalid.</summary>
    public static string VerifyAndExtract(string clearSignedMessage, string armoredPublicKey)
    {
        var armoredIn = new ArmoredInputStream(new MemoryStream(Encoding.UTF8.GetBytes(clearSignedMessage)));

        using var cleartext = new MemoryStream();
        var line = new MemoryStream();
        int lookAhead = ReadInputLine(line, armoredIn);

        if (lookAhead == -1 || !armoredIn.IsClearText())
        {
            throw new ClearSignVerificationException("Input is not a clear-signed OpenPGP message.");
        }

        AppendCanonicalLine(cleartext, line.ToArray());
        while (lookAhead != -1 && armoredIn.IsClearText())
        {
            lookAhead = ReadInputLine(line, lookAhead, armoredIn);
            AppendCanonicalLine(cleartext, line.ToArray());
        }

        byte[] signedContent = cleartext.ToArray();

        PgpPublicKeyRingBundle keyRingBundle;
        using (Stream keyStream = PgpUtilities.GetDecoderStream(
                   new MemoryStream(Encoding.UTF8.GetBytes(armoredPublicKey))))
        {
            keyRingBundle = new PgpPublicKeyRingBundle(keyStream);
        }

        // The signature packet immediately follows the cleartext section in the same armored stream.
        var pgpFactory = new PgpObjectFactory(armoredIn);
        if (pgpFactory.NextPgpObject() is not PgpSignatureList signatureList || signatureList.Count == 0)
        {
            throw new ClearSignVerificationException("Clear-signed message did not contain a signature.");
        }

        PgpSignature signature = signatureList[0];
        PgpPublicKey? publicKey = keyRingBundle.GetPublicKey(signature.KeyId);
        if (publicKey is null)
        {
            throw new ClearSignVerificationException("Signature key does not match the expected public key.");
        }

        signature.InitVerify(publicKey);
        UpdateSignatureWithCleartext(signature, signedContent);

        if (!signature.Verify())
        {
            throw new ClearSignVerificationException("Clear-signed message signature is invalid.");
        }

        return Encoding.UTF8.GetString(signedContent);
    }

    private static void UpdateSignatureWithCleartext(PgpSignature signature, byte[] canonicalContent)
    {
        using var contentStream = new MemoryStream(canonicalContent);
        var line = new MemoryStream();
        int lookAhead = ReadInputLine(line, contentStream);

        ProcessSignatureLine(signature, line.ToArray());

        while (lookAhead != -1)
        {
            lookAhead = ReadInputLine(line, lookAhead, contentStream);
            signature.Update((byte)'\r');
            signature.Update((byte)'\n');
            ProcessSignatureLine(signature, line.ToArray());
        }
    }

    private static void ProcessSignatureLine(PgpSignature signature, byte[] line)
    {
        int length = GetLengthWithoutTrailingWhitespace(line);
        if (length > 0)
        {
            signature.Update(line, 0, length);
        }
    }

    private static void AppendCanonicalLine(Stream target, byte[] line)
    {
        int length = GetLengthWithoutTrailingWhitespace(line);
        target.Write(line, 0, length);
        target.WriteByte((byte)'\r');
        target.WriteByte((byte)'\n');
    }

    private static int GetLengthWithoutTrailingWhitespace(byte[] line)
    {
        int end = line.Length - 1;
        while (end >= 0 && IsWhiteSpace(line[end]))
        {
            end--;
        }

        return end + 1;
    }

    private static bool IsWhiteSpace(byte b) => b is (byte)'\r' or (byte)'\n' or (byte)'\t' or (byte)' ';

    private static int ReadInputLine(MemoryStream buffer, Stream input)
    {
        buffer.SetLength(0);

        int lookAhead = -1;
        int ch;
        while ((ch = input.ReadByte()) >= 0)
        {
            buffer.WriteByte((byte)ch);
            if (ch is '\r' or '\n')
            {
                lookAhead = ReadPastEndOfLine(buffer, ch, input);
                break;
            }
        }

        return lookAhead;
    }

    private static int ReadInputLine(MemoryStream buffer, int lookAheadIn, Stream input)
    {
        buffer.SetLength(0);

        int ch = lookAheadIn;
        int lookAhead = -1;
        do
        {
            buffer.WriteByte((byte)ch);
            if (ch is '\r' or '\n')
            {
                lookAhead = ReadPastEndOfLine(buffer, ch, input);
                break;
            }
        }
        while ((ch = input.ReadByte()) >= 0);

        return ch < 0 ? -1 : lookAhead;
    }

    private static int ReadPastEndOfLine(MemoryStream buffer, int lastChar, Stream input)
    {
        int lookAhead = input.ReadByte();
        if (lastChar == '\r' && lookAhead == '\n')
        {
            buffer.WriteByte((byte)lookAhead);
            lookAhead = input.ReadByte();
        }

        return lookAhead;
    }
}
