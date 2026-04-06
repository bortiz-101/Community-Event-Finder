using Community_Event_Finder.Models;
using Community_Event_Finder.Data.ExternalProviders;
using System;
using System.Collections.Generic;

namespace Community_Event_Finder.Data
{
    /// <summary>
    /// Service for validating events across different contexts (domain models and DTOs).
    /// Provides centralized, reusable validation logic with detailed error messages.
    /// </summary>
    public interface IEventValidator
    {
        /// <summary>
        /// Validates a domain EventItem model.
        /// Checks: Title (required, non-empty), StartTime (required, not default), 
        /// EndTime (required, after StartTime)
        /// </summary>
        ValidationResult ValidateEventItem(EventItem eventItem);

        /// <summary>
        /// Validates an AddEventDto (user-submitted event).
        /// Checks: Title (required, non-empty), StartTime/EndTime validation
        /// </summary>
        ValidationResult ValidateAddEventDto(Models.AddEventDto dto);

        /// <summary>
        /// Validates an external event DTO (from provider).
        /// Checks: Title (required, non-empty), StartTime (required, not default)
        /// </summary>
        ValidationResult ValidateExternalEventDto(ExternalEventDto dto);
    }

    public class EventValidator : IEventValidator
    {
        // Configuration constants for validation
        private const int MaxTitleLength = 200;
        private const int MinTitleLength = 1;

        /// <summary>
        /// Validates a domain EventItem with strict requirements.
        /// </summary>
        public ValidationResult ValidateEventItem(EventItem eventItem)
        {
            if (eventItem == null)
                return ValidationResult.Failure("Event item cannot be null");

            var errors = new List<string>();

            // Title validation
            if (string.IsNullOrWhiteSpace(eventItem.Title))
                errors.Add("Title is required and cannot be empty");
            else if (eventItem.Title.Length > MaxTitleLength)
                errors.Add($"Title cannot exceed {MaxTitleLength} characters");

            // StartTime validation
            if (eventItem.StartTime == default(DateTime))
                errors.Add("StartTime is required and cannot be default");

            // EndTime validation
            if (eventItem.EndTime == default(DateTime))
                errors.Add("EndTime is required and cannot be default");
            else if (eventItem.EndTime <= eventItem.StartTime)
                errors.Add("EndTime must be after StartTime");

            // Return result
            return errors.Count > 0
                ? ValidationResult.Failure(errors)
                : ValidationResult.Success();
        }

        /// <summary>
        /// Validates an AddEventDto (user-submitted event).
        /// This is more lenient than domain validation since defaults will be applied.
        /// </summary>
        public ValidationResult ValidateAddEventDto(AddEventDto dto)
        {
            if (dto == null)
                return ValidationResult.Failure("Event data cannot be null");

            var errors = new List<string>();

            // Title is the only strictly required field for AddEventDto
            if (string.IsNullOrWhiteSpace(dto.Title))
                errors.Add("Title is required");
            else if (dto.Title.Length > MaxTitleLength)
                errors.Add($"Title cannot exceed {MaxTitleLength} characters");

            // If provided, validate StartTime and EndTime
            if (dto.StartTime.HasValue && dto.EndTime.HasValue)
            {
                if (dto.EndTime.Value <= dto.StartTime.Value)
                    errors.Add("EndTime must be after StartTime");
            }

            return errors.Count > 0
                ? ValidationResult.Failure(errors)
                : ValidationResult.Success();
        }

        /// <summary>
        /// Validates an external event DTO from provider.
        /// Enforces: Title (required, non-empty), StartTime (required, not default)
        /// Ingestion errors are logged but don't stop processing of other events.
        /// </summary>
        public ValidationResult ValidateExternalEventDto(ExternalEventDto dto)
        {
            if (dto == null)
                return ValidationResult.Failure("External event data cannot be null");

            var errors = new List<string>();

            // Title validation - required for all providers
            if (string.IsNullOrWhiteSpace(dto.Title))
                errors.Add("Title is required and cannot be empty");
            else if (dto.Title.Length > MaxTitleLength)
                errors.Add($"Title cannot exceed {MaxTitleLength} characters");

            // StartTime validation - required for all providers
            if (dto.StartTime == default(DateTime))
                errors.Add("StartTime is required and cannot be default");

            // EndTime validation - if provided, must be after StartTime
            // EndTime can be null initially; NormalizationService will set default if needed
            if (dto.EndTime.HasValue && dto.EndTime.Value <= dto.StartTime)
                errors.Add("If EndTime is provided, it must be after StartTime");

            return errors.Count > 0
                ? ValidationResult.Failure(errors)
                : ValidationResult.Success();
        }
    }
}
