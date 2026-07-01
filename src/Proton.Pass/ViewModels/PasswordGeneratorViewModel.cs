using System.Security.Cryptography;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Windows.ApplicationModel.DataTransfer;

namespace Proton.Pass.ViewModels;

public partial class PasswordGeneratorViewModel : ObservableObject
{
    // Ambiguous-looking characters (I, l, 1, O, 0) are excluded so generated passwords are easy
    // to transcribe by hand when needed.
    private const string UpperChars = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string LowerChars = "abcdefghijkmnopqrstuvwxyz";
    private const string DigitChars = "23456789";
    private const string SymbolChars = "!@#$%^&*-_=+?";

    [ObservableProperty]
    private int length = 20;

    [ObservableProperty]
    private bool includeUppercase = true;

    [ObservableProperty]
    private bool includeLowercase = true;

    [ObservableProperty]
    private bool includeDigits = true;

    [ObservableProperty]
    private bool includeSymbols = true;

    [ObservableProperty]
    private string generatedPassword = string.Empty;

    [ObservableProperty]
    private bool isCopied;

    public PasswordGeneratorViewModel()
    {
        Regenerate();
    }

    partial void OnLengthChanged(int value) => Regenerate();
    partial void OnIncludeUppercaseChanged(bool value) => Regenerate();
    partial void OnIncludeLowercaseChanged(bool value) => Regenerate();
    partial void OnIncludeDigitsChanged(bool value) => Regenerate();
    partial void OnIncludeSymbolsChanged(bool value) => Regenerate();

    [RelayCommand]
    private void Regenerate()
    {
        IsCopied = false;

        string pool = string.Concat(
            IncludeUppercase ? UpperChars : string.Empty,
            IncludeLowercase ? LowerChars : string.Empty,
            IncludeDigits ? DigitChars : string.Empty,
            IncludeSymbols ? SymbolChars : string.Empty);

        GeneratedPassword = pool.Length == 0 ? string.Empty : Generate(Length, pool);
    }

    [RelayCommand]
    private void CopyToClipboard()
    {
        if (string.IsNullOrEmpty(GeneratedPassword))
        {
            return;
        }

        var package = new DataPackage();
        package.SetText(GeneratedPassword);
        Clipboard.SetContent(package);
        IsCopied = true;
    }

    /// <summary>
    /// Builds a password from cryptographically secure random bytes, using rejection sampling
    /// per character so every character in <paramref name="pool"/> has exactly equal probability
    /// (a plain "randomByte % pool.Length" would bias towards earlier characters).
    /// </summary>
    private static string Generate(int length, string pool)
    {
        var result = new StringBuilder(length);
        byte rejectionThreshold = (byte)(256 - (256 % pool.Length));
        Span<byte> singleByte = stackalloc byte[1];

        for (int i = 0; i < length; i++)
        {
            byte value;
            do
            {
                RandomNumberGenerator.Fill(singleByte);
                value = singleByte[0];
            }
            while (value >= rejectionThreshold);

            result.Append(pool[value % pool.Length]);
        }

        return result.ToString();
    }
}
