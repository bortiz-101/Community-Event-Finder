using Xunit;
using Community_Event_Finder.Data;
using Community_Event_Finder.Models;
using Community_Event_Finder.Data.ExternalProviders;
using System;
using System.Collections.Generic;

namespace Community_Event_Finder.Tests
{
    /// <summary>
    /// Unit tests for centralized validation logic.
    /// Tests validation rules for domain models and DTOs across the application.
    /// </summary>
    public class EventValidationTests
    {
        private readonly EventValidator _validator = new EventValidator();

        // ==================== EventItem Validation Tests ====================

        [Fact]
        public void ValidateEventItem_ValidEvent_ReturnsSuccess()
        {
            // Arrange
            var eventItem = new EventItem
            {
                Title = "Valid Event",
                StartTime = new DateTime(2026, 5, 15, 19, 0, 0),
                EndTime = new DateTime(2026, 5, 15, 21, 0, 0),
                ExternalEventId = "ext-123",
                ExternalEventSourceType = EventSourceType.PredictHQ
            };

            // Act
            var result = _validator.ValidateEventItem(eventItem);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void ValidateEventItem_NullEvent_ReturnsFailure()
        {
            // Act
            var result = _validator.ValidateEventItem(null);

            // Assert
            Assert.False(result.IsValid);
            Assert.NotEmpty(result.Errors);
            Assert.Contains("null", result.Errors[0], StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ValidateEventItem_MissingTitle_ReturnsFailure()
        {
            // Arrange
            var eventItem = new EventItem
            {
                Title = "",
                StartTime = new DateTime(2026, 5, 15, 19, 0, 0),
                EndTime = new DateTime(2026, 5, 15, 21, 0, 0)
            };

            // Act
            var result = _validator.ValidateEventItem(eventItem);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("Title"));
        }

        [Fact]
        public void ValidateEventItem_DefaultStartTime_ReturnsFailure()
        {
            // Arrange
            var eventItem = new EventItem
            {
                Title = "Event",
                StartTime = default(DateTime), // Not set
                EndTime = new DateTime(2026, 5, 15, 21, 0, 0)
            };

            // Act
            var result = _validator.ValidateEventItem(eventItem);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("StartTime"));
        }

        [Fact]
        public void ValidateEventItem_DefaultEndTime_ReturnsFailure()
        {
            // Arrange
            var eventItem = new EventItem
            {
                Title = "Event",
                StartTime = new DateTime(2026, 5, 15, 19, 0, 0),
                EndTime = default(DateTime) // Not set
            };

            // Act
            var result = _validator.ValidateEventItem(eventItem);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("EndTime"));
        }

        [Fact]
        public void ValidateEventItem_EndTimeBeforeStartTime_ReturnsFailure()
        {
            // Arrange
            var eventItem = new EventItem
            {
                Title = "Event",
                StartTime = new DateTime(2026, 5, 15, 21, 0, 0),
                EndTime = new DateTime(2026, 5, 15, 19, 0, 0) // Before start
            };

            // Act
            var result = _validator.ValidateEventItem(eventItem);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("after"));
        }

