using Org.BouncyCastle.Math;
using Proton.Core.Auth.Srp;
using Xunit;

namespace Proton.Core.Tests.Srp;

public class SrpMathTests
{
    [Theory]
    [InlineData(0L)]
    [InlineData(1L)]
    [InlineData(255L)]
    [InlineData(65536L)]
    [InlineData(long.MaxValue)]
    public void LittleEndianRoundTrip_PreservesValue(long value)
    {
        BigInteger original = BigInteger.ValueOf(value);

        byte[] wireBytes = SrpMath.ToLittleEndian(original, 32);
        BigInteger roundTripped = SrpMath.FromLittleEndian(wireBytes);

        Assert.Equal(original, roundTripped);
    }

    [Fact]
    public void ToLittleEndian_StoresLeastSignificantByteFirst()
    {
        // 0x0102 -> LE bytes [0x02, 0x01, 0x00, ...]
        BigInteger value = BigInteger.ValueOf(0x0102);

        byte[] wireBytes = SrpMath.ToLittleEndian(value, 4);

        Assert.Equal(new byte[] { 0x02, 0x01, 0x00, 0x00 }, wireBytes);
    }

    [Fact]
    public void ExpandHash_Returns256Bytes()
    {
        byte[] result = SrpMath.ExpandHash(new byte[] { 1, 2, 3 });

        Assert.Equal(256, result.Length);
    }

    [Fact]
    public void ExpandHash_IsDeterministic()
    {
        byte[] first = SrpMath.ExpandHash(new byte[] { 1, 2, 3 }, new byte[] { 4, 5 });
        byte[] second = SrpMath.ExpandHash(new byte[] { 1, 2, 3 }, new byte[] { 4, 5 });

        Assert.Equal(first, second);
    }

    [Fact]
    public void ExpandHash_DifferentInputs_ProduceDifferentOutput()
    {
        byte[] a = SrpMath.ExpandHash(new byte[] { 1 });
        byte[] b = SrpMath.ExpandHash(new byte[] { 2 });

        Assert.NotEqual(a, b);
    }
}
