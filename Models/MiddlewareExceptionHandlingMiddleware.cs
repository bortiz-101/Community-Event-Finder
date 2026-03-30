using System.Text.Json;

using Community_Event_Finder.Models;



namespace Community_Event_Finder.Middleware

{

    public class ExceptionHandlingMiddleware

    {

        private readonly RequestDelegate _next;

        private readonly ILogger<ExceptionHandlingMiddleware> _logger;



        public ExceptionHandlingMiddleware(

            RequestDelegate next,

            ILogger<ExceptionHandlingMiddleware> logger)

        {

            _next = next;

            _logger = logger;

        }



        public async Task Invoke(HttpContext context)

        {

            try

            {

                await _next(context);

            }

            catch (Exception ex)

            {

                await HandleExceptionAsync(context, ex, _logger);

            }

        }



        private static async Task HandleExceptionAsync(

            HttpContext context,

            Exception exception,

            ILogger logger)

        {

            var traceId = context.TraceIdentifier;

            var statusCode = StatusCodes.Status500InternalServerError;

            var message = "An unexpected error occurred.";



            switch (exception)

            {

                case InvalidOperationException:

                    statusCode = StatusCodes.Status400BadRequest;

                    message = exception.Message;

                    break;



                case KeyNotFoundException:

                    statusCode = StatusCodes.Status404NotFound;

                    message = exception.Message;

                    break;



                case IngestionException ingestionEx:

                    statusCode = StatusCodes.Status502BadGateway;

                    message = ingestionEx.Message;



                    logger.LogError(

                        ingestionEx,

                        "Ingestion failure from provider {ProviderName}. TraceId: {TraceId}",

                        ingestionEx.ProviderName,

                        traceId);

                    break;



                default:

                    logger.LogError(

                        exception,

                        "Unhandled exception occurred. TraceId: {TraceId}",

                        traceId);

                    break;

            }



            if (exception is not IngestionException)

            {

                logger.LogError(

                    exception,

                    "Request failed with status code {StatusCode}. TraceId: {TraceId}",

                    statusCode,

                    traceId);

            }



            context.Response.ContentType = "application/json";

            context.Response.StatusCode = statusCode;



            var response = new ApiErrorResponse

            {

                Error = message,

                StatusCode = statusCode,

                TraceId = traceId,

                TimestampUtc = DateTime.UtcNow

            };



            var json = JsonSerializer.Serialize(response);

            await context.Response.WriteAsync(json);

        }

    }

}