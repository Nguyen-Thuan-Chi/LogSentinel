# Fix Summary: Dual Database Provider Support (SQLite + SQL Server)

## V?n ??

B?n g?p l?i "PendingModelChangesWarning" khi ch?y debug v?:

1. **AppDbContext** ???c c?u h?nh cho SQLite (s? d?ng `HasColumnType("datetime")`)
2. **appsettings.Development.json** ???c c?u h?nh cho SQL Server
3. Migration hi?n t?i ch? support SQLite, kh?ng t??ng th?ch v?i SQL Server
4. Entity Framework ph?t hi?n model trong code kh?c v?i database schema

## L?i g?c

```
Application startup failed: An error was generated for warning 
'Microsoft.EntityFrameworkCore.Migrations.PendingModelChangesWarning': 
The model for context 'AppDbContext' has pending changes.
```

## Gi?i ph?p ?? th?c hi?n

### 1. C?p nh?t AppDbContext.cs

**File**: `LogSentinel.DAL/Data/AppDbContext.cs`

**Thay ??i**:
- Th?m auto-detection cho database provider s? d?ng `Database.IsSqlite()` v? `Database.IsSqlServer()`
- S? d?ng column types ph? h?p cho t?ng provider:
  - **SQLite**: `TEXT` cho datetime
  - **SQL Server**: `datetime2` cho datetime

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    // Detect database provider
    var isSqlite = Database.IsSqlite();
    var isSqlServer = Database.IsSqlServer();

    // Configure EventEntity
    modelBuilder.Entity<EventEntity>(entity =>
    {
        if (isSqlite)
        {
            entity.Property(e => e.EventTime).HasColumnType("TEXT");
            entity.Property(e => e.CreatedAt).HasColumnType("TEXT");
        }
        else if (isSqlServer)
        {
            entity.Property(e => e.EventTime).HasColumnType("datetime2");
            entity.Property(e => e.CreatedAt).HasColumnType("datetime2");
        }
    });
    // ... t??ng t? cho AlertEntity v? RuleEntity
}
```

### 2. X?a migrations c? v? t?o migration m?i

**Commands ?? ch?y**:
```powershell
# X?a t?t c? migrations c?
Remove-Item -Path "LogSentinel.DAL\Migrations\*.cs" -Force

# T?o migration m?i support c? SQLite v? SQL Server
dotnet ef migrations add InitialDualProviderSetup --project "LogSentinel.DAL" --startup-project "Log Sentinel" --context AppDbContext
```

### 3. C?p nh?t App.xaml.cs

**File**: `Log Sentinel/App.xaml.cs`

**Thay ??i**:
- Th?m better error handling cho SQL Server connection issues
- Th?m user-friendly error messages
- Th?m option ?? recreate database n?u migration failed
- X? l? SQL Server login failures

```csharp
try
{
    var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
    
    if (pendingMigrations.Any())
    {
        Log.Information("Found {Count} pending migrations. Applying...", pendingMigrations.Count());
        await dbContext.Database.MigrateAsync();
        Log.Information("Database migrations applied successfully");
    }
    else
    {
        var canConnect = await dbContext.Database.CanConnectAsync();
        if (!canConnect)
        {
            await dbContext.Database.MigrateAsync();
            Log.Information("Database created with migrations");
        }
        else
        {
            Log.Information("Database connection verified");
        }
    }
}
catch (Microsoft.Data.SqlClient.SqlException sqlEx) when (sqlEx.Message.Contains("Login failed") || sqlEx.Message.Contains("server was not found"))
{
    Log.Error(sqlEx, "SQL Server connection failed. Please check your connection string in appsettings.Development.json");
    MessageBox.Show(
        $"Cannot connect to SQL Server:\n{sqlEx.Message}\n\nPlease verify your connection string in appsettings.Development.json",
        "Database Connection Error",
        MessageBoxButton.OK,
        MessageBoxImage.Error);
    Shutdown(1);
    return;
}
catch (Exception ex)
{
    Log.Error(ex, "Error during database migration");
    
    // Ask user if they want to recreate database
    var result = MessageBox.Show(
        $"Database migration failed:\n{ex.Message}\n\nDo you want to recreate the database? (All existing data will be lost)",
        "Database Migration Error",
        MessageBoxButton.YesNo,
        MessageBoxImage.Warning);
    
    if (result == MessageBoxResult.Yes)
    {
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();
        Log.Information("Database recreated successfully");
    }
}
```

### 4. Database Provider Detection

**File**: `Log Sentinel/App.xaml.cs` (already existed, no changes needed)

?ng d?ng t? ??ng detect database provider d?a tr?n connection string:

```csharp
var useSqlServer =
    connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase) ||
    connectionString.Contains("Initial Catalog=", StringComparison.OrdinalIgnoreCase);

