using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Community_Event_Finder.Models;
using Community_Event_Finder.Data.ExternalProviders;
using Microsoft.EntityFrameworkCore;

namespace Community_Event_Finder.Data
{
    /// <summary>
    /// Service responsible for normalizing external event data into domain models.
    /// Handles mapping, validation, deduplication, and consolidation of locations and categories.
    /// </summary>
    public interface INormalizationService
    {
        /// <summary>
        /// Maps and normalizes an external event DTO to a domain EventItem.
        /// Validates required fields and handles deduplication.
        /// </summary>
        Task<EventItem?> NormalizeEventAsync(ExternalEventDto externalEvent, EventSourceType sourceType);

        /// <summary>
        /// Batch normalize multiple external events.
        /// </summary>
        Task<List<EventItem>> NormalizeEventsAsync(List<ExternalEventDto> externalEvents, EventSourceType sourceType);

        /// <summary>
        /// Batch normalize events and track validation statistics.
        /// Returns both normalized events and counts of valid/invalid events.
        /// </summary>
        Task<(List<EventItem> NormalizedEvents, int ValidCount, int InvalidCount)> NormalizeEventsWithStatsAsync(
            List<ExternalEventDto> externalEvents, 
            EventSourceType sourceType);
    }

    public class NormalizationService : INormalizationService
    {
        private readonly ApplicationDbContext _context;

        public NormalizationService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Maps and normalizes an external event DTO to a domain EventItem.
        /// </summary>
        public async Task<EventItem?> NormalizeEventAsync(ExternalEventDto externalEvent, EventSourceType sourceType)
        {
            if (externalEvent == null)
                return null;

            // Validate required fields
            if (string.IsNullOrWhiteSpace(externalEvent.Title))
                return null; // Title is required

            if (externalEvent.StartTime == default(DateTime))
                return null; // StartTime is required

            // Set default end time if not provided (e.g., 2 hours after start)
            var endTime = externalEvent.EndTime ?? externalEvent.StartTime.AddHours(2);

            // Validate EndTime is after StartTime
            if (endTime <= externalEvent.StartTime)
                endTime = externalEvent.StartTime.AddHours(2);

            // Get or create Location
            int? locationId = null;
            if (!string.IsNullOrWhiteSpace(externalEvent.VenueName))
            {
                var location = await FindOrCreateLocationAsync(externalEvent);
                if (location != null)
                    locationId = location.LocationId;
            }

            // Get or create Category
            int? categoryId = null;
            if (!string.IsNullOrWhiteSpace(externalEvent.Category))
            {
                var category = await FindOrCreateCategoryAsync(externalEvent.Category);
                if (category != null)
                    categoryId = category.CategoryId;
            }

            // Create normalized EventItem
            var eventItem = new EventItem
            {
                EventId = Guid.NewGuid().ToString(),
                Source = sourceType.ToString(),
                Title = externalEvent.Title.Trim(),
                Description = externalEvent.Description,
                StartTime = externalEvent.StartTime,
                EndTime = endTime,
                Url = externalEvent.Url,
                LocationId = locationId,
                CategoryId = categoryId,
                ExternalEventId = externalEvent.ExternalId,
                ExternalEventSourceType = sourceType,
                CreatedAt = DateTime.UtcNow
            };

            return eventItem;
        }

        /// <summary>
        /// Batch normalize multiple external events.
        /// </summary>
        public async Task<List<EventItem>> NormalizeEventsAsync(List<ExternalEventDto> externalEvents, EventSourceType sourceType)
        {
            var normalizedEvents = new List<EventItem>();

            foreach (var externalEvent in externalEvents)
            {
                var normalizedEvent = await NormalizeEventAsync(externalEvent, sourceType);
                if (normalizedEvent != null)
                    normalizedEvents.Add(normalizedEvent);
            }

            return normalizedEvents;
        }

        /// <summary>
        /// Batch normalize events and track validation statistics.
        /// </summary>
        public async Task<(List<EventItem> NormalizedEvents, int ValidCount, int InvalidCount)> NormalizeEventsWithStatsAsync(
            List<ExternalEventDto> externalEvents,
            EventSourceType sourceType)
        {
            var normalizedEvents = new List<EventItem>();
            int validCount = 0;
            int invalidCount = 0;

            foreach (var externalEvent in externalEvents)
            {
                var normalizedEvent = await NormalizeEventAsync(externalEvent, sourceType);
                if (normalizedEvent != null)
                {
                    normalizedEvents.Add(normalizedEvent);
                    validCount++;
                }
                else
                {
                    invalidCount++;
                }
            }

            return (normalizedEvents, validCount, invalidCount);
        }

        /// <summary>
        /// Finds an existing location by normalized venue name and address, or creates a new one.
        /// This consolidates venues across different providers.
        /// </summary>
        private async Task<Location?> FindOrCreateLocationAsync(ExternalEventDto externalEvent)
        {
            if (string.IsNullOrWhiteSpace(externalEvent.VenueName))
                return null;

            var normalizedVenueName = externalEvent.VenueName.Trim();
            var normalizedAddress = externalEvent.Address?.Trim() ?? "";

            // Try to find existing location (primary deduplication: venue + address)
            var existingLocation = await _context.Locations
                .FirstOrDefaultAsync(l =>
                    l.VenueName.ToLower() == normalizedVenueName.ToLower() &&
                    (l.Address == null || l.Address.ToLower() == normalizedAddress.ToLower()));

            if (existingLocation != null)
                return existingLocation;

            // Create new location
            var newLocation = new Location
            {
                VenueName = normalizedVenueName,
                Address = externalEvent.Address ?? "",
                City = externalEvent.City ?? "",
                State = externalEvent.State ?? "",
                Zip = externalEvent.Zip ?? "",
                Latitude = externalEvent.Latitude,
                Longitude = externalEvent.Longitude
            };

            _context.Locations.Add(newLocation);
            await _context.SaveChangesAsync();

            return newLocation;
        }

        /// <summary>
        /// Finds an existing category by normalized name, or creates a new one.
        /// This normalizes category names across different providers.
        /// </summary>
        private async Task<Category?> FindOrCreateCategoryAsync(string categoryName)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
                return null;

            var normalizedName = categoryName.Trim();

            // Try to find existing category (case-insensitive match)
            var existingCategory = await _context.Categories
                .FirstOrDefaultAsync(c => c.Name.ToLower() == normalizedName.ToLower());

            if (existingCategory != null)
                return existingCategory;

            // Create new category
            var newCategory = new Category
            {
                Name = normalizedName,
                Description = $"Category: {normalizedName}"
            };

            _context.Categories.Add(newCategory);
            await _context.SaveChangesAsync();

            return newCategory;
        }
    }
}
