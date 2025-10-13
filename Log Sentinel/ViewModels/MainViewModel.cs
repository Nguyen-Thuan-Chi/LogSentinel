using System.Windows.Input;

namespace Log_Sentinel.ViewModels
{
    public class MainViewModel
    {
        public ICommand NavigateDashboard { get; }
        public ICommand NavigateRule { get; }
        public ICommand NavigateEvents { get; }
        public ICommand NavigateSettings { get; }
        public ICommand NavigateHelp { get; }
        public ICommand NavigateAbout { get; }

        public MainViewModel()
        {
            NavigateDashboard = new RelayCommand(_ => { /* Navigation logic */ });
            NavigateRule = new RelayCommand(_ => { /* Navigation logic */ });
            NavigateEvents = new RelayCommand(_ => { /* Navigation logic */ });
            NavigateSettings = new RelayCommand(_ => { /* Navigation logic */ });
            NavigateHelp = new RelayCommand(_ => { /* Navigation logic */ });
            NavigateAbout = new RelayCommand(_ => { /* Navigation logic */ });
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        public RelayCommand(Action<object?> execute) => _execute = execute;
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => _execute(parameter);
    }
}