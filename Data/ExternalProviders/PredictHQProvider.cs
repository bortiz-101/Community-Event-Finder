using Community_Event_Finder.Models;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Community_Event_Finder.Data.ExternalProviders
{
    // PredictHQ event provider implementation
    // API Documentation: https://docs.predicthq.com/
    public class PredictHQProvider : IExternalEventProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly PredictHQSettings _settings;
        private readonly ILogger<PredictHQProvider> _logger;

        public string ProviderName => "PredictHQ";

        public PredictHQProvider(
            IHttpClientFactory httpClientFactory,
            IOptions<ExternalProvidersSettings> options,
            ILogger<PredictHQProvider> logger)
        {
            _httpClientFactory = httpClientFactory;
            _settings = options.Value.PredictHQ;
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
                    _logger.LogInformation("PredictHQ provider is disabled");
                    return events;
                }

                var client = _httpClientFactory.CreateClient();
                var url = _settings.EventsUrl;

                // Build query parameters
                var queryParams = BuildQueryParameters(latitude, longitude, radiusMiles, fromDate, toDate);
                if (queryParams.Count > 0)
                {
                    url += "?" + string.Join("&", queryParams);
                }

                // Setup auth header
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _settings.ApiKey);

                var response = await client.GetAsync(url, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(cancellationToken);
                    events = ParsePredictHQResponse(content);
                    _logger.LogInformation($"Retrieved {events.Count} events from PredictHQ");
                }
                else
                {
                    _logger.LogError($"PredictHQ API error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching events from PredictHQ: {ex.Message}");
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
                queryParams.Add($"location.latitude={latitude}");
                queryParams.Add($"location.longitude={longitude}");

                if (radiusMiles.HasValue)
                {
                    // Convert miles to km (PredictHQ uses km)
                    var km = radiusMiles.Value * 1.60934;
                    queryParams.Add($"location.radius_km={Math.Round(km, 2)}");
                }
            }

            if (fromDate.HasValue)
            {
                queryParams.Add($"active.gte={fromDate:yyyy-MM-ddTHH:mm:ssZ}");
            }

            if (toDate.HasValue)
            {
                queryParams.Add($"active.lte={toDate:yyyy-MM-ddTHH:mm:ssZ}");
            }

            return queryParams;
        }

        private List<ExternalEventDto> ParsePredictHQResponse(string json)
        {
            var events = new List<ExternalEventDto>();

            try
            {
                using var doc = JsonDocument.Parse(json);
                var results = doc.RootElement.GetProperty("results");

                foreach (var eventElement in results.EnumerateArray())
                {
                    var evt = new ExternalEventDto
                    {
                        ExternalId = eventElement.GetProperty("id").GetString(),
                        Title = eventElement.GetProperty("title").GetString(),
                        Description = eventElement.TryGetProperty("description", out var desc) ? desc.GetString() : null,
                        Category = eventElement.TryGetProperty("category", out var cat) ? cat.GetString() : null,
                        Url = eventElement.TryGetProperty("url", out var url) ? url.GetString() : null,
                        Source = "PredictHQ"
                    };

                    // Parse dates
                    if (eventElement.TryGetProperty("start", out var startProp))
                    {
                        evt.StartTime = DateTime.Parse(startProp.GetString() ?? "");
                    }

                    if (eventElement.TryGetProperty("end", out var endProp))
                    {
                        evt.EndTime = DateTime.Parse(endProp.GetString() ?? "");
                    }

                    // Parse location
                    if (eventElement.TryGetProperty("location", out var locProp))
                    {
                        if (locProp.TryGetProperty("lat", out var lat) && locProp.TryGetProperty("lon", out var lon))
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
                _logger.LogError($"Error parsing PredictHQ response: {ex.Message}");
            }

            return events;
        }
    }
}
