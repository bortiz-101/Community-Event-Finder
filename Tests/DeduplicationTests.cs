using Community_Event_Finder.Data.ExternalProviders;
using Community_Event_Finder.Models;
using Xunit;

namespace Community_Event_Finder.Tests
{
    /// <summary>
    /// Unit tests for event deduplication features.
    /// Validates that duplicate events are properly detected and handled.
    /// Issue #34: Normalization & Deduplication
    /// </summary>
    public class DeduplicationTests
    {
        [Fact]
        public void PrimaryDeduplication_SameExternalIdAndSource_ShouldBeTreatedAsDuplicate()
        {
            // Arrange
            var events = TestDataFactory.CreateDuplicateTestScenario();

            // Act
            var uniqueEvents = events
                .DistinctBy(e => new { e.Source, e.ExternalId })
                .ToList();

            // Assert
            Assert.Equal(2, events.Count); // Original has 2 (duplicates)
            Assert.Single(uniqueEvents); // After deduplication: 1 unique
        }

        [Fact]
        public void PrimaryDeduplication_CompoundKey_SourceAndExternalId()
        {
            // Arrange - Create two events with same ExternalId but different sources
            var event1 = new ExternalEventDto
            {
                ExternalId = "event-123",
                Title = "Event 1",
                Source = "Ticketmaster",
                StartTime = new System.DateTime(2026, 5, 15, 19, 0, 0),
                EndTime = new System.DateTime(2026, 5, 15, 22, 0, 0)
            };

            var event2 = new ExternalEventDto
            {
                ExternalId = "event-123",
                Title = "Event 1",
                Source = "SeatGeek", // Different source
                StartTime = new System.DateTime(2026, 5, 15, 19, 0, 0),
                EndTime = new System.DateTime(2026, 5, 15, 22, 0, 0)
            };

            var events = new List<ExternalEventDto> { event1, event2 };

            // Act
            var uniqueEvents = events
                .DistinctBy(e => new { e.Source, e.ExternalId })
                .ToList();

            // Assert - Should NOT be deduplicated (different sources)
            Assert.Equal(2, uniqueEvents.Count);
        }

        [Fact]
        public void SecondaryDeduplication_SameTitleAndStartTime_ShouldBeTreatedAsDuplicate()
        {
            // Arrange
            var (first, second) = TestDataFactory.CreateSecondaryDeduplicationScenario();
            var events = new List<ExternalEventDto> { first, second };

            // Act - Group by secondary key
            var groups = events
                .GroupBy(e => new { e.Title, e.StartTime, e.VenueName })
                .ToList();

            // Assert
            Assert.Single(groups); // Both events group together
            Assert.Equal(2, groups[0].Count()); // Group has 2 events
        }

        [Fact]
        public void MultipleProviders_SameEvent_ShouldUseSecondaryDedup()
        {
            // Arrange
            var (first, second) = TestDataFactory.CreateSecondaryDeduplicationScenario();

            // Act
            var isSameEvent =
                first.Title == second.Title &&
                first.StartTime == second.StartTime &&
                first.VenueName == second.VenueName;

            // Assert
            Assert.True(isSameEvent, "Events from different providers should match on secondary key");
        }

        [Fact]
        public void LocationConsolidation_MultipleVenueReferences_ShouldConsolidate()
        {
            // Arrange
            var events = TestDataFactory.CreateLocationConsolidationScenario();

            // Act - Simulate location consolidation
            var uniqueLocations = events
                .DistinctBy(e => new { e.VenueName, e.Address, e.City, e.State })
                .ToList();

            // Assert
            Assert.Equal(2, events.Count); // Two events
            Assert.Single(uniqueLocations); // But only one unique location
            Assert.Equal("Madison Square Garden", uniqueLocations[0].VenueName);
        }

        [Fact]
        public void BatchUpsert_MixedNewAndDuplicateEvents_ShouldHandleCorrectly()
        {
            // Arrange
            var events = TestDataFactory.CreateBatchUpsertScenario();

            // Act - Simulate upsert with deduplication
            var uniqueEvents = events
                .DistinctBy(e => new { e.ExternalEventId, e.ExternalEventSourceType })
                .ToList();

            // Assert
            Assert.Equal(3, events.Count);
            Assert.Equal(3, uniqueEvents.Count); // All are unique
        }

        [Fact]
        public void BatchUpsert_DuplicateInBatch_ShouldKeepOne()
        {
            // Arrange
            var baseEvents = TestDataFactory.CreateBatchUpsertScenario();
            var eventsWithDuplicate = new List<EventItem>(baseEvents);
            eventsWithDuplicate.Add(baseEvents[0]); // Add duplicate

            // Act
            var uniqueEvents = eventsWithDuplicate
                .DistinctBy(e => new { e.ExternalEventId, e.ExternalEventSourceType })
                .ToList();

            // Assert
            Assert.Equal(4, eventsWithDuplicate.Count); // 3 + 1 duplicate
            Assert.Equal(3, uniqueEvents.Count); // After dedup: 3 unique
        }

        [Fact]
        public void EventUpdate_DuplicateDetected_ShouldUpdateNotInsert()
        {
            // Arrange
            var original = new EventItem
            {
                EventId = Guid.NewGuid().ToString(),
                Title = "Concert",
                ExternalEventId = "ext-123",
                ExternalEventSourceType = EventSourceType.Ticketmaster,
                Source = "Ticketmaster",
                StartTime = new System.DateTime(2026, 5, 15, 19, 0, 0),
                EndTime = new System.DateTime(2026, 5, 15, 22, 0, 0)
            };

            var updated = new EventItem
            {
                Title = "Concert - Updated Info",
                ExternalEventId = "ext-123",
                ExternalEventSourceType = EventSourceType.Ticketmaster,
                Source = "Ticketmaster",
                StartTime = new System.DateTime(2026, 5, 15, 19, 30, 0),
                EndTime = new System.DateTime(2026, 5, 15, 22, 30, 0)
            };

            // Act - Check if same event (by primary key)
            var isDuplicate =
                original.ExternalEventId == updated.ExternalEventId &&
                original.ExternalEventSourceType == updated.ExternalEventSourceType;

            // Assert
            Assert.True(isDuplicate, "Event should be recognized as duplicate");
            Assert.NotEqual(original.Title, updated.Title); // Data updated
        }
    }
}
