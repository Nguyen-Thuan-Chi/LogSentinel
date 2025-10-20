using System.Windows.Controls;
using Log_Sentinel.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace Log_Sentinel.UI
{
    public partial class AlertsView : System.Windows.Controls.UserControl
    {
        public AlertsView()
        {
            InitializeComponent();
            
            // Get the ViewModel from the application's service provider
            if (Application.Current is App app)
            {
                DataContext = app.ServiceProvider.GetRequiredService<AlertsViewModel>();
            }
        }
    }
}
