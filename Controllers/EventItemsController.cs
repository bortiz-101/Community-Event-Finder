using Microsoft.AspNetCore.Mvc;
using Community_Event_Finder.Models;
using System.Text;
using Community_Event_Finder.Data;
using Community_Event_Finder.Data.ExternalProviders;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;

namespace Community_Event_Finder.Controllers
{
    [ApiController]
    [Route("api/events")]
    public class EventItemsController : ControllerBase
    {
        private readonly IEventRepository _repo;
        private readonly IExternalEventProviderFactory _providerFactory;
        private readonly INormalizationService _normalizationService;
        private readonly IEventValidator _validator;
        private readonly ILogger<EventItemsController> _logger;
        private readonly ExternalProvidersSettings _settings;

        public EventItemsController(
            IEventRepository repo,
            IExternalEventProviderFactory providerFactory,
            INormalizationService normalizationService,
            IEventValidator validator,
            ILogger<EventItemsController> logger,
            IOptions<ExternalProvidersSettings> settings)
        {
            _repo = repo;
            _providerFactory = providerFactory;
            _normalizationService = normalizationService;
            _validator = validator;
            _logger = logger;
            _settings = settings.Value;
        }

        // ================= GET ALL / BY MONTH =================
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? month)
        {
            try
            {
                _logger.LogInformation("Fetching all events for the current month.");
                // If month parameter is provided, parse it and get events for that month
                if (!string.IsNullOrWhiteSpace(month))
                {
                    // Expected format: YYYY-MM
                    if (!System.Text.RegularExpressions.Regex.IsMatch(month, @"^\d{4}-\d{2}$"))
                        return BadRequest("Month parameter must be in format YYYY-MM (e.g., 2026-03).");

                    var parts = month.Split('-');
                    if (!int.TryParse(parts[0], out var year) || !int.TryParse(parts[1], out var monthNum))
                        return BadRequest("Invalid year or month value.");

                    var events = await _repo.GetEventsByMonthAsync(year, monthNum);

                    _logger.LogInformation("Fetched {EventCount} events for the current month.", events.Count);

                    return Ok(events);
                }

                // Default: return events for current month (+ 12 months from today)
                return Ok(await _repo.GetEventsForCurrentMonthAsync());
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // ================= GET BY ID =================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                    return BadRequest("Event ID cannot be empty.");

                var eventDto = await _repo.GetEventByIdAsync(id);

                if (eventDto == null)
                    return NotFound(new { error = $"Event with ID '{id}' not found." });

                return Ok(eventDto);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return StatusCode(500, "Error retrieving event.");
            }
        }

