using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Proton.Pass.Models;
using Windows.ApplicationModel.DataTransfer;

namespace Proton.Pass.ViewModels;

public partial class ItemDetailViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MaskedPassword))]
    private PassItem? item;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MaskedPassword))]
    private bool isPasswordRevealed;

    public string MaskedPassword => Item is null
        ? string.Empty
        : IsPasswordRevealed ? Item.Password : new string('•', Item.Password.Length);

    public void Load(string itemId)
    {
        Item = SampleVaultStore.Items.FirstOrDefault(i => i.Id == itemId);
        IsPasswordRevealed = false;
    }

    [RelayCommand]
    private void TogglePasswordVisibility()
    {
        IsPasswordRevealed = !IsPasswordRevealed;
    }

    [RelayCommand]
    private void CopyPassword()
    {
        CopyToClipboard(Item?.Password);
    }

    [RelayCommand]
    private void CopyUsername()
    {
        CopyToClipboard(Item?.Username);
    }

    private static void CopyToClipboard(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        var package = new DataPackage();
        package.SetText(text);
        Clipboard.SetContent(package);
    }
}
