# Community-Event-Finder

[![Build and Test](https://github.com/bortiz-101/Community-Event-Finder/actions/workflows/build.yml/badge.svg)](https://github.com/bortiz-101/Community-Event-Finder/actions/workflows/build.yml)
[![Code Analysis](https://github.com/bortiz-101/Community-Event-Finder/actions/workflows/code-analysis.yml/badge.svg)](https://github.com/bortiz-101/Community-Event-Finder/actions/workflows/code-analysis.yml)

![Project UML Diagram](UML_DIAGRAM.drawio.svg)

COMP 425 Final Project

## Getting Started

### Prerequisites

- **.NET 10 SDK** or later
- **SQL Server** (local or remote instance)
- **PowerShell** (for running setup script)

### Quick Setup

Run the setup script from the project root:

```powershell
.\setup-db.ps1
```

The script will:
1. **Remove .git directory** - Prevents accidental API key commits to version control
2. **Configure database connection** - Prompts for SQL Server details
3. **Configure external providers** - Lets you enable/disable event providers and enter API keys:
   - PredictHQ (https://docs.predicthq.com/)
   - Ticketmaster (https://developer.ticketmaster.com/)
   - SeatGeek (https://platform.seatgeek.com/)
4. **Save configuration** - Writes everything to `appsettings.Development.json`

### After Running Setup

1. **Apply database migrations**:
   ```powershell
   dotnet ef database update
   ```

2. **Build the project**:
   ```powershell
   dotnet build
   ```

3. **Run the application**:
   ```powershell
   dotnet run
   ```

### Configuration Files

- **appsettings.json** - Production defaults (placeholder values, safe to commit)
- **appsettings.Development.json** - Local development config (⚠️ .gitignored, contains real secrets)

**Important Security Notes:**
- `appsettings.Development.json` is in `.gitignore` and will **NOT** be committed to Git
- Never hardcode API keys in source files or version control
- For production, use environment variables or Azure Key Vault
- The setup script removes `.git` to prevent accidental commits of API keys

### Manual Configuration (Alternative to Setup Script)

If you prefer manual setup, edit `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=Community_Event_Finder;User Id=YOUR_USERNAME;Password=YOUR_PASSWORD;TrustServerCertificate=true;"
  },
  "ExternalProviders": {
    "RefreshIntervalMinutes": 60,
    "PredictHQ": {
      "Enabled": false,
      "EventsUrl": "https://api.predicthq.com/v1/events",
      "ApiKey": ""
    },
    "Ticketmaster": {
      "Enabled": false,
      "EventsUrl": "https://app.ticketmaster.com/discovery/v2/events.json",
      "ApiKey": ""
    },
    "SeatGeek": {
      "Enabled": false,
      "EventsUrl": "https://api.seatgeek.com/2/events",
      "ClientId": ""
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

Replace:
- `YOUR_SERVER` - SQL Server instance name/address
- `YOUR_USERNAME` - SQL Server username
- `YOUR_PASSWORD` - SQL Server password

### Database Migrations

#### Local Development
When developing locally, apply migrations directly to your local database:
```powershell
dotnet ef database update
```

#### Remote/Production Databases

For remote databases, **always** generate a SQL script for review before applying changes:

1. **Generate SQL migration script**:
   ```powershell
   dotnet ef migrations script --output migration.sql
   ```

2. **Review the generated SQL** in `migration.sql` before applying to production

3. **Option A - Apply script manually** (Recommended for Production):
   - Execute the SQL script directly on your remote database using SQL Server Management Studio or your database admin tools
   - This gives you full control and auditability

4. **Option B - Apply directly via connection string**:
   ```powershell
   dotnet ef database update --connection "Server=YOUR_REMOTE_SERVER;Database=Community_Event_Finder;User Id=YOUR_USERNAME;Password=YOUR_PASSWORD;TrustServerCertificate=true;"
   ```

**Best Practices:**
- Always back up your remote database before applying migrations
- Keep your application code and database schema in sync across all environments
- Test migrations on a staging database before applying to production
- Review migration scripts carefully for data loss risks (especially DROP operations)

---

![UI State Machine Diagram](doc/UI_NAVIGATION.drawio.svg)

![Events State Machine Diagram](doc/EVENT_BACKEND.drawio.svg)
