# Community Event Finder - Database Setup Script
# This script automates the SQL Server connection configuration

param(
    [string]$Server = $env:SQL_SERVER,
    [string]$Username = $env:SQL_USERNAME,
    [string]$Password = $env:SQL_PASSWORD,
    [string]$Database = "Community_Event_Finder"
)

Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "Community Event Finder - Database Setup" -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host ""

# Prompt for SQL Server details if not provided
if ([string]::IsNullOrEmpty($Server)) {
    $Server = Read-Host "Enter SQL Server name/address (e.g., localhost or your-server.database.windows.net)"
}

if ([string]::IsNullOrEmpty($Username)) {
    $Username = Read-Host "Enter SQL Server username (e.g., sa)"
}

if ([string]::IsNullOrEmpty($Password)) {
    $securePassword = Read-Host "Enter SQL Server password" -AsSecureString
    $Password = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto([System.Runtime.InteropServices.Marshal]::SecureStringToCoTaskMemUnicode($securePassword))
}

# Build connection string
$connectionString = "Server=$Server;Database=$Database;User Id=$Username;Password=$Password;TrustServerCertificate=true;"

Write-Host ""
Write-Host "Configuration Summary:" -ForegroundColor Yellow
Write-Host "  Server:   $Server"
Write-Host "  Database: $Database"
Write-Host "  Username: $Username"
Write-Host ""

# Initialize user secrets if not already done
Write-Host "Initializing user secrets..." -ForegroundColor Green
dotnet user-secrets init --warn-on-init 2>&1 | Out-Null

# Set the connection string in user secrets
Write-Host "Setting connection string in user secrets..." -ForegroundColor Green
dotnet user-secrets set "ConnectionStrings:DefaultConnection" $connectionString

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "[OK] User secrets configured successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "1. Run database migrations: dotnet ef database update"
    Write-Host "2. Build the project: dotnet build"
    Write-Host "3. Run the application: dotnet run"
    Write-Host ""
    Write-Host "Your connection string is stored locally and will NOT be committed to Git." -ForegroundColor Yellow
}
else {
    Write-Host ""
    Write-Host "[ERROR] Error setting user secrets. Please check your input and try again." -ForegroundColor Red
    exit 1
}
