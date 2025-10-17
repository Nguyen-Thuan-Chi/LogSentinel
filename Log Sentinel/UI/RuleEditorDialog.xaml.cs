using System.Windows;

namespace Log_Sentinel.UI
{
    public partial class RuleEditorDialog : Window
    {
        public string RuleName { get; set; } = "";
        public string RuleDescription { get; set; } = "";
        public string RuleSeverity { get; set; } = "Medium";
        public string YamlContent { get; set; } = "";

        public RuleEditorDialog()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(RuleName))
            {
                MessageBox.Show("Rule name is required!", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(YamlContent))
            {
                MessageBox.Show("YAML content is required!", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
