using Community_Event_Finder.Data.ExternalProviders;
using Community_Event_Finder.Models;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Community_Event_Finder.Data
{
    /// <summary>
    /// Background service that periodically syncs events from external providers.
    /// Runs at regular intervals based on configuration (RefreshIntervalMinutes).
    /// </summary>
    public class EventSyncService : BackgroundService
    {
        private readonly ILogger<EventSyncService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly ExternalProvidersSettings _settings;
        private Timer? _timer;

        public EventSyncService(
            ILogger<EventSyncService> logger,
            IServiceProvider serviceProvider,
            IOptions<ExternalProvidersSettings> settings)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _settings = settings.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("EventSyncService started");

            // Set up periodic sync (no initial sync to avoid startup delays)
            var refreshIntervalMinutes = _settings.RefreshIntervalMinutes > 0
                ? _settings.RefreshIntervalMinutes
                : 60; // Default to 60 minutes if not configured

            var timerInterval = TimeSpan.FromMinutes(refreshIntervalMinutes);
            _logger.LogInformation($"Starting periodic sync every {refreshIntervalMinutes} minutes");

            _timer = new Timer(
                async _ => await PeriodicSync(stoppingToken),
                null,
                timerInterval,
                timerInterval);

            // Keep the service running
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private async Task PeriodicSync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation("Starting periodic event sync...");
                await SyncEventsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during periodic sync: {ex.Message}");
            }
        }

        /// <summary>
        /// Synchronizes events from all enabled providers and returns a summary of the operation.
        /// </summary>
        private async Task<EventSyncSummary> SyncEventsAsync(CancellationToken stoppingToken)
        {
            var summary = new EventSyncSummary();

            using (var scope = _serviceProvider.CreateScope())
            {
                var providerFactory = scope.ServiceProvider.GetRequiredService<IExternalEventProviderFactory>();
                var normalizationService = scope.ServiceProvider.GetRequiredService<INormalizationService>();
                var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();

                try
                {
                    var providers = providerFactory.GetEnabledProviders().ToList();

                    if (!providers.Any())
                    {
                        _logger.LogWarning("No external providers enabled for sync");
                        summary.Message = "No providers enabled";
                        return summary;
                    }

                    var allNormalizedEvents = new List<EventItem>();

                    // Fetch and process events from all enabled providers
                    foreach (var provider in providers)
                    {
                        try
                        {
                            _logger.LogInformation($"Fetching events from {provider.ProviderName}...");
                            summary.ProvidersProcessed.Add(provider.ProviderName);

                            var events = await provider.GetEventsAsync(
                                latitude: _settings.SearchLatitude,
                                longitude: _settings.SearchLongitude,
                                radiusMiles: _settings.SearchRadiusMiles,
                                cancellationToken: stoppingToken);

                            _logger.LogInformation($"Retrieved {events.Count} events from {provider.ProviderName}");
                            summary.EventsFetched += events.Count;

                            if (!events.Any())
                                continue;

                            // Determine the event source type
                            var sourceType = GetEventSourceType(provider.ProviderName);
                            if (sourceType == null)
                            {
                                _logger.LogWarning($"Unknown provider name: {provider.ProviderName}, skipping");
                                summary.ErrorsEncountered[$"{provider.ProviderName}_Unknown"] = "Unknown provider name";
                                continue;
                            }

                            // Normalize and track valid/invalid counts
                            _logger.LogInformation($"Normalizing {events.Count} events from {provider.ProviderName}...");
                            var (normalizedEvents, validCount, invalidCount) =
                                await normalizationService.NormalizeEventsWithStatsAsync(events, sourceType.Value);

                            _logger.LogInformation($"Normalization complete: {validCount} valid, {invalidCount} invalid");
                            summary.EventsValid += validCount;
                            summary.EventsInvalid += invalidCount;

                            allNormalizedEvents.AddRange(normalizedEvents);
                        }
                        catch (OperationCanceledException)
                        {
                            _logger.LogInformation("Sync operation was cancelled");
                            throw;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Error fetching/processing from {provider.ProviderName}: {ex.Message}");
                            summary.ErrorsEncountered[provider.ProviderName] = ex.Message;
                        }
                    }

                    if (!allNormalizedEvents.Any())
                    {
                        _logger.LogWarning("No events to import after normalization");
                        summary.Message = "No valid events to import";
                        return summary;
                    }

                    // Upsert with duplicate tracking
                    _logger.LogInformation($"Upserting {allNormalizedEvents.Count} normalized events...");
                    var (importedEventIds, duplicateCount) =
                        await eventRepository.UpsertManyWithStatsAsync(allNormalizedEvents);

                    summary.EventsUpserted = importedEventIds.Count;
                    summary.EventsDuplicate = duplicateCount;
                    _logger.LogInformation($"Upsert complete: {importedEventIds.Count} events upserted, {duplicateCount} duplicates detected");

                    // Mark old events from all providers as inactive
                    foreach (var provider in summary.ProvidersProcessed)
                    {
                        var sourceType = GetEventSourceType(provider);
                        if (sourceType.HasValue)
                        {
                            var inactiveCount = await eventRepository.MarkExternalEventsAsInactiveAsync(
                                sourceType.Value,
                                DateTime.UtcNow.AddDays(-1));

                            summary.EventsMarkedInactive += inactiveCount;
                            if (inactiveCount > 0)
                                _logger.LogInformation($"Marked {inactiveCount} events as inactive from {provider}");
                        }
                    }

                    summary.Message = $"Sync completed: {summary.EventsUpserted} events processed";
                    _logger.LogInformation($"Event sync completed successfully. Summary: {JsonSerializer.Serialize(summary)}");
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Sync service is shutting down");
                    summary.Message = "Sync cancelled";
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Unexpected error during event sync: {ex.Message}");
                    summary.ErrorsEncountered["Unexpected"] = ex.Message;
                    summary.Message = "Sync failed with unexpected error";
                }
            }

            return summary;
        }

        /// <summary>
        /// Maps provider name to EventSourceType enum value.
        /// </summary>
        private EventSourceType? GetEventSourceType(string providerName)
        {
            return providerName.ToLowerInvariant() switch
            {
                "seatgeek" => EventSourceType.SeatGeek,
                "ticketmaster" => EventSourceType.Ticketmaster,
                _ => null
            };
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("EventSyncService stopping");
            _timer?.Change(Timeout.Infinite, 0);
            _timer?.Dispose();
            await base.StopAsync(cancellationToken);
        }

        public override void Dispose()
        {
            _timer?.Dispose();
            base.Dispose();
        }
    }
}
