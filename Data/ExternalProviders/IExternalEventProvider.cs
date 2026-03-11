namespace Community_Event_Finder.Data.ExternalProviders
{
    // Interface for external event provider services
    public interface IExternalEventProvider
    {
        // The name of the provider
        string ProviderName { get; }

        // Gets events from the external provider
        Task<List<ExternalEventDto>> GetEventsAsync(
            decimal? latitude = null,
            decimal? longitude = null,
            double? radiusMiles = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            CancellationToken cancellationToken = default);
    }
}
