using Community_Event_Finder.Models;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Community_Event_Finder.Data.ExternalProviders
{
    // Ticketmaster event provider implementation
    // API Documentation: https://developer.ticketmaster.com/
    public class TicketmasterProvider : IExternalEventProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly TicketmasterSettings _settings;
        private readonly ILogger<TicketmasterProvider> _logger;

        public string ProviderName => "Ticketmaster";

        public TicketmasterProvider(
            IHttpClientFactory httpClientFactory,
            IOptions<ExternalProvidersSettings> options,
            ILogger<TicketmasterProvider> logger)
        {
            _httpClientFactory = httpClientFactory;
            _settings = options.Value.Ticketmaster;
            _logger = logger;
        }

        public async Task<List<ExternalEventDto>> GetEventsAsync(
            decimal? latitude = null,
            decimal? longitude = null,
            double? radiusMiles = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            CancellationToken cancellationToken = default)
        {
            var events = new List<ExternalEventDto>();

            try
            {
                if (!_settings.Enabled)
                {
                    _logger.LogInformation("Ticketmaster provider is disabled");
                    return events;
                }

                var client = _httpClientFactory.CreateClient();
                var url = _settings.EventsUrl;

                // Build query parameters
                var queryParams = BuildQueryParameters(latitude, longitude, radiusMiles, fromDate, toDate);
                queryParams.Add($"apikey={Uri.EscapeDataString(_settings.ApiKey ?? "")}");

                url += "?" + string.Join("&", queryParams);

                var response = await client.GetAsync(url, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(cancellationToken);
                    events = ParseTicketmasterResponse(content);
                    _logger.LogInformation($"Retrieved {events.Count} events from Ticketmaster");
                }
                else
                {
                    _logger.LogError($"Ticketmaster API error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching events from Ticketmaster: {ex.Message}");
            }

            return events;
        }

        private List<string> BuildQueryParameters(
            decimal? latitude, decimal? longitude, double? radiusMiles,
            DateTime? fromDate, DateTime? toDate)
        {
            var queryParams = new List<string>();

            if (latitude.HasValue && longitude.HasValue)
            {
                queryParams.Add($"latlong={latitude},{longitude}");

                if (radiusMiles.HasValue)
                {
                    queryParams.Add($"radius={radiusMiles}");
                    queryParams.Add("unit=miles");
                }
            }

            if (fromDate.HasValue)
            {
                queryParams.Add($"startDateTime={fromDate:yyyy-MM-ddTHH:mm:ssZ}");
            }

            if (toDate.HasValue)
            {
                queryParams.Add($"endDateTime={toDate:yyyy-MM-ddTHH:mm:ssZ}");
            }

            return queryParams;
        }

        private List<ExternalEventDto> ParseTicketmasterResponse(string json)
        {
            var events = new List<ExternalEventDto>();

            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (!root.TryGetProperty("_embedded", out var embedded))
                {
                    return events;
                }

                if (!embedded.TryGetProperty("events", out var eventsArray))
                {
                    return events;
                }

                foreach (var eventElement in eventsArray.EnumerateArray())
                {
                    var evt = new ExternalEventDto
                    {
                        ExternalId = eventElement.GetProperty("id").GetString(),
                        Title = eventElement.GetProperty("name").GetString(),
                        Description = eventElement.TryGetProperty("description", out var desc) ? desc.GetString() : null,
                        Url = eventElement.TryGetProperty("url", out var url) ? url.GetString() : null,
                        Source = "Ticketmaster"
                    };

                    // Parse dates
                    if (eventElement.TryGetProperty("dates", out var dates) &&
                        dates.TryGetProperty("start", out var start) &&
                        start.TryGetProperty("dateTime", out var dateTime))
                    {
                        evt.StartTime = DateTime.Parse(dateTime.GetString() ?? "");
                    }

                    // Parse location/venue
                    if (eventElement.TryGetProperty("_embedded", out var eventEmbedded) &&
                        eventEmbedded.TryGetProperty("venues", out var venues))
                    {
                        var venue = venues[0];
                        evt.VenueName = venue.TryGetProperty("name", out var venueName) ? venueName.GetString() : null;

                        if (venue.TryGetProperty("address", out var address) &&
                            address.TryGetProperty("line1", out var line1))
                        {
                            evt.Address = line1.GetString();
                        }

                        evt.City = venue.TryGetProperty("city", out var city) ? city.GetProperty("name").GetString() : null;
                        evt.State = venue.TryGetProperty("state", out var state) ? state.GetProperty("stateCode").GetString() : null;
                        evt.Zip = venue.TryGetProperty("postalCode", out var zip) ? zip.GetString() : null;

                        if (venue.TryGetProperty("location", out var location) &&
                            location.TryGetProperty("latitude", out var lat) &&
                            location.TryGetProperty("longitude", out var lon))
                        {
                            evt.Latitude = decimal.Parse(lat.GetString() ?? "0");
                            evt.Longitude = decimal.Parse(lon.GetString() ?? "0");
                        }
                    }

                    events.Add(evt);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error parsing Ticketmaster response: {ex.Message}");
            }

            return events;
        }
    }
}
