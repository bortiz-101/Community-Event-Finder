using Community_Event_Finder.Data.ExternalProviders;
using Community_Event_Finder.Models;
using System;
using System.Collections.Generic;

namespace Community_Event_Finder.Tests
{
    /// <summary>
    /// Factory for creating test data for normalization and deduplication tests.
    /// </summary>
    public static class TestDataFactory
    {
    /// <summary>
    /// Creates a test ExternalEventDto from SeatGeek provider.
    /// Validates proper source tracking and field mapping.
    /// </summary>
        public static ExternalEventDto CreateSeatGeekTestEvent()
        {
            return new ExternalEventDto
            {
                ExternalId = "seatgeek-demo-456",
                Title = "International Festival - SeatGeek",
                Description = "Normalized from SeatGeek provider",
                StartTime = new DateTime(2026, 6, 20, 10, 0, 0),
                EndTime = new DateTime(2026, 6, 20, 18, 0, 0),
                VenueName = "Venue B",
                Address = "200 Park Ave",
                City = "New York",
                State = "NY",
                Zip = "10016",
                Category = "Festival",
                Url = "https://seatgeek.example.com/events/456",
                Source = "SeatGeek"
            };
        }

        /// <summary>
        /// Creates a duplicate test scenario where same external event is fetched twice.
        /// First sync: Creates new EventItem
        /// Second sync: Updates existing EventItem (no duplicate insert)
        /// </summary>
        public static List<ExternalEventDto> CreateDuplicateTestScenario()
        {
            var eventData = new ExternalEventDto
            {
                ExternalId = "dup-primary-789",
                Title = "Duplicate Test Event",
                StartTime = new DateTime(2026, 7, 10, 14, 0, 0),
                EndTime = new DateTime(2026, 7, 10, 17, 0, 0),
                VenueName = "Arena X",
                City = "Chicago",
                State = "IL",
                Category = "Sports",
                Source = "Ticketmaster"
            };

            return new List<ExternalEventDto> { eventData, eventData };
        }

        /// <summary>
        /// Creates a secondary deduplication scenario with event from different provider
        /// but same details. Expected: Recognized as duplicate by secondary key.
        /// </summary>
        public static (ExternalEventDto first, ExternalEventDto second) CreateSecondaryDeduplicationScenario()
        {
            var first = new ExternalEventDto
            {
                ExternalId = "first-provider-101",
                Title = "World Football Championship",
                StartTime = new DateTime(2026, 8, 15, 18, 0, 0),
                EndTime = new DateTime(2026, 8, 15, 21, 0, 0),
                VenueName = "Stadium Central",
                City = "Atlanta",
                State = "GA",
                Category = "Sports",
                Source = "Ticketmaster"
            };

            var second = new ExternalEventDto
            {
                ExternalId = "second-provider-202",
                Title = "World Football Championship",
                StartTime = new DateTime(2026, 8, 15, 18, 0, 0),
                EndTime = new DateTime(2026, 8, 15, 21, 30, 0),
                VenueName = "Stadium Central",
                City = "Atlanta",
                State = "GA",
                Category = "Sports",
                Source = "SeatGeek"
            };

            return (first, second);
        }

        /// <summary>
        /// Creates a location consolidation scenario where venues from different providers
        /// should be merged to reference the same Location record.
        /// </summary>
        public static List<ExternalEventDto> CreateLocationConsolidationScenario()
        {
            return new List<ExternalEventDto>
            {
                new ExternalEventDto
                {
                    ExternalId = "msg-event-1",
                    Title = "Concert Series Part 1",
                    StartTime = new DateTime(2026, 9, 1, 19, 0, 0),
                    VenueName = "Madison Square Garden",
                    Address = "33 Penn Plaza",
                    City = "New York",
                    State = "NY",
                    Zip = "10001",
                    Source = "Ticketmaster"
                },
                new ExternalEventDto
                {
                    ExternalId = "msg-event-2",
                    Title = "Concert Series Part 2",
                    StartTime = new DateTime(2026, 9, 8, 20, 0, 0),
                    VenueName = "Madison Square Garden",
                    Address = "33 Penn Plaza",
                    City = "New York",
                    State = "NY",
                    Zip = "10001",
                    Source = "SeatGeek"
                }
            };
        }

