namespace Community_Event_Finder.Data.ExternalProviders
{
    // Factory for managing external event providers
    public interface IExternalEventProviderFactory
    {
        // Gets all enabled providers
        IEnumerable<IExternalEventProvider> GetEnabledProviders();

        // Gets a specific provider by name
        IExternalEventProvider? GetProvider(string providerName);
    }

    public class ExternalEventProviderFactory : IExternalEventProviderFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ExternalEventProviderFactory> _logger;

        public ExternalEventProviderFactory(IServiceProvider serviceProvider, ILogger<ExternalEventProviderFactory> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public IEnumerable<IExternalEventProvider> GetEnabledProviders()
        {
            var providers = new List<IExternalEventProvider>();

            try
            {
                var predictHQ = _serviceProvider.GetRequiredService<PredictHQProvider>();
                if (predictHQ != null)
                    providers.Add(predictHQ);
            }
            catch { _logger.LogDebug("PredictHQ provider not available"); }

            try
            {
                var ticketmaster = _serviceProvider.GetRequiredService<TicketmasterProvider>();
                if (ticketmaster != null)
                    providers.Add(ticketmaster);
            }
            catch { _logger.LogDebug("Ticketmaster provider not available"); }

            try
            {
                var seatGeek = _serviceProvider.GetRequiredService<SeatGeekProvider>();
                if (seatGeek != null)
                    providers.Add(seatGeek);
            }
            catch { _logger.LogDebug("SeatGeek provider not available"); }

            return providers;
        }

        public IExternalEventProvider? GetProvider(string providerName)
        {
            return GetEnabledProviders().FirstOrDefault(p => 
                p.ProviderName.Equals(providerName, StringComparison.OrdinalIgnoreCase));
        }
    }
}
