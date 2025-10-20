using System.Windows.Controls;
using Log_Sentinel.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace Log_Sentinel.UI
{
    /// <summary>
    /// Interaction logic for EventsView.xaml
    /// </summary>
    public partial class EventsView : System.Windows.Controls.UserControl
    {
        public EventsView()
        {
            InitializeComponent();
            
            // Create a new scope and get the ViewModel
            if (Application.Current is App app)
            {
                var scope = app.ServiceProvider.CreateScope();
                var viewModel = scope.ServiceProvider.GetRequiredService<EventsViewModel>();
                DataContext = viewModel;
                EventsDataGrid.ItemsSource = viewModel.FilteredSystemLogs;
            }
            else
            {
                // Fallback for design-time
                var viewModel = new EventsViewModel();
                DataContext = viewModel;
                EventsDataGrid.ItemsSource = viewModel.FilteredSystemLogs;
            }
        }
    }
}
