using System.Windows.Controls;
using Log_Sentinel.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Log_Sentinel.UI
{
    /// <summary>
    /// Interaction logic for SettingsView.xaml
    /// </summary>
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
            
            // Set DataContext from DI container
            if (System.Windows.Application.Current is App app)
            {
                DataContext = app.ServiceProvider.GetRequiredService<SettingsViewModel>();
            }
        }
    }
}
