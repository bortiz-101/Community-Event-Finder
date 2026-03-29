namespace Community_Event_Finder.Data.ExternalProviders
{
    // Interface for external event provider services
    public interface IExternalEventProvider
    {
        // The name of the provider
        string ProviderName { get; }

        // Gets events from the external provider with location filtering
        Task<List<ExternalEventDto>> GetEventsAsync(
            decimal? latitude = null,
            decimal? longitude = null,
            double? radiusMiles = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            CancellationToken cancellationToken = default);

        // Fetches events for a given date range from the external provider
        // Returns provider-specific DTOs normalized to ExternalEventDto format
        // Default implementation that delegates to GetEventsAsync
        async Task<List<ExternalEventDto>> FetchEventsAsync(
            DateTime start,
            DateTime end,
            CancellationToken cancellationToken = default)
        {
            return await GetEventsAsync(
                latitude: null,
                longitude: null,
                radiusMiles: null,
                fromDate: start,
                toDate: end,
                cancellationToken: cancellationToken);
        }
    }
}
