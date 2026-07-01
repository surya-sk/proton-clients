using System.Text;
using Proton.Core.Auth.Srp;
using Xunit;

namespace Proton.Core.Tests.Srp;

/// <summary>
/// Vectors copied verbatim from go-srp's hash_test.go (Test_bcryptHash): each pins a 22-char
/// Proton-base64 salt to bcrypt's own reference output for password "test!!!". Verified through
/// <see cref="PasswordHasher.HashMailboxPassword"/>, which (unlike HashPassword) returns the raw
/// bcrypt string with no expandHash wrapping - the same shape go-srp's internal bcryptHash returns.
/// </summary>
public class PasswordHasherTests
{
    [Theory]
    [InlineData("PTTsDBs/mlLnSk6VmtFghe", "$2y$10$PTTsDBs/mlLnSk6VmtFgheNSiK/lSwtJsrBLLDK3kZYI7193nInqy")]
    [InlineData("4DZHd6WZX4fEaWKtCfYdde", "$2y$10$4DZHd6WZX4fEaWKtCfYddeZfcryISo9eEMgbA90O.Wnnz1s1VKmKC")]
    [InlineData("RpyeXO7K2eD3r/ZZ/B63V.", "$2y$10$RpyeXO7K2eD3r/ZZ/B63V.Tya53OExbyO8LR7TB93KYP4PvC.EPMW")]
    [InlineData("xVEeHQI8CyNkblUJDhyx3u", "$2y$10$xVEeHQI8CyNkblUJDhyx3uZjo8GDXoNNVoRpLwLvssO1GvV3eYFJS")]
    [InlineData("d4Q1rrFYjGq2jyVUi7YwTu", "$2y$10$d4Q1rrFYjGq2jyVUi7YwTuikgSeAgJfaAYJSJZIbIOvW1GBFwx2J6")]
    [InlineData("/.3KXCwRnsrxURMGxN7.R.", "$2y$10$/.3KXCwRnsrxURMGxN7.R.GLpVq0zyBbI9wgS0wB2U/g2btx1RYoy")]
    [InlineData("tuE3bNGezetI9Ra2aGePqu", "$2y$10$tuE3bNGezetI9Ra2aGePqutWPxG2r36BOzMGoXYzM0p2vmGT9fK1i")]
    [InlineData("GFfbuV2J/9BsY0Mb8sJOCe", "$2y$10$GFfbuV2J/9BsY0Mb8sJOCejr2HSgVY2R93m7qQYqSID5ONeYg7ngG")]
    [InlineData("FYvnvw/ghdYJbOADddZ3Ae", "$2y$10$FYvnvw/ghdYJbOADddZ3Ae.XoxSKZqOf5t0S/epYUaNn7YmdxmxD6")]
    [InlineData("jjMNLFvjPepiyCfuKxYUcO", "$2y$10$jjMNLFvjPepiyCfuKxYUcOykUITQRwkNY1oY5ZgxCDIgj6lXypXx2")]
    public void HashMailboxPassword_MatchesGoSrpBcryptVectors(string encodedSalt, string expectedBcryptString)
    {
        byte[] salt = ProtonBase64.Decode(encodedSalt, 16);

        byte[] actual = PasswordHasher.HashMailboxPassword("test!!!", salt);

        Assert.Equal(expectedBcryptString, Encoding.ASCII.GetString(actual));
    }

    [Fact]
    public void HashPassword_Version4_IsDeterministicForSameInputs()
    {
        byte[] salt = Convert.FromBase64String("yKlc5/CvObfoiw==");
        byte[] modulus = new byte[256];
        Random.Shared.NextBytes(modulus);

        byte[] first = PasswordHasher.HashPassword(4, "abc123", "jakubqa", salt, modulus);
        byte[] second = PasswordHasher.HashPassword(4, "abc123", "jakubqa", salt, modulus);

        Assert.Equal(first, second);
        Assert.Equal(256, first.Length); // expandHash always yields 4 * SHA-512 = 256 bytes.
    }

    [Fact]
    public void HashPassword_DifferentPasswords_ProduceDifferentHashes()
    {
        byte[] salt = Convert.FromBase64String("yKlc5/CvObfoiw==");
        byte[] modulus = new byte[256];
        Random.Shared.NextBytes(modulus);

        byte[] a = PasswordHasher.HashPassword(4, "correct horse", "user", salt, modulus);
        byte[] b = PasswordHasher.HashPassword(4, "wrong horse", "user", salt, modulus);

        Assert.NotEqual(a, b);
    }
}
