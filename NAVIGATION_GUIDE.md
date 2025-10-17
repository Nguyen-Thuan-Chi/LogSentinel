# Log Sentinel - Navigation Guide

## T?ng quan

Log Sentinel hi?n ?? c? h? th?ng navigation ho?n ch?nh v?i c?c view sau:

### Main Navigation (Sidebar tr?n)
1. **Dashboard** - T?ng quan h? th?ng v?i th?ng k? v? bi?u ??
2. **Log Viewer** - Xem v? t?m ki?m c?c event logs
3. **Rules** - Qu?n l? c?c detection rules (th?m, s?a, x?a, b?t/t?t)
4. **Alerts** - Qu?n l? c?nh b?o (acknowledge, xem chi ti?t, export)
5. **Analytics** - Ph?n t?ch v? bi?u ?? (placeholder)
6. **Import / Export** - Import/Export d? li?u (placeholder)

### Secondary Navigation (Sidebar d??i)
1. **Settings** - C?u h?nh ?ng d?ng (database, theme, notifications)
2. **About** - Th?ng tin v? ?ng d?ng
3. **Help** - H??ng d?n s? d?ng v? t?i li?u

## Ki?n tr?c Navigation

### 1. MainViewModel
- Qu?n l? `CurrentView` property ?? hi?n th? UserControl hi?n t?i
- C?c command ?? navigate: `NavigateDashboardCommand`, `NavigateRulesCommand`, etc.
- Method `NavigateTo(string viewName)` ?? chuy?n view

### 2. MainWindow
- S? d?ng `ContentControl` ?? hi?n th? view ??ng
- Event handlers cho `MainNavListBox_SelectionChanged` v? `SecondaryNavListBox_SelectionChanged`
- T? ??ng clear selection c?a listbox c?n l?i khi ch?n menu

### 3. UserControl Views
M?i view l? m?t UserControl ri?ng bi?t:
- **DashboardView.xaml** - Hi?n th? th?ng k?, recent events, active rules
- **EventsView.xaml** - DataGrid v?i search v? filter
- **RuleView.xaml** - Qu?n l? rules v?i CRUD operations
- **AlertsView.xaml** - Qu?n l? alerts v?i acknowledge, export
- **SettingsView.xaml** - C?u h?nh ?ng d?ng
- **HelpView.xaml** - Documentation v? shortcuts
- **AboutView.xaml** - Th?ng tin ?ng d?ng

## Dependency Injection

T?t c? ViewModels v? Services ???c ??ng k? trong `App.xaml.cs`:

```csharp
// ViewModels
services.AddSingleton<MainViewModel>();
services.AddSingleton<DashboardViewModel>();
services.AddSingleton<RuleViewModel>();
services.AddSingleton<AlertsViewModel>();
services.AddSingleton<EventsViewModel>();
services.AddSingleton<SettingsViewModel>();

// Services
services.AddScoped<IRuleEngine, RuleEngine>();
services.AddScoped<IAlertService, AlertService>();
services.AddScoped<IEventImporter, EventImporter>();
```

## C?ch th?m View m?i

1. T?o file XAML v? code-behind trong folder `UI/`:
```xaml
<UserControl x:Class="Log_Sentinel.UI.MyNewView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <!-- Your UI here -->
</UserControl>
```

2. T?o ViewModel t??ng ?ng (n?u c?n):
```csharp
public class MyNewViewModel : INotifyPropertyChanged
{
    // Properties and commands
}
```

3. ??ng k? trong `App.xaml.cs`:
```csharp
services.AddSingleton<MyNewViewModel>();
```

4. Th?m case trong `MainViewModel.NavigateTo()`:
```csharp
public void NavigateTo(string viewName)
{
    CurrentView = viewName switch
    {
        "MyNew" => new MyNewView(),
        // ... other cases
        _ => CurrentView
    };
}
```

5. Th?m menu item trong `MainWindow.xaml` ListBox

## Event Flow

