using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Proton.Mail.ViewModels;

namespace Proton.Mail.Views;

public sealed partial class ConversationPage : Page
{
    public ConversationViewModel ViewModel { get; } = new();

    public ConversationPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is string conversationId)
        {
            ViewModel.Load(conversationId);
        }
    }
}
