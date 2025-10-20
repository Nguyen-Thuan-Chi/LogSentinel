using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Log_Sentinel.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Log_Sentinel.UI
{
    /// <summary>
    /// Interaction logic for RuleView.xaml
    /// </summary>
    public partial class RuleView : System.Windows.Controls.UserControl
    {
        public RuleView()
        {
            InitializeComponent();
            
            // Create a new scope and get the ViewModel
            if (Application.Current is App app)
            {
                var scope = app.ServiceProvider.CreateScope();
                DataContext = scope.ServiceProvider.GetRequiredService<RuleViewModel>();
            }
        }
    }
}
