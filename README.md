# LogSentinel - Security Event Monitoring & Alert System

[![Build Status](https://img.shields.io/badge/build-passing-brightgreen)](https://github.com/Nguyen-Thuan-Chi/LogSentinel)
[![.NET Version](https://img.shields.io/badge/.NET-9.0-blueviolet)](https://dotnet.microsoft.com/download/dotnet/9.0)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

LogSentinel is a comprehensive security information and event management (SIEM) solution built with .NET 9 and WPF.
<br>
*LogSentinel là một giải pháp quản lý thông tin và sự kiện bảo mật (SIEM) toàn diện, được xây dựng bằng .NET 9 và WPF.*



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

- **Real-time Event Monitoring**
  <br>
  *Giám sát sự kiện thời gian thực*

- **Rule-Based Detection**
  <br>
  *Phát hiện dựa trên quy tắc YAML linh hoạt*

- **Full-Text Search**
  <br>
  *Tìm kiếm toàn văn mạnh mẽ với SQLite FTS5*

- **Modern UI**
  <br>
  *Giao diện người dùng hiện đại với WPF và Material Design*

- **Data Visualization**
  <br>
  *Trực quan hóa dữ liệu bằng biểu đồ và đồ thị*

- **Webhook Integration**
  <br>
  *Tích hợp Webhook để gửi cảnh báo đến hệ thống bên ngoài*

- **Dual Database Support**
  <br>
  *Hỗ trợ hai loại Database: SQLite và SQL Server*

---

## 📋 Prerequisites

- **.NET 9 SDK** or later.
- **Windows 10/11** or **Windows Server 2019+**.
- **Visual Studio 2022** (17.8+) or **Rider 2024.1+** (Optional, for development).
- **SQL Server 2019+** (Optional, for production deployments).

---

## 🏗️ Architecture

LogSentinel follows a clean 3-tier architecture for maintainability and scalability.
<br>
*LogSentinel tuân theo kiến trúc 3 tầng rõ ràng để dễ bảo trì và mở rộng.*

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

The application automatically applies migrations on startup. For manual execution:
<br>
*Ứng dụng sẽ tự động áp dụng migration khi khởi động. Để chạy thủ công:*
```bash
dotnet ef database update --project LogSentinel.DAL
```

**5. Run the Application**
```bash
dotnet run --project "Log Sentinel"
```
Or simply press **F5** in Visual Studio.
<br>
*Hoặc chỉ cần nhấn F5 trong Visual Studio.*

---

## 📖 Usage Guide

- **Dashboard**: Get an at-a-glance view of system health, recent alerts, and event trends.
  <br>
  *Cung cấp cái nhìn tổng quan về hệ thống, các cảnh báo gần đây và xu hướng sự kiện.*
- **Events View**: Browse, search, and filter all ingested events. Right-click for context actions.
  <br>
  *Duyệt, tìm kiếm và lọc tất cả sự kiện. Nhấp chuột phải vào một sự kiện để xem các hành động ngữ cảnh.*
- **Rules View**: Manage all detection rules (enable/disable, edit YAML, and test).
  <br>
  *Quản lý tất cả quy tắc phát hiện (bật/tắt, chỉnh sửa YAML và kiểm tra).*

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
| ------------------------  | -------- | ------------------------------------------------------ |
| **Failed Login Threshold**| 4625     | 5+ failed logins from the same user in 5 minutes.      |
| **Admin User Created**    | 4732     | A user was added to the local Administrators group.    |
| **Suspicious PowerShell** | -        | Detects PowerShell commands with obfuscation flags.    |
| **Privilege Escalation**  | 4672     | Special privileges assigned to a new logon.            |
| **Event Log Cleared**     | 1102     | The Security event log was cleared.                    |
| **New Service Installed** | 7045     | A new service was installed on the system.             |
| **Account Lockout**       | 4740     | A user account was locked out.                         |

---

## 🤔 Troubleshooting

-   **Issue: Database is locked.**
    -   *Vấn đề: Database bị khóa.*
    -   **Solution**: This is a limitation of SQLite. Ensure only one instance of the application is running.
    -   *Giải pháp: Đây là giới hạn của SQLite. Hãy đảm bảo chỉ có một phiên bản của ứng dụng đang chạy.*
-   **Issue: Events are not appearing.**
    -   *Vấn đề: Sự kiện không xuất hiện.*
    -   **Solution**: Check that the log file directory exists and the application has permissions. Review application logs in the `logs/` directory for errors.
    -   *Giải pháp: Kiểm tra thư mục chứa log có tồn tại và ứng dụng có quyền truy cập không. Xem file log của ứng dụng trong thư mục `logs/` để tìm lỗi.*
-   **Issue: A rule is not triggering.**
    -   *Vấn đề: Một quy tắc không được kích hoạt.*
    -   **Solution**: In the Rules View, ensure the rule is enabled. Use a YAML validator to check your rule's syntax.
    -   *Giải pháp: Trong màn hình Rules, hãy chắc chắn rằng quy tắc đã được bật. Sử dụng một công cụ kiểm tra cú pháp YAML để đảm bảo file rule của bạn chính xác.*

---

## 📜 License

This project is licensed under the MIT License.
<br>
*Dự án này được cấp phép dưới Giấy phép MIT.*