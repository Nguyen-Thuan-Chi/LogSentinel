using System.Windows.Controls;
using Log_Sentinel.ViewModels;

namespace Log_Sentinel.UI
{
    /// <summary>
    /// Interaction logic for EventsView.xaml
    /// </summary>
    public partial class EventsView : UserControl
    {
        private readonly EventsViewModel _viewModel;

        public EventsView()
        {
            InitializeComponent();
            
            _viewModel = new EventsViewModel();
            EventsDataGrid.ItemsSource = _viewModel.SystemLogs;
        }
    }
}
