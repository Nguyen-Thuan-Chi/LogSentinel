# Database Configuration Guide

## T?ng quan

LogSentinel hi?n h? tr? **2 database providers**:
- **SQLite** - D?nh cho production/release (ng??i d?ng cu?i)
- **SQL Server** - D?nh cho development/testing

## C?u h?nh

### 1. SQLite (Production/Release)

**File**: `appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=./data/logsentinel.db"
  }
}
```

- SQLite ???c s? d?ng khi connection string ch?a `Data Source=` ho?c `Filename=`
- Kh?ng c?n c?i ??t server
- File database s? ???c t?o t? ??ng
- Ph? h?p cho ng??i d?ng cu?i

### 2. SQL Server (Development)

**File**: `appsettings.Development.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=LAPTOP-PN16MELH;Database=LogSentinel;Integrated Security=True;TrustServerCertificate=True;"
  }
}
```

- SQL Server ???c s? d?ng khi connection string ch?a `Server=` ho?c `Initial Catalog=`
- C?n c? SQL Server/SSMS ?? c?i ??t
- Ph? h?p cho development v? testing

## C?ch th?c ho?t ??ng

?ng d?ng t? ??ng ph?t hi?n database provider d?a tr?n connection string:

```csharp
var useSqlServer = 
    connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase) ||
    connectionString.Contains("Initial Catalog=", StringComparison.OrdinalIgnoreCase);
```

## Migration

### T?o migration m?i

```powershell
dotnet ef migrations add <MigrationName> --project "LogSentinel.DAL" --startup-project "Log Sentinel" --context AppDbContext
```

### Apply migration

Migration s? ???c apply t? ??ng khi kh?i ??ng ?ng d?ng. N?u c? l?i, b?n s? ???c h?i c? mu?n recreate database hay kh?ng.

### X?a database v? t?o l?i

N?u g?p l?i "pending model changes", ?ng d?ng s? h?i b?n c? mu?n x?a v? t?o l?i database kh?ng.

## Troubleshooting

### L?i: "Login failed" ho?c "Server was not found"

**Nguy?n nh?n**: Kh?ng th? k?t n?i ??n SQL Server

**Gi?i ph?p**:
1. Ki?m tra SQL Server ?? ???c kh?i ??ng ch?a
2. Ki?m tra server name trong connection string c? ??ng kh?ng
3. Ki?m tra Windows Authentication c? ???c enable kh?ng

### L?i: "Pending model changes"

**Nguy?n nh?n**: Model trong code ?? thay ??i nh?ng database ch?a ???c c?p nh?t

**Gi?i ph?p**:
1. ?ng d?ng s? t? ??ng h?i b?n c? mu?n recreate database kh?ng
2. Ho?c t?o migration m?i v? apply

### L?i: Migration failed

**Gi?i ph?p**:
1. X?a t?t c? migrations c?:
```powershell
Remove-Item -Path "LogSentinel.DAL\Migrations\*.cs" -Force
```

2. T?o migration m?i:
```powershell
dotnet ef migrations add InitialCreate --project "LogSentinel.DAL" --startup-project "Log Sentinel" --context AppDbContext
```

3. Ho?c ch?n "Yes" khi ?ng d?ng h?i c? mu?n recreate database

## Switching Between Providers

### T? SQL Server v? SQLite

1. S?a `appsettings.Development.json` ho?c ??i t?n file th?nh `appsettings.Development.json.bak`
2. ?ng d?ng s? t? ??ng s? d?ng SQLite t? `appsettings.json`

### T? SQLite l?n SQL Server

1. T?o/s?a `appsettings.Development.json` v?i connection string SQL Server
2. Kh?i ??ng ?ng d?ng, migration s? ???c apply t? ??ng

## Best Practices

1. **Development**: D?ng SQL Server ?? c? tools t?t h?n (SSMS, profiler, etc.)
2. **Production**: D?ng SQLite ?? d? deploy v? kh?ng c?n c?i ??t server
3. **Backup**: Lu?n backup database tr??c khi recreate
4. **Migration**: Test migration tr?n c? SQLite v? SQL Server tr??c khi commit

## Database Schema

C? SQLite v? SQL Server ??u s? d?ng c?ng m?t schema:

- **Events** - L?u tr? log events
- **Alerts** - L?u tr? alerts ???c trigger
- **Rules** - L?u tr? detection rules

## AppDbContext Configuration

`AppDbContext` t? ??ng detect database provider v? apply configuration ph? h?p:

- **SQLite**: S? d?ng `TEXT` cho datetime columns
- **SQL Server**: S? d?ng `datetime2` cho datetime columns

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    var isSqlite = Database.IsSqlite();
    var isSqlServer = Database.IsSqlServer();
    
    if (isSqlite)
    {
        entity.Property(e => e.EventTime).HasColumnType("TEXT");
    }
    else if (isSqlServer)
    {
        entity.Property(e => e.EventTime).HasColumnType("datetime2");
    }
}
```

## Notes

- C? hai providers ??u ???c test v? support
- Migration ???c t?o ?? t??ng th?ch v?i c? hai
- Switching gi?a providers kh?ng l?m m?t data (n?u b?n backup tr??c)
- ?ng d?ng s? t? ??ng handle connection errors v? migration issues
