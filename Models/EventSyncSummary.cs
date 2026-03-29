namespace Community_Event_Finder.Models
{
    /// <summary>
    /// Summary of results from an event synchronization operation.
    /// </summary>
    public class EventSyncSummary
    {
        /// <summary>
        /// Names of providers that were processed.
        /// </summary>
        public List<string> ProvidersProcessed { get; set; } = new();

        /// <summary>
        /// Total number of events fetched from all providers.
        /// </summary>
        public int EventsFetched { get; set; }

        /// <summary>
        /// Number of events that passed validation.
        /// </summary>
        public int EventsValid { get; set; }

        /// <summary>
        /// Number of events that failed validation.
        /// </summary>
        public int EventsInvalid { get; set; }

        /// <summary>
        /// Number of duplicate events detected and skipped.
        /// </summary>
        public int EventsDuplicate { get; set; }

        /// <summary>
        /// Number of events successfully inserted or updated in the database.
        /// </summary>
        public int EventsUpserted { get; set; }

        /// <summary>
        /// Number of existing events marked as inactive (no longer found in providers).
        /// </summary>
        public int EventsMarkedInactive { get; set; }

        /// <summary>
        /// Errors encountered during the sync process, keyed by provider name.
        /// </summary>
        public Dictionary<string, string> ErrorsEncountered { get; set; } = new();

        /// <summary>
        /// Overall operation success status.
        /// </summary>
        public bool Success => ErrorsEncountered.Count == 0;

        /// <summary>
        /// Human-readable message summarizing the sync results.
        /// </summary>
        public string Message { get; set; } = "Sync completed";
    }
}
