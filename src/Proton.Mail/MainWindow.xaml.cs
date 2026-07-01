using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Proton.Mail.Views;

namespace Proton.Mail;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Title = "Proton Mail";
        ContentFrame.Navigate(typeof(InboxPage));
        RootNavigationView.SelectedItem = RootNavigationView.MenuItems[0];
    }

    private void RootNavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is not NavigationViewItem { Tag: string tag })
        {
            return;
        }

        Type pageType = tag switch
        {
            "Inbox" => typeof(InboxPage),
            "Folders" => typeof(FoldersPage),
            _ => typeof(InboxPage),
        };

        if (ContentFrame.CurrentSourcePageType != pageType)
        {
            ContentFrame.Navigate(pageType);
        }
    }

    private void ComposeButton_Click(object sender, RoutedEventArgs e)
    {
        ContentFrame.Navigate(typeof(ComposePage));
    }
}
