namespace Community_Event_Finder.Models
{
    // Configuration settings for external event providers
    public class ExternalProvidersSettings
    {
        public const string SectionName = "ExternalProviders";

        // PredictHQ provider settings
        public PredictHQSettings PredictHQ { get; set; } = new();

        // Ticketmaster provider settings
        public TicketmasterSettings Ticketmaster { get; set; } = new();

        // SeatGeek provider settings
        public SeatGeekSettings SeatGeek { get; set; } = new();

        // Global refresh interval in minutes
        public int RefreshIntervalMinutes { get; set; } = 60;
    }

    // PredictHQ API configuration
    // Reference: https://api.predicthq.com/v1/events/
    public class PredictHQSettings
    {
        // Events endpoint URL (e.g., https://api.predicthq.com/v1/events)
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
                    errors.Add("PredictHQ EventsUrl is required when enabled");
                if (string.IsNullOrWhiteSpace(ApiKey))
                    errors.Add("PredictHQ ApiKey is required when enabled");
            }

            return errors;
        }
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
