using Community_Event_Finder.Data.ExternalProviders;
using Community_Event_Finder.Models;
using Microsoft.Extensions.Options;

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

        private async Task SyncEventsAsync(CancellationToken stoppingToken)
        {
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
                        return;
                    }

                    var allNormalizedEvents = new List<EventItem>();

                    // Fetch and process events from all enabled providers
                    foreach (var provider in providers)
                    {
                        try
                        {
                            _logger.LogInformation($"Fetching events from {provider.ProviderName}...");
                            var events = await provider.GetEventsAsync(
                                latitude: _settings.SearchLatitude,
                                longitude: _settings.SearchLongitude,
                                radiusMiles: _settings.SearchRadiusMiles,
                                cancellationToken: stoppingToken);
                            _logger.LogInformation($"Retrieved {events.Count} events from {provider.ProviderName}");

                            if (!events.Any())
                                continue;

                            // Determine the event source type
                            var sourceType = GetEventSourceType(provider.ProviderName);
                            if (sourceType == null)
                            {
                                _logger.LogWarning($"Unknown provider name: {provider.ProviderName}, skipping");
                                continue;
                            }

                            _logger.LogInformation($"Normalizing {events.Count} events from {provider.ProviderName}...");
                            var normalizedEvents = await normalizationService.NormalizeEventsAsync(events, sourceType.Value);
                            _logger.LogInformation($"Normalized {normalizedEvents.Count} events from {provider.ProviderName}");
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
                        }
                    }

                    if (!allNormalizedEvents.Any())
                    {
                        _logger.LogWarning("No events to import after normalization");
                        return;
                    }

                    _logger.LogInformation($"Upserting {allNormalizedEvents.Count} normalized events...");
                    var importedEventIds = await eventRepository.UpsertManyAsync(allNormalizedEvents);

                    _logger.LogInformation($"Successfully synced {importedEventIds.Count} events from external providers");
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Sync service is shutting down");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Unexpected error during event sync: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Maps provider name to EventSourceType enum value.
        /// </summary>
        private EventSourceType? GetEventSourceType(string providerName)
        {
            return providerName.ToLowerInvariant() switch
            {
                "predicthq" => EventSourceType.PredictHQ,
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
