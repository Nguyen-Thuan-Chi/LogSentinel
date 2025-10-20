# LogSentinel - Security Event Monitoring & Alert System

[![Build Status](https://img.shields.io/badge/build-passing-brightgreen)](https://github.com/Nguyen-Thuan-Chi/LogSentinel)
[![.NET Version](https://img.shields.io/badge/.NET-9.0-blueviolet)](https://dotnet.microsoft.com/download/dotnet/9.0)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

**LogSentinel** is a comprehensive security information and event management (SIEM) solution built with .NET 9 and WPF. It provides real-time monitoring, rule-based alerting, and advanced analytics for Windows security events and custom log files.



---

## 📜 Table of Contents

- [✨ Key Features](#-key-features)
- [📋 Prerequisites](#-prerequisites)
- [🏗️ Architecture](#️-architecture)
- [🚀 Quick Start Guide](#-quick-start-guide)
- [📖 Usage Guide](#-usage-guide)
- [🛠️ For Developers & Contributors](#️-for-developers--contributors)
- [📦 Deployment](#-deployment)
- [📝 Sample Rules Included](#-sample-rules-included)
- [🤔 Troubleshooting](#-troubleshooting)
- [📜 License](#-license)

---

## ✨ Key Features

- **Real-time Event Monitoring**: Stream and process security events as they occur.
- **Rule-Based Detection**: Flexible detection rules defined in simple **YAML** files.
- **Full-Text Search**: Powered by **SQLite FTS5** for fast and powerful queries across all event data.
- **Modern UI**: An intuitive and responsive interface built with WPF and Material Design.
- **Data Visualization**: Charts and graphs using LiveCharts2 to visualize event trends.
- **Webhook Integration**: Send alerts to external systems like Slack, Discord, or custom APIs.
- **Dual Database Support**: Use **SQLite** for development and standalone deployments, or **SQL Server** for production environments.

---

## 📋 Prerequisites

- **.NET 9 SDK** or later.
- **Windows 10/11** or **Windows Server 2019+**.
- **Visual Studio 2022** (17.8+) or **Rider 2024.1+** (Optional, for development).
- **SQL Server 2019+** (Optional, for production deployments).

---

## 🏗️ Architecture

LogSentinel follows a clean 3-tier architecture for maintainability and scalability.

```
+-------------------------------------------------+
|  UI Layer (WPF - LogSentinel.UI)                |
|  - Material Design XAML Toolkit                 |
|  - MVVM Pattern (CommunityToolkit.Mvvm)         |
|  - LiveCharts2 for visualizations               |
+-------------------------------------------------+
                         |
                         V
+-------------------------------------------------+
|  Business Logic Layer (LogSentinel.BUS)         |
|  - Event Normalizer (Log Parsing)               |
|  - Rule Engine (YAML-based Detection)           |
|  - Alert Service (Notification & Export)        |
+-------------------------------------------------+
                         |
                         V
+-------------------------------------------------+
|  Data Access Layer (LogSentinel.DAL)            |
|  - Entity Framework Core 9.0                    |
|  - Repository Pattern                           |
|  - SQLite / SQL Server Providers                |
|  - FTS5 Full-Text Search                        |
+-------------------------------------------------+
```

---

## 🚀 Quick Start Guide

Get LogSentinel up and running in 5 minutes.

**1. Clone the Repository**
```bash
git clone [https://github.com/Nguyen-Thuan-Chi/LogSentinel.git](https://github.com/Nguyen-Thuan-Chi/LogSentinel.git)
cd LogSentinel
```

**2. Restore & Build**
```bash
dotnet restore
dotnet build
```

**3. Configure the Database**

The project uses SQLite by default. No configuration is needed. To use SQL Server, update the connection string in `appsettings.json`.

**4. Run Migrations**

The application automatically applies migrations on startup. Alternatively, you can run them manually:
```bash
dotnet ef database update --project LogSentinel.DAL
```

**5. Run the Application**
```bash
dotnet run --project "Log Sentinel"
```
Or simply press **F5** in Visual Studio.

---

## 📖 Usage Guide

- **Dashboard**: Get an at-a-glance view of system health, recent alerts, and event trends.
- **Events View**: Browse, search, and filter all ingested events. Right-click on an event for context actions.
- **Rules View**: Manage all detection rules. Enable/disable, edit YAML definitions, and test rules against events.

---

## 🛠️ For Developers & Contributors

We welcome contributions! Here's how to get started.

### Project Structure

The solution is organized into three main projects: `LogSentinel.UI`, `LogSentinel.BUS` (Business Logic), and `LogSentinel.DAL` (Data Access).

### Creating a Custom Rule

1.  Create a new `.yml` file in `LogSentinel.DAL/Data/Rules/`.
2.  Define the `selection`, `condition`, and `action` for your rule.
    ```yaml
    name: New Admin User Created
    description: Detects when a new user is added to a privileged group.
    severity: Medium
    enabled: true
    selection:
      event_id: 4732
    condition:
      # No specific condition needed, the event itself is the trigger
      count: 1
      timeframe: 60
    action:
      alert: true
      title: "Admin User '{user}' Created"
      description: "A new user '{user}' was added to the Administrators group."
    ```
3.  The application will automatically load the new rule on startup.

### Contribution Workflow

1.  **Fork** the repository.
2.  Create a new feature branch (`git checkout -b feature/YourAmazingFeature`).
3.  Commit your changes (`git commit -m 'feat: Add some AmazingFeature'`).
4.  Push to the branch (`git push origin feature/YourAmazingFeature`).
5.  Open a **Pull Request**.

---

## 📦 Deployment

### Standalone (SQLite)

1.  Publish the application as a self-contained executable:
    ```bash
    dotnet publish -c Release -r win-x64 --self-contained
    ```
2.  Copy the output from `bin\Release\net9.0-windows\win-x64\publish\`.
3.  Ensure the application has write permissions to the `data/` folder.

### Enterprise (SQL Server)

1.  Create a new database in your SQL Server instance.
2.  Update the `DefaultConnection` in `appsettings.Production.json` with your SQL Server connection string.
3.  Deploy the published application files to your server.

---

## 📝 Sample Rules Included

| Rule Name                 | Event ID | Description                                            |
| ------------------------- | -------- | ------------------------------------------------------ |
| **Failed Login Threshold** | 4625     | 5+ failed logins from the same user in 5 minutes.      |
| **Admin User Created** | 4732     | A user was added to the local Administrators group.    |
| **Suspicious PowerShell** | -        | Detects PowerShell commands with obfuscation flags.    |
| **Privilege Escalation** | 4672     | Special privileges assigned to a new logon.            |
| **Event Log Cleared** | 1102     | The Security event log was cleared.                    |
| **New Service Installed** | 7045     | A new service was installed on the system.             |
| **Account Lockout** | 4740     | A user account was locked out.                         |

---

## 🤔 Troubleshooting

-   **Issue: Database is locked.**
    -   **Solution**: This is a limitation of SQLite. Ensure only one instance of the application is running.
-   **Issue: Events are not appearing.**
    -   **Solution**: Check that the log file directory exists and that the application has the necessary read/write permissions. Review the application logs in the `logs/` directory for errors.
-   **Issue: A rule is not triggering.**
    -   **Solution**: In the Rules View, ensure the rule is enabled. Use a YAML validator to check your rule's syntax. Use the "Test Rule" feature against a known matching event.

---

## 📜 License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.