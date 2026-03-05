# Community Event Finder - Setup Script

# Optional: Remove .git directory
$removeGit = Read-Host "Remove .git directory? (y/n)"
if ($removeGit -eq "y" -or $removeGit -eq "Y") {
    if (Test-Path ".git") {
        Remove-Item -Path ".git" -Recurse -Force -ErrorAction SilentlyContinue
    }
}

# Database configuration
$Server = Read-Host "SQL Server name/address"
$Username = Read-Host "SQL Server username"
$securePassword = Read-Host "SQL Server password" -AsSecureString
$Password = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto([System.Runtime.InteropServices.Marshal]::SecureStringToCoTaskMemUnicode($securePassword))
$connectionString = "Server=$Server;Database=Community_Event_Finder;User Id=$Username;Password=$Password;TrustServerCertificate=true;"

# Provider configuration
$predictHQEnabled = (Read-Host "Enable PredictHQ? (y/n)") -eq "y"
$predictHQKey = if ($predictHQEnabled) { Read-Host "PredictHQ API key" } else { "" }

$ticketmasterEnabled = (Read-Host "Enable Ticketmaster? (y/n)") -eq "y"
$ticketmasterKey = if ($ticketmasterEnabled) { Read-Host "Ticketmaster API key" } else { "" }

$seatGeekEnabled = (Read-Host "Enable SeatGeek? (y/n)") -eq "y"
$seatGeekId = if ($seatGeekEnabled) { Read-Host "SeatGeek Client ID" } else { "" }

# Build settings and write as clean JSON
$jsonTemplate = @"
{
  "ConnectionStrings": {
    "DefaultConnection": "$connectionString"
  },
  "ExternalProviders": {
    "RefreshIntervalMinutes": 60,
    "PredictHQ": {
      "Enabled": $(if ($predictHQEnabled) { 'true' } else { 'false' }),
      "EventsUrl": "https://api.predicthq.com/v1/events",
      "ApiKey": "$predictHQKey"
    },
    "Ticketmaster": {
      "Enabled": $(if ($ticketmasterEnabled) { 'true' } else { 'false' }),
      "EventsUrl": "https://app.ticketmaster.com/discovery/v2/events.json",
      "ApiKey": "$ticketmasterKey"
    },
    "SeatGeek": {
      "Enabled": $(if ($seatGeekEnabled) { 'true' } else { 'false' }),
      "EventsUrl": "https://api.seatgeek.com/2/events",
      "ClientId": "$seatGeekId"
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
"@

$jsonTemplate | Out-File -FilePath "appsettings.Development.json" -Encoding UTF8

Write-Host "Setup complete. Next: dotnet ef database update"
