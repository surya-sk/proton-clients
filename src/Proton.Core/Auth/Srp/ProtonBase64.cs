using System.Text;

namespace Proton.Core.Auth.Srp;

/// <summary>
/// Base64 codec using bcrypt's "./A-Za-z0-9" alphabet with standard (non-bcrypt-reordered)
/// bit packing, matching Go's <c>base64.NewEncoding(alphabet).WithPadding(NoPadding)</c> as
/// used by go-srp for the "based64DotSlash" salt encoding.
/// </summary>
internal static class ProtonBase64
{
    private const string Alphabet = "./ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    private static readonly sbyte[] DecodeTable = BuildDecodeTable();

    private static sbyte[] BuildDecodeTable()
    {
        sbyte[] table = new sbyte[128];
        Array.Fill<sbyte>(table, -1);
        for (int i = 0; i < Alphabet.Length; i++)
        {
            table[Alphabet[i]] = (sbyte)i;
        }

        return table;
    }

    public static string Encode(byte[] data)
    {
        var sb = new StringBuilder((data.Length * 4 + 2) / 3);
        int i = 0;
        for (; i + 3 <= data.Length; i += 3)
        {
            EncodeGroup(sb, data[i], data[i + 1], data[i + 2]);
        }

        int remaining = data.Length - i;
        if (remaining == 1)
        {
            EncodeGroup(sb, data[i], 0, 0, 2);
        }
        else if (remaining == 2)
        {
            EncodeGroup(sb, data[i], data[i + 1], 0, 3);
        }

        return sb.ToString();
    }

    private static void EncodeGroup(StringBuilder sb, byte a1, byte a2, byte a3, int chars = 4)
    {
        sb.Append(Alphabet[a1 >> 2]);
        sb.Append(Alphabet[((a1 << 4) | (a2 >> 4)) & 0x3f]);
        if (chars > 2)
        {
            sb.Append(Alphabet[((a2 << 2) | (a3 >> 6)) & 0x3f]);
        }

        if (chars > 3)
        {
            sb.Append(Alphabet[a3 & 0x3f]);
        }
    }

    /// <summary>
    /// Decodes the leading <paramref name="byteCount"/> bytes represented by
    /// <paramref name="chars"/>, right-padding with the zero-valued alphabet
    /// character ('.') as needed to complete the final 4-character group. This
    /// mirrors how legacy (v1/v2) Proton salts - which are longer hex strings
    /// truncated to 22 base64 characters by the reference bcrypt binding - decode.
    /// </summary>
    public static byte[] Decode(string chars, int byteCount)
    {
        int charsNeeded = ((byteCount * 8) + 5) / 6;
        if (chars.Length < charsNeeded)
        {
            throw new ArgumentException("Not enough characters to decode the requested byte count.", nameof(chars));
        }

        int paddedLength = ((charsNeeded + 3) / 4) * 4;
        Span<int> values = stackalloc int[paddedLength];
        for (int i = 0; i < paddedLength; i++)
        {
            char c = i < charsNeeded ? chars[i] : '.';
            values[i] = c < 128 ? DecodeTable[c] : (sbyte)-1;
            if (values[i] < 0)
            {
                throw new ArgumentException($"Invalid Proton-base64 character '{c}'.", nameof(chars));
            }
        }

        byte[] output = new byte[(paddedLength / 4) * 3];
        int outIndex = 0;
        for (int i = 0; i < paddedLength; i += 4)
        {
            int b1 = values[i], b2 = values[i + 1], b3 = values[i + 2], b4 = values[i + 3];
            output[outIndex++] = (byte)((b1 << 2) | (b2 >> 4));
            output[outIndex++] = (byte)((b2 << 4) | (b3 >> 2));
            output[outIndex++] = (byte)((b3 << 6) | b4);
        }

        return output[..byteCount];
    }
}
