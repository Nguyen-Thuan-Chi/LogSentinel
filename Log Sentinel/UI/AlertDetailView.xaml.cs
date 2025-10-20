using System.Windows;
using LogSentinel.DAL.Data;
using Log_Sentinel.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Log_Sentinel.UI
{
    /// <summary>
    /// Interaction logic for AlertDetailView.xaml
    /// A window that displays detailed information about a selected alert and its triggering event.
    /// </summary>
    public partial class AlertDetailView : Window
    {
        public AlertDetailView(AlertEntity alertEntity, IServiceProvider? serviceProvider = null)
        {
            InitializeComponent();
            
            // Set the DataContext to our ViewModel
            DataContext = new AlertDetailViewModel(alertEntity, serviceProvider);
        }

        /// <summary>
        /// Handles the Window_Loaded event. Initializes the ViewModel asynchronously.
        /// </summary>
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
                else
                {
                    // This should never happen, but let's handle it gracefully
                    MessageBox.Show("Failed to initialize alert details view.", "Error", 
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                // Log any exceptions that occur during initialization
                System.Diagnostics.Debug.WriteLine($"Error in Window_Loaded: {ex}");
                
                // Show a user-friendly message
                MessageBox.Show($"Error loading alert details: {ex.Message}", "Error", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
                
                // Optionally close the window if initialization fails critically
                // this.Close();
            }
        }

        /// <summary>
        /// Handles the Close button click event.
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}