        [Fact]
        public void ValidateEventItem_EndTimeEqualToStartTime_ReturnsFailure()
        {
            // Arrange
            var startTime = new DateTime(2026, 5, 15, 19, 0, 0);
            var eventItem = new EventItem
            {
                Title = "Event",
                StartTime = startTime,
                EndTime = startTime // Same as start
            };

            // Act
            var result = _validator.ValidateEventItem(eventItem);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("after"));
        }

        [Fact]
        public void ValidateEventItem_TitleExceedsMaxLength_ReturnsFailure()
        {
            // Arrange
            var longTitle = new string('A', 201); // Exceeds max of 200
            var eventItem = new EventItem
            {
                Title = longTitle,
                StartTime = new DateTime(2026, 5, 15, 19, 0, 0),
                EndTime = new DateTime(2026, 5, 15, 21, 0, 0)
            };

            // Act
            var result = _validator.ValidateEventItem(eventItem);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("exceed"));
        }

        [Fact]
        public void ValidateEventItem_MultipleErrors_ReturnsAllErrors()
        {
            // Arrange
            var eventItem = new EventItem
            {
                Title = "", // Missing
                StartTime = default(DateTime), // Default
                EndTime = new DateTime(2026, 5, 15, 21, 0, 0)
            };

            // Act
            var result = _validator.ValidateEventItem(eventItem);

            // Assert
            Assert.False(result.IsValid);
            Assert.True(result.Errors.Count >= 2, "Should have multiple errors");
        }

        // ==================== AddEventDto Validation Tests ====================

        [Fact]
        public void ValidateAddEventDto_ValidDto_ReturnsSuccess()
        {
            // Arrange
            var dto = new AddEventDto
            {
                Title = "New Event",
                StartTime = new DateTime(2026, 5, 15, 19, 0, 0),
                EndTime = new DateTime(2026, 5, 15, 21, 0, 0)
            };

            // Act
            var result = _validator.ValidateAddEventDto(dto);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void ValidateAddEventDto_ValidWithoutEndTime_ReturnsSuccess()
        {
            // Arrange - valid even without EndTime (will be set to default 1 hour)
            var dto = new AddEventDto
            {
                Title = "New Event",
                StartTime = new DateTime(2026, 5, 15, 19, 0, 0)
            };

            // Act
            var result = _validator.ValidateAddEventDto(dto);

            // Assert
            Assert.True(result.IsValid);
        }

        [Fact]
        public void ValidateAddEventDto_NullDto_ReturnsFailure()
        {
            // Act
            var result = _validator.ValidateAddEventDto(null);

            // Assert
            Assert.False(result.IsValid);
            Assert.NotEmpty(result.Errors);
        }

        [Fact]
        public void ValidateAddEventDto_MissingTitle_ReturnsFailure()
        {
            // Arrange
            var dto = new AddEventDto
            {
                Title = "",
                StartTime = new DateTime(2026, 5, 15, 19, 0, 0)
            };

            // Act
            var result = _validator.ValidateAddEventDto(dto);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("Title"));
        }

        [Fact]
        public void ValidateAddEventDto_EndTimeBeforeStartTime_ReturnsFailure()
        {
            // Arrange
            var dto = new AddEventDto
            {
                Title = "Event",
                StartTime = new DateTime(2026, 5, 15, 21, 0, 0),
                EndTime = new DateTime(2026, 5, 15, 19, 0, 0) // Before start
            };

            // Act
            var result = _validator.ValidateAddEventDto(dto);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("after"));
        }

        [Fact]
        public void ValidateAddEventDto_TitleExceedsMaxLength_ReturnsFailure()
        {
            // Arrange
            var longTitle = new string('A', 201);
            var dto = new AddEventDto
            {
                Title = longTitle,
                StartTime = new DateTime(2026, 5, 15, 19, 0, 0)
            };

            // Act
            var result = _validator.ValidateAddEventDto(dto);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("exceed"));
        }

        // ==================== ExternalEventDto Validation Tests ====================

        [Fact]
        public void ValidateExternalEventDto_ValidEvent_ReturnsSuccess()
        {
            // Arrange
            var dto = new ExternalEventDto
            {
                ExternalId = "ext-123",
                Title = "Festival",
                StartTime = new DateTime(2026, 5, 15, 19, 0, 0),
                Source = "PredictHQ"
            };

            // Act
            var result = _validator.ValidateExternalEventDto(dto);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void ValidateExternalEventDto_ValidWithoutEndTime_ReturnsSuccess()
        {
            // Arrange - valid without EndTime (NormalizationService sets default)
            var dto = new ExternalEventDto
            {
                ExternalId = "ext-123",
                Title = "Festival",
                StartTime = new DateTime(2026, 5, 15, 19, 0, 0),
                Source = "PredictHQ"
            };

            // Act
            var result = _validator.ValidateExternalEventDto(dto);

            // Assert
            Assert.True(result.IsValid);
        }

        [Fact]
        public void ValidateExternalEventDto_NullDto_ReturnsFailure()
        {
            // Act
            var result = _validator.ValidateExternalEventDto(null);

            // Assert
            Assert.False(result.IsValid);
            Assert.NotEmpty(result.Errors);
        }

        [Fact]
        public void ValidateExternalEventDto_MissingTitle_ReturnsFailure()
        {
            // Arrange
            var dto = new ExternalEventDto
            {
                ExternalId = "ext-123",
                Title = "",
                StartTime = new DateTime(2026, 5, 15, 19, 0, 0),
                Source = "PredictHQ"
            };

            // Act
            var result = _validator.ValidateExternalEventDto(dto);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("Title"));
        }

        [Fact]
        public void ValidateExternalEventDto_DefaultStartTime_ReturnsFailure()
        {
            // Arrange
            var dto = new ExternalEventDto
            {
                ExternalId = "ext-123",
                Title = "Festival",
                StartTime = default(DateTime), // Not set
                Source = "PredictHQ"
            };

            // Act
            var result = _validator.ValidateExternalEventDto(dto);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("StartTime"));
        }

        [Fact]
        public void ValidateExternalEventDto_EndTimeBeforeStartTime_ReturnsFailure()
        {
            // Arrange
            var dto = new ExternalEventDto
            {
                ExternalId = "ext-123",
                Title = "Festival",
                StartTime = new DateTime(2026, 5, 15, 21, 0, 0),
                EndTime = new DateTime(2026, 5, 15, 19, 0, 0), // Before start
                Source = "PredictHQ"
            };

            // Act
            var result = _validator.ValidateExternalEventDto(dto);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("after"));
        }

        [Fact]
        public void ValidateExternalEventDto_TitleExceedsMaxLength_ReturnsFailure()
        {
            // Arrange
            var longTitle = new string('A', 201);
            var dto = new ExternalEventDto
            {
                ExternalId = "ext-123",
                Title = longTitle,
                StartTime = new DateTime(2026, 5, 15, 19, 0, 0),
                Source = "PredictHQ"
            };

            // Act
            var result = _validator.ValidateExternalEventDto(dto);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("exceed"));
        }

        // ==================== ValidationResult Tests ====================

        [Fact]
        public void ValidationResult_Success_IsValidTrue()
        {
            // Act
            var result = ValidationResult.Success();

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void ValidationResult_FailureParams_IsValidFalse()
        {
            // Act
            var result = ValidationResult.Failure("Error 1", "Error 2");

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(2, result.Errors.Count);
        }

        [Fact]
        public void ValidationResult_FailureList_IsValidFalse()
        {
            // Arrange
            var errors = new List<string> { "Error 1", "Error 2" };

            // Act
            var result = ValidationResult.Failure(errors);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(2, result.Errors.Count);
        }

        [Fact]
        public void ValidationResult_GetErrorsAsString_ConcatenatesErrors()
        {
            // Arrange
            var result = ValidationResult.Failure("Error 1", "Error 2", "Error 3");

            // Act
            var errorString = result.GetErrorsAsString();

            // Assert
            Assert.Contains("Error 1", errorString);
            Assert.Contains("Error 2", errorString);
            Assert.Contains("Error 3", errorString);
            Assert.Contains("|", errorString);
        }

        // ==================== Integration Scenario Tests ====================

        [Fact]
        public void Validation_CommercialEventScenario_Valid()
        {
            // Arrange - Simulate a commercial event like a concert
            var eventItem = new EventItem
            {
                Title = "Coldplay Live Concert 2026",
                StartTime = new DateTime(2026, 6, 15, 19, 30, 0),
                EndTime = new DateTime(2026, 6, 15, 22, 30, 0),
                LocationId = 1,
                CategoryId = 1,
                ExternalEventId = "tmn-coldplay-2026",
                ExternalEventSourceType = EventSourceType.Ticketmaster
            };

            // Act
            var result = _validator.ValidateEventItem(eventItem);

            // Assert
            Assert.True(result.IsValid);
        }

        [Fact]
        public void Validation_FestivalEventScenario_Valid()
        {
            // Arrange - Simulate a festival event
            var dto = new ExternalEventDto
            {
                ExternalId = "phq-festival-2026",
                Title = "Boston Summer Festival 2026",
                StartTime = new DateTime(2026, 7, 1, 11, 0, 0),
                EndTime = new DateTime(2026, 7, 1, 23, 0, 0),
                VenueName = "Boston Commons",
                City = "Boston",
                State = "MA",
                Category = "Festival",
                Source = "PredictHQ"
            };

            // Act
            var result = _validator.ValidateExternalEventDto(dto);

            // Assert
            Assert.True(result.IsValid);
        }

        [Fact]
        public void Validation_IncompleteIngestionEvent_Invalid()
        {
            // Arrange - Simulate an incomplete event from ingestion
            var dto = new ExternalEventDto
            {
                ExternalId = "incomplete-event",
                Title = "", // Missing required title
                StartTime = default(DateTime), // Missing required start time
                Source = "SeatGeek"
            };

            // Act
            var result = _validator.ValidateExternalEventDto(dto);

            // Assert
            Assert.False(result.IsValid);
            Assert.True(result.Errors.Count >= 2);
        }
    }
}
