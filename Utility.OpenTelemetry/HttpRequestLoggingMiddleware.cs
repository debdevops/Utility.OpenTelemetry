using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace Utility.OpenTelemetry
{
    public class HttpRequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<HttpRequestLoggingMiddleware> _logger;

        public HttpRequestLoggingMiddleware(RequestDelegate next, ILogger<HttpRequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var request = context.Request;

            // ✅ Capture Headers Properly
            var headersDict = request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString());
            var headersJson = JsonSerializer.Serialize(headersDict);

            // ✅ Capture Request Body
            string requestBody = string.Empty;
            if (request.ContentLength > 0)
            {
                request.EnableBuffering();
                using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
                requestBody = await reader.ReadToEndAsync();
                request.Body.Position = 0; // Reset stream position

                // Ensure clean JSON logging (remove extra quotes if it's a simple string)
                if (!string.IsNullOrWhiteSpace(requestBody))
                {
                    try
                    {
                        var parsedJson = JsonSerializer.Deserialize<object>(requestBody);
                        requestBody = parsedJson is string str ? str : JsonSerializer.Serialize(parsedJson);
                    }
                    catch
                    {
                        // If JSON parsing fails, use raw request body
                    }
                }
            }

            // Capture Response Body
            var originalResponseBody = context.Response.Body;
            await using var responseBodyStream = new MemoryStream();
            context.Response.Body = responseBodyStream;

            using var activity = Activity.Current;
            activity?.SetTag("api.method", request.Method);
            activity?.SetTag("api.path", request.Path);
            activity?.SetTag("api.request.headers", headersJson);  // ✅ Log headers in OpenTelemetry
            activity?.SetTag("api.request.body", requestBody);

            try
            {
                await _next(context);
                stopwatch.Stop();

                // Capture Response Body
                context.Response.Body.Seek(0, SeekOrigin.Begin);
                string responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
                context.Response.Body.Seek(0, SeekOrigin.Begin);

                activity?.SetTag("api.execution_time_ms", stopwatch.ElapsedMilliseconds);
                activity?.SetTag("api.response.status_code", context.Response.StatusCode);
                activity?.SetTag("api.response.body", responseBody);

                _logger.LogInformation("API Execution - {Method} {Path} took {ExecutionTime}ms, StatusCode: {StatusCode}, Headers: {Headers}, RequestBody: {RequestBody}",
                    request.Method, request.Path, stopwatch.ElapsedMilliseconds, context.Response.StatusCode, headersJson, requestBody);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                activity?.SetTag("api.execution_time_ms", stopwatch.ElapsedMilliseconds);
                activity?.SetTag("error", true);
                activity?.SetTag("exception.message", ex.Message);
                activity?.SetTag("exception.stacktrace", ex.StackTrace);

                _logger.LogError(ex, "API Error - {Method} {Path} failed after {ExecutionTime}ms, Headers: {Headers}, RequestBody: {RequestBody}",
                    request.Method, request.Path, stopwatch.ElapsedMilliseconds, headersJson, requestBody);

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsync("An error occurred while processing the request.");
            }
            finally
            {
                context.Response.Body.Seek(0, SeekOrigin.Begin);
                await responseBodyStream.CopyToAsync(originalResponseBody);
                context.Response.Body = originalResponseBody; // Restore original response stream
            }
        }
    }
}
