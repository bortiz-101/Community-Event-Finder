namespace Community_Event_Finder.Models

{

    public class ApiErrorResponse

    {

        public string Error { get; set; } = "An unexpected error occurred.";

        public int StatusCode { get; set; }

        public string TraceId { get; set; } = string.Empty;

        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

    }

}