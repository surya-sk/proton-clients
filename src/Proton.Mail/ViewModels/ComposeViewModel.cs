using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Proton.Mail.ViewModels;

public partial class ComposeViewModel : ObservableObject
{
    [ObservableProperty]
    private string to = string.Empty;

    [ObservableProperty]
    private string subject = string.Empty;

    [ObservableProperty]
    private string body = string.Empty;

    [ObservableProperty]
    private bool isSent;

    /// <summary>
    /// Placeholder send action. Actual sending requires PGP-encrypting the message body to each
    /// recipient's public key and calling the Mail send API - both land with the Mail API client
    /// in Proton.Core.
    /// </summary>
    [RelayCommand]
    private void Send()
    {
        IsSent = true;
    }

    [RelayCommand]
    private void Discard()
    {
        To = string.Empty;
        Subject = string.Empty;
        Body = string.Empty;
    }
}
