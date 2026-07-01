using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Proton.Pass.Views;

namespace Proton.Pass;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Title = "Proton Pass";
        ContentFrame.Navigate(typeof(VaultListPage));
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
            "Vaults" => typeof(VaultListPage),
            "Generator" => typeof(PasswordGeneratorPage),
            _ => typeof(VaultListPage),
        };

        if (ContentFrame.CurrentSourcePageType != pageType)
        {
            ContentFrame.Navigate(pageType);
        }
    }
}
