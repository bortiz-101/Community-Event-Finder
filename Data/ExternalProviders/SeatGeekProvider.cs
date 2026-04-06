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
        private const int PageSize = 100;
        private const int MaxPages = 100;

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
                int page = 1;
                int pageCount = 0;

                while (pageCount < MaxPages)
                {
                    var url = _settings.EventsUrl;
                    var queryParams = BuildQueryParameters(latitude, longitude, radiusMiles, fromDate, toDate);
                    queryParams.Add($"client_id={Uri.EscapeDataString(_settings.ClientId ?? "")}");
                    
                    if (!string.IsNullOrWhiteSpace(_settings.ClientSecret))
                    {
                        queryParams.Add($"client_secret={Uri.EscapeDataString(_settings.ClientSecret)}");
                    }
                    
                    queryParams.Add($"page={page}");
                    queryParams.Add($"per_page={PageSize}");

                    url += "?" + string.Join("&", queryParams);

                    var response = await client.GetAsync(url, cancellationToken);

                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync(cancellationToken);
                        var pageEvents = ParseSeatGeekResponse(content);

                        if (pageEvents.Count == 0)
                        {
                            break; // No more events on this page
                        }

                        events.AddRange(pageEvents);
                        page++;
                        pageCount++;
                    }
                    else
                    {
                        _logger.LogError($"SeatGeek API error on page {page}: {response.StatusCode} - {response.ReasonPhrase}");
                        break; // Stop pagination on error
                    }
                }

                _logger.LogInformation($"Retrieved {events.Count} events from SeatGeek ({pageCount} pages)");
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
                
                // SeatGeek API returns an object with "events" property and "meta" property
                var root = doc.RootElement;
                
                if (root.ValueKind != JsonValueKind.Object)
                {
                    _logger.LogWarning("SeatGeek response is not an object");
                    return events;
                }

                if (!root.TryGetProperty("events", out var events_array))
                {
                    _logger.LogWarning("SeatGeek response does not contain 'events' property");
                    return events;
                }

                if (events_array.ValueKind != JsonValueKind.Array)
                {
                    _logger.LogWarning("SeatGeek 'events' property is not an array");
                    return events;
                }

                foreach (var eventElement in events_array.EnumerateArray())
                {
                    var evt = new ExternalEventDto
                    {
                        // ID is a number in the JSON, convert to string
                        ExternalId = eventElement.TryGetProperty("id", out var id) ? id.GetInt32().ToString() : null,
                        Title = eventElement.TryGetProperty("title", out var title) ? title.GetString() : null,
                        Url = eventElement.TryGetProperty("url", out var url) ? url.GetString() : null,
                        Source = "SeatGeek"
                    };

                    // Parse start date/time (using datetime_local for user-facing display)
                    if (eventElement.TryGetProperty("datetime_local", out var dateTime))
                    {
                        try
                        {
                            evt.StartTime = DateTime.Parse(dateTime.GetString() ?? "");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"Failed to parse SeatGeek datetime_local: {ex.Message}");
                        }
                    }

                    // Parse end date/time (using end_datetime_local if available)
                    if (eventElement.TryGetProperty("enddatetime_local", out var endDateTime))
                    {
                        try
                        {
                            var parsedEndTime = DateTime.Parse(endDateTime.GetString() ?? "");
                            // Only use end time if it's after start time
                            if (parsedEndTime > evt.StartTime)
                            {
                                evt.EndTime = parsedEndTime;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"Failed to parse SeatGeek enddatetime_local: {ex.Message}");
                        }
                    }

                    // Parse venue and location
                    if (eventElement.TryGetProperty("venue", out var venue))
                    {
                        evt.VenueName = venue.TryGetProperty("name", out var venueName) ? venueName.GetString() : null;
                        evt.Address = venue.TryGetProperty("address", out var address) ? address.GetString() : null;
                        evt.City = venue.TryGetProperty("city", out var city) ? city.GetString() : null;
                        evt.State = venue.TryGetProperty("state", out var state) ? state.GetString() : null;
                        evt.Zip = venue.TryGetProperty("postal_code", out var zip) ? zip.GetString() : null;

                        // Try to extract latitude and longitude from venue
                        // SeatGeek may store them at different levels
                        if (venue.TryGetProperty("location", out var location) && location.ValueKind == JsonValueKind.Object)
                        {
                            if (location.TryGetProperty("lat", out var lat))
                            {
                                evt.Latitude = lat.ValueKind == JsonValueKind.Number 
                                    ? (decimal)lat.GetDouble() 
                                    : null;
                            }
                            
                            if (location.TryGetProperty("lon", out var lon))
                            {
                                evt.Longitude = lon.ValueKind == JsonValueKind.Number 
                                    ? (decimal)lon.GetDouble() 
                                    : null;
                            }
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