        // ================= SYNC EXTERNAL PROVIDERS =================
        [HttpPost("sync")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SyncExternalEvents()
        {
            var summary = new EventSyncSummary();

            try
            {
                _logger.LogInformation("Starting external event sync...");

                var providers = _providerFactory.GetEnabledProviders().ToList();

                if (!providers.Any())
                {
                    _logger.LogWarning("No external providers enabled");
                    summary.Message = "No providers enabled";
                    return Ok(summary);
                }

                var allNormalizedEvents = new List<EventItem>();
                var successfulProviders = new List<string>(); // Track only providers that successfully fetched events

                // Fetch and process events from all enabled providers
                foreach (var provider in providers)
                {
                    try
                    {
                        _logger.LogInformation($"Fetching events from {provider.ProviderName}...");

                        var events = await provider.GetEventsAsync(
                            latitude: _settings.SearchLatitude,
                            longitude: _settings.SearchLongitude,
                            radiusMiles: _settings.SearchRadiusMiles);

                        _logger.LogInformation($"Retrieved {events.Count} events from {provider.ProviderName}");
                        summary.EventsFetched += events.Count;
                        summary.ProvidersProcessed.Add(provider.ProviderName);

                        if (!events.Any())
                        {
                            _logger.LogWarning($"No events returned from {provider.ProviderName}");
                            continue;
                        }

                        successfulProviders.Add(provider.ProviderName);

                        // Determine the event source type
                        var sourceType = GetEventSourceType(provider.ProviderName);
                        if (sourceType == null)
                        {
                            _logger.LogWarning($"Unknown provider name: {provider.ProviderName}, skipping");
                            summary.ErrorsEncountered[$"{provider.ProviderName}_Unknown"] = "Unknown provider name";
                            continue;
                        }

                        // Normalize and track valid/invalid counts
                        _logger.LogInformation($"Normalizing {events.Count} events from {provider.ProviderName}...");
                        var (normalizedEvents, validCount, invalidCount) =
                            await _normalizationService.NormalizeEventsWithStatsAsync(events, sourceType.Value);

                        _logger.LogInformation($"Normalization complete: {validCount} valid, {invalidCount} invalid");
                        summary.EventsValid += validCount;
                        summary.EventsInvalid += invalidCount;

                        allNormalizedEvents.AddRange(normalizedEvents);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error fetching/processing from {provider.ProviderName}: {ex.Message}");
                        summary.ErrorsEncountered[provider.ProviderName] = ex.Message;
                    }
                }

                if (!allNormalizedEvents.Any())
                {
                    _logger.LogWarning("No events to import after normalization");
                    summary.Message = "No valid events to import";
                    return Ok(summary);
                }

                // Upsert with duplicate tracking
                _logger.LogInformation($"Upserting {allNormalizedEvents.Count} normalized events...");
                var (importedEventIds, duplicateCount) =
                    await _repo.UpsertManyWithStatsAsync(allNormalizedEvents);

                summary.EventsUpserted = importedEventIds.Count;
                summary.EventsDuplicate = duplicateCount;
                _logger.LogInformation($"Upsert complete: {importedEventIds.Count} events upserted, {duplicateCount} duplicates detected");

                // Mark old events from providers that were successfully synced as inactive
                // Only mark as inactive if the provider actually returned data in this sync
                foreach (var providerName in successfulProviders)
                {
                    var sourceType = GetEventSourceType(providerName);
                    if (sourceType.HasValue)
                    {
                        var inactiveCount = await _repo.MarkExternalEventsAsInactiveAsync(
                            sourceType.Value,
                            DateTime.UtcNow.AddDays(-1));

                        summary.EventsMarkedInactive += inactiveCount;
                        if (inactiveCount > 0)
                            _logger.LogInformation($"Marked {inactiveCount} events as inactive from {providerName}");
                    }
                }

                summary.Message = $"Sync completed: {summary.EventsUpserted} events processed from {summary.ProvidersProcessed.Count} providers";
                _logger.LogInformation($"Event sync completed successfully");
                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during external event sync: {ex.Message}");
                summary.ErrorsEncountered["Unexpected"] = ex.Message;
                summary.Message = "Sync failed with error";
                return StatusCode(500, summary);
            }
        }

        /// <summary>
        /// Maps provider name to EventSourceType enum value.
        /// </summary>
        private EventSourceType? GetEventSourceType(string providerName)
        {
            return providerName.ToLowerInvariant() switch
            {
                "seatgeek" => EventSourceType.SeatGeek,
                "ticketmaster" => EventSourceType.Ticketmaster,
                _ => null
            };
        }

        // ================= ADD EVENT =================
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Add([FromBody] AddEventDto dto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Add event request failed model validation.");
                return BadRequest(ModelState);
            }

            // Use centralized validator for business logic validation
            var validationResult = _validator.ValidateAddEventDto(dto);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Add event business validation failed. Errors: {Errors}", validationResult.GetErrorsAsString());
                return BadRequest(new { errors = validationResult.Errors });
            }

            var start = dto.StartTime ?? DateTime.Now;
            var end = dto.EndTime ?? start.AddHours(1);

            _logger.LogInformation("Attempting to add event with title: {Title}", dto.Title);

            try
            {
                var id = await _repo.InsertEventAsync(
                    dto.Title,
                    dto.Category,
                    start,
                    end,
                    dto.VenueName,
                    dto.Address,
                    dto.City,
                    dto.State,
                    dto.Zip,
                    dto.Description,
                    dto.Url);

                _logger.LogInformation("Event added successfully with ID: {Id}", id);

                return Ok(new { id });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Duplicate or invalid event add attempt for title: {Title}", dto.Title);

                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while adding event with title: {Title}", dto.Title);

                return StatusCode(500, "Error: Duplicated event with same name, time and added by same user.");
            }
        }

        // ================= FAVORITES =================
        [HttpGet("favorites")]
        [Authorize]
        public async Task<IActionResult> Favorites()
        {
            return Ok(await _repo.GetFavoriteEventsForCurrentMonthAsync());
        }

        // ================= TOGGLE FAVORITE =================
        [HttpPut("favorite/{id}")]
        [Authorize]
        public async Task<IActionResult> ToggleFavorite(string id)
        {
            await _repo.ToggleFavoriteAsync(id);
            return Ok();
        }

        // ================= DELETE =================
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _repo.DeleteEventAsync(id);
                return Ok();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return StatusCode(500, "Delete failed.");
            }
        }

        // ================= EXPORT ICS =================
        [HttpGet("ics")]
        [Authorize]
        public async Task<IActionResult> ExportIcs()
        {
            var events = await _repo.GetFavoriteEventsForCurrentMonthAsync();

            var sb = new StringBuilder();
            sb.AppendLine("BEGIN:VCALENDAR");
            sb.AppendLine("VERSION:2.0");

            foreach (var e in events)
            {
                sb.AppendLine("BEGIN:VEVENT");
                sb.AppendLine($"SUMMARY:{e.Title}");
                sb.AppendLine($"DTSTART:{e.StartTime:yyyyMMddTHHmmss}");
                sb.AppendLine($"DTEND:{e.EndTime:yyyyMMddTHHmmss}");
                sb.AppendLine("END:VEVENT");
            }

            sb.AppendLine("END:VCALENDAR");

            return File(
                Encoding.UTF8.GetBytes(sb.ToString()),
                "text/calendar",
                "events.ics");
        }
    }
}