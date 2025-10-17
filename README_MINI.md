# LogSentinel Mini - SIEM Log Analysis System

## ?? M? t?

LogSentinel Mini l? m?t h? th?ng ph?n t?ch nh?t k? SIEM (Security Information and Event Management) d?nh cho Windows. ?ng d?ng thu th?p v? ph?n t?ch Windows Event Log, ?p d?ng c?c lu?t ph?t hi?n (detection rules) ?? c?nh b?o c?c h?nh vi b?t th??ng.

### ? T?nh n?ng ch?nh

- ? **Dashboard Real-time**: Hi?n th? th?ng k? s? ki?n, rules ?ang ho?t ??ng v? alerts
- ? **Rule Management**: Qu?n l? c?c lu?t ph?t hi?n d?ng YAML (Sigma-inspired)
- ? **Alert System**: H? th?ng c?nh b?o v?i kh? n?ng acknowledge, export CSV/JSON
- ? **Event Viewer**: Xem danh s?ch events v?i filter v? search
- ? **FTS5 Search**: T?m ki?m full-text nhanh tr?n SQLite
- ? **Real-time Streaming**: Thu th?p events real-time t? Windows Event Log

## ??? Ki?n tr?c

```
LogSentinel/
„¥„Ÿ„Ÿ LogSentinel.UI (WPF + MVVM)
„    „¥„Ÿ„Ÿ ViewModels/
„    „    „¥„Ÿ„Ÿ DashboardViewModel.cs
„    „    „¥„Ÿ„Ÿ RuleViewModel.cs
„    „    „¥„Ÿ„Ÿ AlertsViewModel.cs
„    „    „¤„Ÿ„Ÿ EventsViewModel.cs
„    „¤„Ÿ„Ÿ UI/
„        „¥„Ÿ„Ÿ DashboardView.xaml
„        „¥„Ÿ„Ÿ RuleView.xaml
„        „¥„Ÿ„Ÿ AlertsView.xaml
„        „¤„Ÿ„Ÿ EventsView.xaml
„¥„Ÿ„Ÿ LogSentinel.BUS (Business Logic)
„    „¥„Ÿ„Ÿ Services/
„    „    „¥„Ÿ„Ÿ RuleEngine.cs
„    „    „¥„Ÿ„Ÿ AlertService.cs
„    „    „¥„Ÿ„Ÿ EventNormalizer.cs
„    „    „¥„Ÿ„Ÿ EventImporter.cs
„    „    „¤„Ÿ„Ÿ RuleProvider.cs
„    „¤„Ÿ„Ÿ Models/
„        „¥„Ÿ„Ÿ RuleDefinition.cs
„        „¥„Ÿ„Ÿ EventDto.cs
„        „¤„Ÿ„Ÿ AlertDto.cs
„¤„Ÿ„Ÿ LogSentinel.DAL (Data Access)
    „¥„Ÿ„Ÿ Data/
    „    „¥„Ÿ„Ÿ AppDbContext.cs
    „    „¥„Ÿ„Ÿ EventEntity.cs
    „    „¥„Ÿ„Ÿ AlertEntity.cs
    „    „¥„Ÿ„Ÿ RuleEntity.cs
    „    „¤„Ÿ„Ÿ Rules/ (YAML rules)
    „¤„Ÿ„Ÿ Repositories/
        „¥„Ÿ„Ÿ EventRepository.cs
        „¥„Ÿ„Ÿ AlertRepository.cs
        „¤„Ÿ„Ÿ RuleRepository.cs
```

## ?? Y?u c?u h? th?ng

- **.NET 9.0 SDK**
- **Windows 10/11** (do s? d?ng Windows Event Log APIs)
- **SQL Server** ho?c **SQLite** (m?c ??nh)
- **Visual Studio 2022** (khuy?n d?ng)

## ?? C?i ??t

### 1. Clone repository

```bash
git clone https://github.com/Nguyen-Thuan-Chi/LogSentinel.git
cd LogSentinel
```

### 2. Restore packages

```bash
dotnet restore
```

### 3. C?u h?nh Database

Ch?nh s?a `appsettings.json` trong project `Log Sentinel`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=logsentinel.db"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

### 4. Run migrations

```bash
cd LogSentinel.DAL
dotnet ef database update
```

### 5. Build v? ch?y

```bash
dotnet build
dotnet run --project "Log Sentinel\LogSentinel.UI.csproj"
```

## ?? H??ng d?n s? d?ng

### Dashboard

Dashboard hi?n th? t?ng quan v? h? th?ng:
- **Active Events**: S? l??ng events hi?n t?i
- **Rules Active**: S? l??ng rules ?ang ho?t ??ng
- **Alerts**: S? l??ng alerts ch?a acknowledge
- **Recent Events**: 10 events g?n ??y nh?t
- **Active Rules**: 10 rules ?ang ho?t ??ng

### Rule Management

#### Th?m Rule m?i

1. Click **? Add Rule**
2. ?i?n th?ng tin:
   - **Rule Name**: T?n rule
   - **Description**: M? t?
   - **Severity**: Low/Medium/High/Critical
   - **YAML Content**: N?i dung rule theo format Sigma

#### V? d? YAML Rule

```yaml
name: Failed Login Threshold
description: Detects multiple failed login attempts from the same user
severity: High
enabled: true
selection:
  event_id: 4625
  level: Warning
condition:
  count: 5
  timeframe: 300  # seconds
  group_by: user
action:
  alert: true
  title: "Multiple Failed Login Attempts"
  description: "User {user} had {count} failed logins in 5 minutes"
```

