using Microsoft.AspNetCore.Mvc;
using Community_Event_Finder.Models;
using System.Text;
using Community_Event_Finder.Data;

namespace Community_Event_Finder.Controllers
{
    [ApiController]
    [Route("api/events")]
    public class EventItemsController : ControllerBase
    {
        private readonly IEventRepository _repo;

        public EventItemsController(IEventRepository repo)
        {
            _repo = repo;
        }

        // ================= GET ALL / BY MONTH =================
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? month)
        {
            try
            {
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

        // ================= ADD EVENT =================
        [HttpPost]
        public async Task<IActionResult> Add([FromBody] AddEventDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var start = dto.StartTime ?? DateTime.Now;
            var end = dto.EndTime ?? start.AddHours(1);

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

                return Ok(new { id });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return StatusCode(500, "Error: Duplicated event with same name, time and added by same user.");
            }
        }

        // ================= FAVORITES =================
        [HttpGet("favorites")]
        public async Task<IActionResult> Favorites()
        {
            return Ok(await _repo.GetFavoriteEventsForCurrentMonthAsync());
        }

        // ================= TOGGLE FAVORITE =================
        [HttpPut("favorite/{id}")]
        public async Task<IActionResult> ToggleFavorite(string id)
        {
            await _repo.ToggleFavoriteAsync(id);
            return Ok();
        }

        // ================= DELETE =================
        [HttpDelete("{id}")]
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