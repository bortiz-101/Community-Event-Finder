namespace Community_Event_Finder.Models
{
    // Configuration settings for external event providers
    public class ExternalProvidersSettings
    {
        public const string SectionName = "ExternalProviders";

        // Ticketmaster provider settings
        public TicketmasterSettings Ticketmaster { get; set; } = new();

        // SeatGeek provider settings
        public SeatGeekSettings SeatGeek { get; set; } = new();

        // Global refresh interval in minutes
        public int RefreshIntervalMinutes { get; set; } = 60;

        // Location settings for event searches
        // Use null to search globally, or set to specific coordinates to limit searches
        public decimal? SearchLatitude { get; set; }
        public decimal? SearchLongitude { get; set; }
        public double? SearchRadiusMiles { get; set; }
    }

    // Ticketmaster API configuration
    // Reference: https://app.ticketmaster.com/discovery/v2/events.json
    public class TicketmasterSettings
    {
        // Events endpoint URL (e.g., https://app.ticketmaster.com/discovery/v2/events.json)
        public string? EventsUrl { get; set; }

        // API key for authentication
        public string? ApiKey { get; set; }

        // Whether this provider is enabled
        public bool Enabled { get; set; }

        // Validates that all required settings are configured
        public List<string> Validate()
        {
            var errors = new List<string>();

            if (Enabled)
            {
                if (string.IsNullOrWhiteSpace(EventsUrl))
                    errors.Add("Ticketmaster EventsUrl is required when enabled");
                if (string.IsNullOrWhiteSpace(ApiKey))
                    errors.Add("Ticketmaster ApiKey is required when enabled");
            }

            return errors;
        }
    }

    // SeatGeek API configuration
    // Reference: https://platform.seatgeek.com/
    public class SeatGeekSettings
    {
        // Events endpoint URL (e.g., https://api.seatgeek.com/2/events)
        public string? EventsUrl { get; set; }

        // Client ID for authentication
        public string? ClientId { get; set; }

        // Client Secret for authentication (optional but recommended)
        public string? ClientSecret { get; set; }

        // Whether this provider is enabled
        public bool Enabled { get; set; }

        // Validates that all required settings are configured
        public List<string> Validate()
        {
            var errors = new List<string>();

            if (Enabled)
            {
                if (string.IsNullOrWhiteSpace(EventsUrl))
                    errors.Add("SeatGeek EventsUrl is required when enabled");
                if (string.IsNullOrWhiteSpace(ClientId))
                    errors.Add("SeatGeek ClientId is required when enabled");
            }

            return errors;
        }
    }
}
