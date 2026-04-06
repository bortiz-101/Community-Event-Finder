using System;
using System.Collections.Generic;
using System.Linq;

namespace Community_Event_Finder.Data
{
    /// <summary>
    /// Represents the result of validation, capturing whether validation passed
    /// and any validation errors encountered.
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; private set; }
        public List<string> Errors { get; private set; }

        private ValidationResult()
        {
            Errors = new List<string>();
            IsValid = true;
        }

        /// <summary>
        /// Creates a successful validation result.
        /// </summary>
        public static ValidationResult Success()
        {
            return new ValidationResult { IsValid = true, Errors = new List<string>() };
        }

        /// <summary>
        /// Creates a failed validation result with the specified error messages.
        /// </summary>
        public static ValidationResult Failure(params string[] errors)
        {
            return new ValidationResult
            {
                IsValid = false,
                Errors = errors.ToList()
            };
        }

        /// <summary>
        /// Creates a failed validation result with a list of error messages.
        /// </summary>
        public static ValidationResult Failure(List<string> errors)
        {
            return new ValidationResult
            {
                IsValid = false,
                Errors = errors ?? new List<string>()
            };
        }

        /// <summary>
        /// Returns all errors concatenated as a single string.
        /// </summary>
        public string GetErrorsAsString()
        {
            return string.Join(" | ", Errors);
        }
    }
}