### Alert Notification Flow
1. `RuleEngine` evaluate events v? trigger alerts
2. `AlertService.CreateAlertAsync()` t?o alert v? raise `AlertCreated` event
3. `MainWindow` subscribe v?o event v? hi?n th? MessageBox
4. User click OK Å® Navigate to Alerts view
5. `AlertsViewModel` subscribe v?o `AlertCreated` event ?? update UI real-time

### Navigation Flow
1. User click menu item trong ListBox
2. `SelectionChanged` event ???c trigger
3. Event handler g?i `MainViewModel.NavigateTo()`
4. `NavigateTo()` t?o instance m?i c?a UserControl
5. `CurrentView` property ???c update
6. WPF binding system t? ??ng update `ContentControl.Content`

## Real-time Updates

C?c ViewModel subscribe v?o service events ?? update UI real-time:

- **DashboardViewModel**: Subscribe `AlertService.AlertCreated` ?? update alert count
- **AlertsViewModel**: Subscribe `AlertService.AlertCreated` ?? th?m alert m?i v?o list
- **RuleViewModel**: Kh?ng c?n real-time (load on demand)
- **EventsViewModel**: Load events t? database theo y?u c?u

## Commands v? Actions

### RuleViewModel
- `RefreshCommand` - Reload rules t? database
- `AddRuleCommand` - M? dialog ?? th?m rule m?i
- `EditRuleCommand` - S?a rule ?ang ch?n
- `DeleteRuleCommand` - X?a rule (v?i confirmation)
- `ToggleRuleCommand` - Enable/disable rule

### AlertsViewModel
- `RefreshCommand` - Reload alerts
- `AcknowledgeAlertCommand` - Acknowledge m?t alert
- `AcknowledgeAllCommand` - Acknowledge t?t c? alerts
- `ViewDetailsCommand` - Xem chi ti?t alert
- `DeleteAlertCommand` - X?a alert
- `ExportCsvCommand` - Export alerts to CSV

### DashboardViewModel
- `RefreshCommand` - Reload all dashboard data

### SettingsViewModel
- `SaveSettingsCommand` - L?u c?u h?nh
- `BrowseDatabaseCommand` - Browse database file

## Styling v? Theme

?ng d?ng s? d?ng:
- Material Design Icons (MaterialDesignThemes)
- Custom color scheme: Blue (#2563EB), Green (#10B981), Red (#EF4444)
- Consistent padding, margins, v? border radius
- Responsive layout v?i Grid v? StackPanel

## Performance Optimization

- DataGrid s? d?ng virtualization: `VirtualizingPanel.IsVirtualizing="True"`
- Lazy loading cho c?c UserControl (ch? t?o khi navigate)
- Scoped services cho database operations
- Async/await cho t?t c? I/O operations

## Troubleshooting

### View kh?ng hi?n th?
- Check DataContext c? ???c set trong constructor c?a UserControl
- Check `NavigateTo()` switch case c? case t??ng ?ng
- Check ListBox SelectionChanged event c? g?i ??ng command

### Commands kh?ng work
- Check ICommand property c? ???c bind trong XAML
- Check CanExecute condition (n?u c?)
- Check RelayCommand implementation trong ViewModel

### Real-time update kh?ng work
- Check event subscription trong ViewModel constructor
- Check Dispatcher.Invoke() cho UI thread updates
- Check event c? ???c raise trong service

## Best Practices

1. **Separation of Concerns**: M?i View c? ViewModel ri?ng
2. **MVVM Pattern**: View kh?ng access tr?c ti?p v?o Model ho?c Service
3. **Dependency Injection**: S? d?ng constructor injection
4. **Async Programming**: T?t c? database operations l? async
5. **Error Handling**: Try-catch v?i logging v? user-friendly messages
6. **Resource Management**: Dispose IDisposable objects (DbContext, HttpClient)

## Future Enhancements

- [ ] Th?m Analytics View v?i charts (LiveCharts2)
- [ ] Th?m Import/Export View
- [ ] Th?m Settings persistence (JSON file ho?c database)
- [ ] Th?m Dark theme support
- [ ] Th?m Keyboard shortcuts
- [ ] Th?m Notification system (Toast)
- [ ] Th?m Caching cho frequently accessed data
