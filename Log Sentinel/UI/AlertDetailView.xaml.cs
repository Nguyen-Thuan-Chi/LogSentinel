using System.Windows;
using LogSentinel.DAL.Data;
using Log_Sentinel.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Log_Sentinel.UI
{
    public partial class AlertDetailView : Window
    {
        public AlertDetailView(AlertEntity alertEntity, IServiceProvider? serviceProvider = null)
        {
            InitializeComponent();
            
            // Set the DataContext to our ViewModel
            DataContext = new AlertDetailViewModel(alertEntity, serviceProvider);
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Get the ViewModel from DataContext
                if (DataContext is AlertDetailViewModel viewModel)
                {
                    // Initialize the ViewModel asynchronously
                    await viewModel.InitializeAsync();
                }
            }
            catch (Exception ex)
            {
                // Log any exceptions that occur during initialization
                System.Diagnostics.Debug.WriteLine($"Error in Window_Loaded: {ex}");
                
                // Optionally show a message to the user
                MessageBox.Show($"Error loading alert details: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}