#### Qu?n l? Rules

- **? Toggle**: B?t/t?t rule
- **? Edit**: Ch?nh s?a rule
- **?? Delete**: X?a rule
- **?? Search**: T?m ki?m rules

### Alert Management

#### Xem Alerts

- **Filter by Severity**: L?c theo m?c ?? nguy hi?m
- **Show Acknowledged**: Hi?n th?/?n alerts ?? acknowledge

#### Thao t?c v?i Alerts

- **? Acknowledge**: ??nh d?u ?? x? l?
- **?? View Details**: Xem chi ti?t
- **?? Delete**: X?a alert
- **?? Export CSV**: Xu?t ra file CSV
- **? Acknowledge All**: Acknowledge t?t c?

### Event Viewer

Xem danh s?ch events v?i c?c th?ng tin:
- **Time**: Th?i gian x?y ra
- **Level**: M?c ?? (Info/Warning/Error/Critical)
- **Message**: N?i dung s? ki?n
- **Host**: M?y t?nh
- **User**: Ng??i d?ng
- **Process**: Ti?n tr?nh

## ?? C?u tr?c YAML Rule

### Selection Criteria

```yaml
selection:
  event_id: 4625           # Event ID to match
  level: Warning           # Event level (Info/Warning/Error/Critical)
  provider: Security       # Event provider
  process: powershell.exe  # Process name
  user: admin              # Username
  host: SERVER-01          # Hostname
```

### Condition Criteria

```yaml
condition:
  # Simple condition - always trigger
  always: true

  # Threshold-based condition
  count: 5                # Number of events
  timeframe: 300          # Time window in seconds
  group_by: user          # Group by field (user/host/process)

  # Pattern matching
  pattern: "(-Enc|-NoP)"  # Regex pattern
  field: details_json     # Field to match against
```

### Action

```yaml
action:
  alert: true
  title: "Alert Title with {placeholders}"
  description: "Description with {user}, {host}, {count}"
```

**Placeholders h? tr?:**
- `{user}`: Username
- `{host}`: Hostname
- `{process}`: Process name
- `{count}`: Event count

## ??? Detection Rules m?u

### 1. Failed Login Threshold
Ph?t hi?n nhi?u l?n ??ng nh?p th?t b?i (brute-force attack)

### 2. Admin User Created
Ph?t hi?n khi user m?i ???c th?m v?o nh?m Administrators

### 3. Suspicious PowerShell Execution
Ph?t hi?n PowerShell ch?y v?i flags ??ng ng? (-Enc, -NoP, -W Hidden)

### 4. Security Log Cleared
Ph?t hi?n khi Security event log b? x?a

### 5. New Service Installation
Ph?t hi?n khi c? Windows service m?i ???c c?i ??t

## ?? API v? Extension

### IEventNormalizer
Chu?n h?a events t? Windows Event Log sang schema chung

### IRuleProvider
Load v? parse YAML rules

### IRuleEngine
??nh gi? events theo rules v? trigger alerts

### IAlertService
Qu?n l? alerts, export, v? notifications

## ?? Database Schema

### Events Table
- Id (PK)
- EventTime
- Host
- User
- EventId
- Provider
- Level
- Process
- ParentProcess
- Action
- Object
- DetailsJson
- RawXml

### Rules Table
- Id (PK)
- Name
- Description
- Severity
- YamlContent
- IsEnabled
- CreatedAt
- UpdatedAt
- LastTriggeredAt
- TriggerCount

### Alerts Table
- Id (PK)
- RuleId (FK)
- RuleName
- Severity
- Timestamp
- Title
- Description
- EventIdsJson
- MetadataJson
- IsAcknowledged
- AcknowledgedAt
- AcknowledgedBy

## ?? UI Components

### Material Design
S? d?ng MaterialDesignThemes.Wpf cho giao di?n hi?n ??i

### Color Scheme
- **Primary**: Blue (#2563EB)
- **Success**: Green (#10B981)
- **Warning**: Orange (#F59E0B)
- **Danger**: Red (#EF4444)
- **Gray**: (#6B7280)

## ?? Testing

```bash
dotnet test LogSentinel.Tests
```

### Test Coverage
- EventNormalizer
- RuleEngine
- AlertService

## ?? Logging

Logs ???c l?u t?i: `logs/logsentinel-{Date}.log`

S? d?ng Serilog cho structured logging

## ?? Security Notes

- ?ng d?ng c?n ch?y v?i quy?n Administrator ?? ??c Security Event Log
- Kh?ng l?u tr? passwords ho?c sensitive data
- Rules ???c validate tr??c khi execute

## ?? Contributing

1. Fork repository
2. T?o feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to branch (`git push origin feature/AmazingFeature`)
5. Open Pull Request

## ?? License

Distributed under the MIT License.

## ?? Authors

- **Nguyen Thuan Chi** - [GitHub](https://github.com/Nguyen-Thuan-Chi)

## ?? Acknowledgments

- Material Design in XAML
- Entity Framework Core
- Serilog
- YamlDotNet
- Sigma Rules Project

---

**?? Project for:** ?? ?n m?n h?c - H? th?ng SIEM mini ph?n t?ch nh?t k? Windows

**?? School:** [Your School Name]

**?? Date:** January 2025
