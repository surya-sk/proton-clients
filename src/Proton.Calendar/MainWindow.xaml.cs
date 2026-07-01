using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Proton.Calendar.Views;

namespace Proton.Calendar;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Title = "Proton Calendar";
        ContentFrame.Navigate(typeof(MonthPage));
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
            "Month" => typeof(MonthPage),
            "Week" => typeof(WeekPage),
            "Day" => typeof(DayPage),
            _ => typeof(MonthPage),
        };

        if (ContentFrame.CurrentSourcePageType != pageType)
        {
            ContentFrame.Navigate(pageType);
        }
    }

    private void NewEventButton_Click(object sender, RoutedEventArgs e)
    {
        ContentFrame.Navigate(typeof(EventEditorPage));
    }
}
