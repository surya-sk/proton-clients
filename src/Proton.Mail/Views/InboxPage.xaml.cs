using Microsoft.UI.Xaml.Controls;
using Proton.Mail.Models;
using Proton.Mail.ViewModels;

namespace Proton.Mail.Views;

public sealed partial class InboxPage : Page
{
    public InboxViewModel ViewModel { get; } = new();

    public InboxPage()
    {
        InitializeComponent();
    }

    private void ConversationListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ConversationListView.SelectedItem is ConversationSummary conversation)
        {
            Frame.Navigate(typeof(ConversationPage), conversation.Id);
        }
    }
}
