using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Utility.OpenTelemetry
{
    /// <summary>
    /// HTTP request logging
    /// </summary>
    public class HttpRequestLoggingMiddleware
    {
        /// <summary>
        /// The next
        /// </summary>
        private readonly RequestDelegate _next;
        /// <summary>
        /// The logger
        /// </summary>
        private readonly ILogger<HttpRequestLoggingMiddleware> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpRequestLoggingMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next.</param>
        /// <param name="logger">The logger.</param>
        public HttpRequestLoggingMiddleware(RequestDelegate next, ILogger<HttpRequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        /// <summary>
        /// Invokes the asynchronous.
        /// </summary>
        /// <param name="context">The context.</param>
        public async Task InvokeAsync(HttpContext context)
        {
            //Log request header
            var request = context.Request;

            //Log request header
            _logger.LogInformation("Request headers: ");
            foreach (var item in request.Headers) { 
                _logger.LogInformation($"{item.Key} : {item.Value}");
            }

            //Log query string parameters
            _logger.LogInformation($"Request query parameters : ");
            foreach (var item in request.Query) { 
                _logger.LogInformation($"{item.Key} : {item.Value}");  
            }

            if (context.Request.Method == HttpMethods.Post ||
                context.Request.Method == HttpMethods.Put ||
                context.Request.Method == HttpMethods.Delete)
            {
                context.Request.EnableBuffering(); // Allows reading the body multiple times

                using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
                var requestBody = await reader.ReadToEndAsync();

                if (!string.IsNullOrWhiteSpace(requestBody))
                {
                    _logger.LogInformation("HTTP {Method} Request to {Path} with Body: {Body}",
                        context.Request.Method, context.Request.Path, requestBody);
                }

                context.Request.Body.Position = 0; // Reset body position for next middleware
            }

            await _next(context);
        }
    }
}