        /// <summary>
        /// Creates a category normalization scenario where "Music" and "Concert"
        /// should map to the same category record.
        /// </summary>
        public static List<ExternalEventDto> CreateCategoryNormalizationScenario()
        {
            return new List<ExternalEventDto>
            {
                new ExternalEventDto
                {
                    ExternalId = "cat-event-1",
                    Title = "Live Music Night",
                    StartTime = new DateTime(2026, 10, 5, 19, 0, 0),
                    Category = "Music",
                    Source = "Ticketmaster"
                },
                new ExternalEventDto
                {
                    ExternalId = "cat-event-2",
                    Title = "Rock Band Performance",
                    StartTime = new DateTime(2026, 10, 12, 20, 0, 0),
                    Category = "Music",
                    Source = "SeatGeek"
                }
            };
        }

        /// <summary>
        /// Creates a validation test scenario with valid and invalid events.
        /// Invalid events without required fields should be skipped (not persisted).
        /// </summary>
        public static List<ExternalEventDto> CreateValidationTestScenario()
        {
            return new List<ExternalEventDto>
            {
                new ExternalEventDto
                {
                    ExternalId = "valid-event",
                    Title = "Valid Event",
                    StartTime = new DateTime(2026, 11, 1, 10, 0, 0),
                    Source = "Ticketmaster"
                },
                new ExternalEventDto
                {
                    ExternalId = "invalid-no-title",
                    Title = "",
                    StartTime = new DateTime(2026, 11, 2, 10, 0, 0),
                    Source = "Ticketmaster"
                },
                new ExternalEventDto
                {
                    ExternalId = "invalid-no-starttime",
                    Title = "Event Without Start Time",
                    StartTime = default(DateTime),
                    Source = "Ticketmaster"
                }
            };
        }

        /// <summary>
        /// Creates a batch upsert scenario with multiple events.
        /// Expected: N new events + M updated events = result count
        /// </summary>
        public static List<EventItem> CreateBatchUpsertScenario()
        {
            return new List<EventItem>
            {
                new EventItem
                {
                    Title = "Batch Event 1",
                    Source = "SeatGeek",
                    StartTime = new DateTime(2026, 12, 1, 10, 0, 0),
                    EndTime = new DateTime(2026, 12, 1, 12, 0, 0),
                    ExternalEventId = "batch-1",
                    ExternalEventSourceType = EventSourceType.SeatGeek
                },
                new EventItem
                {
                    Title = "Batch Event 2",
                    Source = "Ticketmaster",
                    StartTime = new DateTime(2026, 12, 5, 14, 0, 0),
                    EndTime = new DateTime(2026, 12, 5, 16, 0, 0),
                    ExternalEventId = "batch-2",
                    ExternalEventSourceType = EventSourceType.Ticketmaster
                },
                new EventItem
                {
                    Title = "Batch Event 3",
                    Source = "Ticketmaster",
                    StartTime = new DateTime(2026, 12, 10, 19, 0, 0),
                    EndTime = new DateTime(2026, 12, 10, 22, 0, 0),
                    ExternalEventId = "batch-3",
                    ExternalEventSourceType = EventSourceType.Ticketmaster
                }
            };
        }

        /// <summary>
        /// Helper method that validates an EventItem has all required fields set correctly.
        /// </summary>
        public static bool ValidateNormalizedEvent(EventItem evt)
        {
            return !string.IsNullOrWhiteSpace(evt.Title) &&
                   evt.StartTime != default(DateTime) &&
                   evt.EndTime > evt.StartTime &&
                   !string.IsNullOrWhiteSpace(evt.ExternalEventId) &&
                   evt.ExternalEventSourceType.HasValue;
        }
    }
}
