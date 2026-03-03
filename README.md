# Community-Event-Finder
![Project UML Diagram](UML_DIAGRAM.drawio.svg)

COMP 425 Final Project

## Getting Started

### Database Configuration (Required)

This project uses SQL Server. You **must** configure the database connection string before running the application.

#### Option 1: Automated Setup (Recommended)

Run the setup script from the project root:
```powershell
.\setup-db.ps1
```

The script will prompt you for:
- SQL Server address/name
- Username
- Password

Then run migrations:
```powershell
dotnet ef database update
```

#### Option 2: Manual Setup

1. **Initialize User Secrets** (one-time setup):
   ```powershell
   dotnet user-secrets init
   ```

2. **Set your SQL Server connection string**:
   ```powershell
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=YOUR_SERVER;Database=Community_Event_Finder;User Id=YOUR_USERNAME;Password=YOUR_PASSWORD;TrustServerCertificate=true;"
   ```
   Replace:
   - `YOUR_SERVER` - your SQL Server instance name or address
   - `YOUR_USERNAME` - SQL Server login username
   - `YOUR_PASSWORD` - SQL Server login password

3. **Apply database migrations**:
   ```powershell
   dotnet ef database update
   ```

**Important:** Your connection string is stored locally in user secrets and will never be committed to Git. **Do not hardcode credentials in configuration files.**

#### Understanding Configuration Sources

The `appsettings.json` file contains a placeholder connection string for reference, but your actual credentials come from **User Secrets**, which override it. 

**Configuration Priority** (highest to lowest):
1. **User Secrets** (local machine only, never committed to Git)
2. **appsettings.Development.json** (development environment)
3. **Environment Variables**
4. **appsettings.json** (default/production fallback)

When the application starts, it reads the connection string `DefaultConnection` from the highest priority source. So:
- In **development**: Your user secrets connection string is used
- In **production**: appsettings.json would be used (but you should use environment secrets there)
- The placeholder in `appsettings.json` is just documentation and safe to commit

This approach ensures real credentials are never in version control while keeping configuration centralized.

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