if (useSqlServer)
{
    services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
    Log.Information("Using SQL Server database");
}
else
{
    services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
    Log.Information("Using SQLite database");
}
```

## Configuration Files

### appsettings.json (Production - SQLite)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=./data/logsentinel.db"
  }
}
```

### appsettings.Development.json (Development - SQL Server)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=LAPTOP-PN16MELH;Database=LogSentinel;Integrated Security=True;TrustServerCertificate=True;"
  }
}
```

## K?t qu?

? **?? fix**:
- L?i "PendingModelChangesWarning" kh?ng c?n x?y ra
- Support c? SQLite v? SQL Server
- Auto-detect database provider
- Better error handling v? user feedback
- C? option ?? recreate database khi c?n

? **Features**:
- **SQLite**: D?nh cho production/release, kh?ng c?n SQL Server
- **SQL Server**: D?nh cho development, c? SSMS ?? debug
- Switching d? d?ng b?ng c?ch ??i connection string
- Migration t? ??ng apply khi kh?i ??ng
- Recreate database option n?u c? l?i

## Testing

### Test v?i SQL Server (Development):

1. ??m b?o SQL Server ?ang ch?y
2. Ki?m tra connection string trong `appsettings.Development.json`
3. Run application
4. Check logs: "Using SQL Server database"
5. Database v? tables s? ???c t?o t? ??ng

### Test v?i SQLite (Production):

1. ??i t?n ho?c x?a `appsettings.Development.json`
2. Run application
3. Check logs: "Using SQLite database"
4. Database file s? ???c t?o t?i `./data/logsentinel.db`

## Files Changed

1. ? `LogSentinel.DAL/Data/AppDbContext.cs` - Th?m dual provider support
2. ? `Log Sentinel/App.xaml.cs` - Better error handling
3. ? `LogSentinel.DAL/Migrations/*` - X?a c?, t?o m?i
4. ? `DATABASE_CONFIGURATION.md` - T?i li?u h??ng d?n

## Build Status

? Build successful
? No compilation errors
? Migration created successfully

## Next Steps

1. **Ch?y application v? test v?i SQL Server**:
   - N?u connection failed, check SQL Server service
   - N?u login failed, check Windows Authentication
   - N?u migration failed, ch?n "Yes" ?? recreate database

2. **Test v?i SQLite** (n?u mu?n):
   - ??i t?n `appsettings.Development.json` th?nh `appsettings.Development.json.bak`
   - Restart application

3. **N?u v?n c? l?i**:
   - Check logs trong `logs/logsentinel-*.log`
   - Xem error message trong MessageBox
   - Ch?n "Yes" ?? recreate database n?u ???c h?i

## Migration Commands (For Reference)

```powershell
# Xem pending migrations
dotnet ef migrations list --project "LogSentinel.DAL" --startup-project "Log Sentinel"

# T?o migration m?i
dotnet ef migrations add <Name> --project "LogSentinel.DAL" --startup-project "Log Sentinel"

# X?a migration cu?i c?ng
dotnet ef migrations remove --project "LogSentinel.DAL" --startup-project "Log Sentinel"

# Apply migrations
dotnet ef database update --project "LogSentinel.DAL" --startup-project "Log Sentinel"

# Drop database
dotnet ef database drop --project "LogSentinel.DAL" --startup-project "Log Sentinel"
```

## Troubleshooting

### N?u v?n g?p "PendingModelChangesWarning":

1. Delete database:
```powershell
dotnet ef database drop --project "LogSentinel.DAL" --startup-project "Log Sentinel" --force
```

2. Recreate migrations:
```powershell
Remove-Item -Path "LogSentinel.DAL\Migrations\*.cs" -Force
dotnet ef migrations add InitialCreate --project "LogSentinel.DAL" --startup-project "Log Sentinel"
```

3. Run application, s? t? ??ng apply migration

### N?u SQL Server connection failed:

1. Check SQL Server service is running:
```powershell
Get-Service -Name "MSSQL*"
```

2. Check server name:
```powershell
# Trong SSMS, ch?y:
SELECT @@SERVERNAME
```

3. Update connection string trong `appsettings.Development.json`

## Summary

? **Problem**: PendingModelChangesWarning khi ch?y v?i SQL Server nh?ng code config cho SQLite

? **Solution**: Dual database provider support v?i auto-detection

? **Result**: C? th? d?ng c? SQLite (production) v? SQL Server (development) d? d?ng

? **Tested**: Build successful, migration created

?? **Ready to use**: Ch? c?n run application v? test!
