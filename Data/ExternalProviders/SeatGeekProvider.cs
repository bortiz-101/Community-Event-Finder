using Community_Event_Finder.Models;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Community_Event_Finder.Data.ExternalProviders
{
    // SeatGeek event provider implementation
    // API Documentation: https://platform.seatgeek.com/
    public class SeatGeekProvider : IExternalEventProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly SeatGeekSettings _settings;
        private readonly ILogger<SeatGeekProvider> _logger;

        public string ProviderName => "SeatGeek";

        public SeatGeekProvider(
            IHttpClientFactory httpClientFactory,
            IOptions<ExternalProvidersSettings> options,
            ILogger<SeatGeekProvider> logger)
        {
            _httpClientFactory = httpClientFactory;
            _settings = options.Value.SeatGeek;
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
                    _logger.LogInformation("SeatGeek provider is disabled");
                    return events;
                }

                var client = _httpClientFactory.CreateClient();
                var url = _settings.EventsUrl;

                // Build query parameters
                var queryParams = BuildQueryParameters(latitude, longitude, radiusMiles, fromDate, toDate);
                queryParams.Add($"client_id={Uri.EscapeDataString(_settings.ClientId ?? "")}");

                url += "?" + string.Join("&", queryParams);

                var response = await client.GetAsync(url, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(cancellationToken);
                    events = ParseSeatGeekResponse(content);
                    _logger.LogInformation($"Retrieved {events.Count} events from SeatGeek");
                }
                else
                {
                    _logger.LogError($"SeatGeek API error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching events from SeatGeek: {ex.Message}");
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
                // SeatGeek uses geoip parameter for lat,lon
                queryParams.Add($"lat={latitude}");
                queryParams.Add($"lon={longitude}");

                if (radiusMiles.HasValue)
                {
                    queryParams.Add($"range={radiusMiles}mi");
                }
            }

            if (fromDate.HasValue)
            {
                queryParams.Add($"datetime_utc.gte={fromDate:yyyy-MM-ddTHH:mm:ssZ}");
            }

            if (toDate.HasValue)
            {
                queryParams.Add($"datetime_utc.lte={toDate:yyyy-MM-ddTHH:mm:ssZ}");
            }

            return queryParams;
        }

        private List<ExternalEventDto> ParseSeatGeekResponse(string json)
        {
            var events = new List<ExternalEventDto>();

            try
            {
                using var doc = JsonDocument.Parse(json);
                var events_array = doc.RootElement.GetProperty("events");

                foreach (var eventElement in events_array.EnumerateArray())
                {
                    var evt = new ExternalEventDto
                    {
                        ExternalId = eventElement.GetProperty("id").GetString(),
                        Title = eventElement.GetProperty("title").GetString(),
                        Url = eventElement.TryGetProperty("url", out var url) ? url.GetString() : null,
                        Source = "SeatGeek"
                    };

                    // Parse dates
                    if (eventElement.TryGetProperty("datetime_utc", out var dateTime))
                    {
                        evt.StartTime = DateTime.Parse(dateTime.GetString() ?? "");
                    }

                    // Parse venue and location
                    if (eventElement.TryGetProperty("venue", out var venue))
                    {
                        evt.VenueName = venue.TryGetProperty("name", out var venueName) ? venueName.GetString() : null;
                        evt.Address = venue.TryGetProperty("address", out var address) ? address.GetString() : null;
                        evt.City = venue.TryGetProperty("city", out var city) ? city.GetString() : null;
                        evt.State = venue.TryGetProperty("state", out var state) ? state.GetString() : null;
                        evt.Zip = venue.TryGetProperty("postal_code", out var zip) ? zip.GetString() : null;

                        if (venue.TryGetProperty("latitude", out var lat) && venue.TryGetProperty("longitude", out var lon))
                        {
                            evt.Latitude = decimal.Parse(lat.GetString() ?? "0");
                            evt.Longitude = decimal.Parse(lon.GetString() ?? "0");
                        }
                    }

                    // Parse category from type
                    if (eventElement.TryGetProperty("type", out var type))
                    {
                        evt.Category = type.GetString();
                    }

                    events.Add(evt);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error parsing SeatGeek response: {ex.Message}");
            }

            return events;
        }
    }
}
