using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Log_Sentinel.Helpers;
using Log_Sentinel.UI;
using LogSentinel.BUS.Interfaces;
using LogSentinel.DAL.Data;
using LogSentinel.DAL.Repositories;

namespace Log_Sentinel.ViewModels
{
    public class RuleViewModel : INotifyPropertyChanged
    {
        private readonly IRuleRepository _ruleRepository;
        private readonly IRuleEngine _ruleEngine;
        private string _searchText = "";
        private RuleItem? _selectedRule;

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                FilterRules();
            }
        }

        public RuleItem? SelectedRule
        {
            get => _selectedRule;
            set
            {
                _selectedRule = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<RuleItem> Rules { get; } = new();
        public ObservableCollection<RuleItem> FilteredRules { get; } = new();

        public ICommand RefreshCommand { get; }
        public ICommand AddRuleCommand { get; }
        public ICommand EditRuleCommand { get; }
        public ICommand DeleteRuleCommand { get; }
        public ICommand ToggleRuleCommand { get; }

        public RuleViewModel(IRuleRepository ruleRepository, IRuleEngine ruleEngine)
        {
            _ruleRepository = ruleRepository;
            _ruleEngine = ruleEngine;

            RefreshCommand = new RelayCommand(async _ => await LoadRulesAsync());
            AddRuleCommand = new RelayCommand(async _ => await AddRuleAsync());
            EditRuleCommand = new RelayCommand(async param => await EditRuleAsync(param as RuleItem), param => param is RuleItem);
            DeleteRuleCommand = new RelayCommand(async param => await DeleteRuleAsync(param as RuleItem), param => param is RuleItem);
            ToggleRuleCommand = new RelayCommand(async param => await ToggleRuleAsync(param as RuleItem), param => param is RuleItem);

            // Initial load - use Dispatcher to avoid blocking constructor
            _ = System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => await LoadRulesAsync());
        }

        private async Task LoadRulesAsync()
        {
            try
            {
                var rules = await _ruleRepository.GetAllAsync();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Rules.Clear();
                    foreach (var rule in rules)
                    {
                        Rules.Add(new RuleItem
                        {
                            Id = rule.Id,
                            Name = rule.Name,
                            Description = rule.Description,
                            Severity = rule.Severity,
                            IsEnabled = rule.IsEnabled,
                            TriggerCount = rule.TriggerCount,
                            LastTriggered = rule.LastTriggeredAt?.ToString("yyyy-MM-dd HH:mm") ?? "Never",
                            YamlContent = rule.YamlContent
                        });
                    }

                    FilterRules();
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading rules: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FilterRules()
        {
            FilteredRules.Clear();

            var filtered = string.IsNullOrWhiteSpace(SearchText)
                ? Rules
                : Rules.Where(r => r.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                                  r.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

            foreach (var rule in filtered)
            {
                FilteredRules.Add(rule);
            }
        }

        private async Task AddRuleAsync()
        {
            var dialog = new RuleEditorDialog();
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var rule = new RuleEntity
                    {
                        Name = dialog.RuleName,
                        Description = dialog.RuleDescription,
                        Severity = dialog.RuleSeverity,
                        YamlContent = dialog.YamlContent,
                        IsEnabled = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _ruleRepository.AddAsync(rule);
                    await _ruleEngine.ReloadRulesAsync();
                    await LoadRulesAsync();

                    MessageBox.Show("Rule added successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error adding rule: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task EditRuleAsync(RuleItem? ruleItem)
        {
            if (ruleItem == null) return;

            var rule = await _ruleRepository.GetByIdAsync(ruleItem.Id);
            if (rule == null) return;

            var dialog = new RuleEditorDialog
            {
                RuleName = rule.Name,
                RuleDescription = rule.Description,
                RuleSeverity = rule.Severity,
                YamlContent = rule.YamlContent
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    rule.Name = dialog.RuleName;
                    rule.Description = dialog.RuleDescription;
                    rule.Severity = dialog.RuleSeverity;
                    rule.YamlContent = dialog.YamlContent;
                    rule.UpdatedAt = DateTime.UtcNow;

                    await _ruleRepository.UpdateAsync(rule);
                    await _ruleEngine.ReloadRulesAsync();
                    await LoadRulesAsync();

                    MessageBox.Show("Rule updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error updating rule: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task DeleteRuleAsync(RuleItem? ruleItem)
        {
            if (ruleItem == null) return;

            var result = MessageBox.Show(
                $"Are you sure you want to delete the rule '{ruleItem.Name}'?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var rule = await _ruleRepository.GetByIdAsync(ruleItem.Id);
                    if (rule != null)
                    {
                        await _ruleRepository.DeleteAsync(rule);
                        await _ruleEngine.ReloadRulesAsync();
                        await LoadRulesAsync();

                        MessageBox.Show("Rule deleted successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting rule: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task ToggleRuleAsync(RuleItem? ruleItem)
        {
            if (ruleItem == null) return;

            try
            {
                var rule = await _ruleRepository.GetByIdAsync(ruleItem.Id);
                if (rule != null)
                {
                    rule.IsEnabled = !rule.IsEnabled;
                    rule.UpdatedAt = DateTime.UtcNow;

                    await _ruleRepository.UpdateAsync(rule);
                    await _ruleEngine.ReloadRulesAsync();

                    ruleItem.IsEnabled = rule.IsEnabled;
                    OnPropertyChanged(nameof(FilteredRules));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error toggling rule: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RuleItem : INotifyPropertyChanged
    {
        private bool _isEnabled;

        public long Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Severity { get; set; } = "";

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                _isEnabled = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusText));
            }
        }

        public int TriggerCount { get; set; }
        public string LastTriggered { get; set; } = "";
        public string YamlContent { get; set; } = "";

        public string StatusText => IsEnabled ? "Active" : "Disabled";

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
