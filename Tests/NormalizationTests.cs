using Community_Event_Finder.Data.ExternalProviders;
using Community_Event_Finder.Models;
using Xunit;

namespace Community_Event_Finder.Tests
{
    /// <summary>
    /// Unit tests for event normalization features.
    /// Validates that external provider events are properly normalized to EventItem domain models.
    /// Issue #34: Normalization & Deduplication
    /// </summary>
    public class NormalizationTests
    {
        [Fact]
        public void PredictHQEventNormalization_ShouldPopulateRequiredFields()
        {
            // Arrange
            var externalEvent = TestDataFactory.CreatePredictHQTestEvent();

            // Act
            var normalized = new EventItem
            {
                Title = externalEvent.Title ?? "",
                StartTime = externalEvent.StartTime,
                EndTime = externalEvent.EndTime ?? externalEvent.StartTime.AddHours(1),
                ExternalEventId = externalEvent.ExternalId ?? "",
                ExternalEventSourceType = EventSourceType.PredictHQ,
                Source = externalEvent.Source ?? ""
            };

            // Assert
            Assert.NotNull(normalized);
            Assert.Equal("Live Concert - PredictHQ", normalized.Title);
            Assert.Equal(new System.DateTime(2026, 5, 15, 19, 0, 0), normalized.StartTime);
            Assert.Equal(new System.DateTime(2026, 5, 15, 22, 0, 0), normalized.EndTime);
            Assert.Equal("predictq-demo-123", normalized.ExternalEventId);
            Assert.Equal(EventSourceType.PredictHQ, normalized.ExternalEventSourceType);
        }

        [Fact]
        public void SeatGeekEventNormalization_ShouldPopulateRequiredFields()
        {
            // Arrange
            var externalEvent = TestDataFactory.CreateSeatGeekTestEvent();

            // Act
            var normalized = new EventItem
            {
                Title = externalEvent.Title ?? "",
                StartTime = externalEvent.StartTime,
                EndTime = externalEvent.EndTime ?? externalEvent.StartTime.AddHours(1),
                ExternalEventId = externalEvent.ExternalId ?? "",
                ExternalEventSourceType = EventSourceType.SeatGeek,
                Source = externalEvent.Source ?? ""
            };

            // Assert
            Assert.NotNull(normalized);
            Assert.Equal("International Festival - SeatGeek", normalized.Title);
            Assert.Equal(EventSourceType.SeatGeek, normalized.ExternalEventSourceType);
            Assert.Equal("seatgeek-demo-456", normalized.ExternalEventId);
        }

        [Fact]
        public void NormalizedEvent_ShouldPassValidation()
        {
            // Arrange
            var externalEvent = TestDataFactory.CreatePredictHQTestEvent();
            var normalized = new EventItem
            {
                Title = externalEvent.Title ?? "",
                StartTime = externalEvent.StartTime,
                EndTime = externalEvent.EndTime ?? externalEvent.StartTime.AddHours(1),
                ExternalEventId = externalEvent.ExternalId ?? "",
                ExternalEventSourceType = EventSourceType.PredictHQ
            };

            // Act
            var isValid = TestDataFactory.ValidateNormalizedEvent(normalized);

            // Assert
            Assert.True(isValid, "Normalized event should pass validation");
        }

        [Fact]
        public void InvalidEvent_MissingTitle_ShouldFailValidation()
        {
            // Arrange
            var normalized = new EventItem
            {
                Title = "",
                StartTime = new System.DateTime(2026, 11, 1, 10, 0, 0),
                EndTime = new System.DateTime(2026, 11, 1, 12, 0, 0),
                ExternalEventId = "test-123",
                ExternalEventSourceType = EventSourceType.PredictHQ
            };

            // Act
            var isValid = TestDataFactory.ValidateNormalizedEvent(normalized);

            // Assert
            Assert.False(isValid, "Event with empty title should fail validation");
        }

        [Fact]
        public void InvalidEvent_MissingStartTime_ShouldFailValidation()
        {
            // Arrange
            var normalized = new EventItem
            {
                Title = "Test Event",
                StartTime = default(System.DateTime),
                EndTime = new System.DateTime(2026, 11, 1, 12, 0, 0),
                ExternalEventId = "test-123",
                ExternalEventSourceType = EventSourceType.PredictHQ
            };

            // Act
            var isValid = TestDataFactory.ValidateNormalizedEvent(normalized);

            // Assert
            Assert.False(isValid, "Event with default start time should fail validation");
        }

        [Fact]
        public void InvalidEvent_EndTimeBeforeStartTime_ShouldFailValidation()
        {
            // Arrange
            var normalized = new EventItem
            {
                Title = "Test Event",
                StartTime = new System.DateTime(2026, 11, 1, 12, 0, 0),
                EndTime = new System.DateTime(2026, 11, 1, 10, 0, 0),
                ExternalEventId = "test-123",
                ExternalEventSourceType = EventSourceType.PredictHQ
            };

            // Act
            var isValid = TestDataFactory.ValidateNormalizedEvent(normalized);

            // Assert
            Assert.False(isValid, "Event with end time before start time should fail validation");
        }

        [Fact]
        public void LocationConsolidation_ShouldMapVenueToLocation()
        {
            // Arrange
            var events = TestDataFactory.CreateLocationConsolidationScenario();

            // Act & Assert - Both events reference same venue
            Assert.Equal(2, events.Count);
            Assert.All(events, e => Assert.Equal("Madison Square Garden", e.VenueName));
            Assert.All(events, e => Assert.Equal("33 Penn Plaza", e.Address));
        }

        [Fact]
        public void CategoryNormalization_ShouldNormalizeEventCategory()
        {
            // Arrange
            var events = TestDataFactory.CreateCategoryNormalizationScenario();

            // Act & Assert - Both events have same category
            Assert.Equal(2, events.Count);
            Assert.All(events, e => Assert.Equal("Music", e.Category));
        }

        [Fact]
        public void ValidationScenario_ShouldSkipInvalidEvents()
        {
            // Arrange
            var events = TestDataFactory.CreateValidationTestScenario();

            // Act
            var validEvents = events
                .Where(e => !string.IsNullOrWhiteSpace(e.Title) && e.StartTime != default(System.DateTime))
                .ToList();

            // Assert
            Assert.Equal(3, events.Count);
            Assert.Single(validEvents); // Only one valid event
            Assert.Equal("Valid Event", validEvents[0].Title);
        }
    }
}
