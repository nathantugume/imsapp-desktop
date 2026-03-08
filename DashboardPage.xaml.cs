using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using imsapp_desktop.ViewModels;

namespace imsapp_desktop;

public sealed partial class DashboardPage : Page
{
    public DashboardPage()
    {
        InitializeComponent();
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is DashboardViewModel vm)
            vm.LoadCommand.Execute(null);
    }
}
