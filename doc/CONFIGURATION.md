# External Event Providers Configuration Guide

## Overview

This guide explains how to configure external event providers for the Community Event Finder application. The application supports three major event provider APIs:

1. **PredictHQ** - Event intelligence platform
2. **Ticketmaster** - Ticket sales and events
3. **SeatGeek** - Ticketing and event discovery platform

## Configuration Requirements

All external provider configuration is stored in `appsettings.json` and organized under the `ExternalProviders` section.

### Key Principles

- **Security**: API keys should **NEVER** be hardcoded in source files or version control
- **Validation**: The application validates all enabled provider configurations on startup
- **Fail-Fast**: If required configuration is missing for an enabled provider, the application will fail with a clear error message
- **Per-Provider**: Each provider can be independently enabled/disabled

## Configuration Structure

```json
{
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
  }
}
```

## Environment-Specific Configuration

### Development (`appsettings.Development.json`)

For local development, you can add your API keys to `appsettings.Development.json` which is safe since it's git-ignored:

```json
{
  "ExternalProviders": {
    "RefreshIntervalMinutes": 30,
    "PredictHQ": {
      "Enabled": true,
      "EventsUrl": "https://api.predicthq.com/v1/events",
      "ApiKey": "your-development-api-key-here"
    }
  }
}
```

### Production

For production environments, configure API keys through:

1. **Environment Variables**: Set environment-specific variables and reference them
2. **Azure Key Vault**: Store secrets in Azure Key Vault and load them
3. **User Secrets**: Use .NET User Secrets (for development only)
4. **CI/CD Pipelines**: Inject secrets during deployment

Example using environment variables in `appsettings.json`:

```json
{
  "ExternalProviders": {
    "PredictHQ": {
      "ApiKey": "${PREDICTHQ_API_KEY}"
    }
  }
}
```

## Provider-Specific Configuration

### PredictHQ

**Documentation**: https://docs.predicthq.com/

**Configuration Parameters**:
- `Enabled`: Set to `true` to enable this provider
- `EventsUrl`: Complete events endpoint URL (e.g., `https://api.predicthq.com/v1/events`)
- `ApiKey`: Bearer token for authentication

**Getting Started**:
1. Sign up at https://www.predicthq.com/
2. Create an API token in the PredictHQ dashboard
3. Add the token to your configuration

**Example**:
```json
{
  "PredictHQ": {
    "Enabled": true,
    "EventsUrl": "https://api.predicthq.com/v1/events",
    "ApiKey": "your-predicthq-bearer-token"
  }
}
```

### Ticketmaster

**Documentation**: https://developer.ticketmaster.com/

**Configuration Parameters**:
- `Enabled`: Set to `true` to enable this provider
- `EventsUrl`: Complete events endpoint URL (e.g., `https://app.ticketmaster.com/discovery/v2/events.json`)
- `ApiKey`: API key for authentication

**Getting Started**:
1. Register at https://developer.ticketmaster.com/
2. Create an application in your developer dashboard
3. Generate an API key
4. Add the key to your configuration

**Example**:
```json
{
  "Ticketmaster": {
    "Enabled": true,
    "EventsUrl": "https://app.ticketmaster.com/discovery/v2/events.json",
    "ApiKey": "your-ticketmaster-api-key"
  }
}
```

### SeatGeek

**Documentation**: https://platform.seatgeek.com/

**Configuration Parameters**:
- `Enabled`: Set to `true` to enable this provider
- `EventsUrl`: Complete events endpoint URL (e.g., `https://api.seatgeek.com/2/events`)
- `ClientId`: Client ID for authentication (note: SeatGeek doesn't require client secret for public API)

**Getting Started**:
1. Sign up at https://seatgeek.com/
2. Register your application on https://platform.seatgeek.com/
3. Obtain your Client ID
4. Add the Client ID to your configuration

**Example**:
```json
{
  "SeatGeek": {
    "Enabled": true,
    "EventsUrl": "https://api.seatgeek.com/2/events",
    "ClientId": "your-seatgeek-client-id"
  }
}
```

## Global Settings

### RefreshIntervalMinutes

Specifies how frequently external event data should be refreshed (in minutes).

**Default**: 60 minutes
**Recommended**: 30-120 minutes depending on your needs

```json
{
  "ExternalProviders": {
    "RefreshIntervalMinutes": 60
  }
}
```

## Validation & Error Handling

### Configuration Validation

The application validates all enabled provider configurations during startup:

1. **Enabled providers must have all required fields populated**
2. **Missing required configuration throws `InvalidOperationException`**
3. **Disabled providers are not validated**

### Validation Error Example

If you enable PredictHQ but forget to set the API key:

```
Unhandled exception. System.InvalidOperationException: 
External Providers Configuration Errors:
PredictHQ ApiKey is required when enabled
```

### Clear Error Messages

Each configuration error clearly indicates:
- Which provider has the issue
- Which field is missing or invalid
- What is required

## Usage in Code

### Injecting the Provider Factory

```csharp
public class MyEventService
{
    private readonly IExternalEventProviderFactory _providerFactory;
    
    public MyEventService(IExternalEventProviderFactory providerFactory)
    {
        _providerFactory = providerFactory;
    }
    
    public async Task FetchEventsAsync()
    {
        var providers = _providerFactory.GetEnabledProviders();
        
        foreach (var provider in providers)
        {
            var events = await provider.GetEventsAsync(
                latitude: 40.7128m,
                longitude: -74.0060m,
                radiusMiles: 5
            );
        }
    }
}
```