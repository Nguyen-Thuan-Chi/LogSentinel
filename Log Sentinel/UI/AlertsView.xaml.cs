using System.Windows.Controls;
using Log_Sentinel.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace Log_Sentinel.UI
{
    public partial class AlertsView : UserControl
    {
        public AlertsView()
        {
            InitializeComponent();
            
            // Create a new scope and get the ViewModel
            if (Application.Current is App app)
            {
                var scope = app.ServiceProvider.CreateScope();
                DataContext = scope.ServiceProvider.GetRequiredService<AlertsViewModel>();
            }
        }
    }
}
