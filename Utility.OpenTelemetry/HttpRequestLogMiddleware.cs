using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Utility.OpenTelemetry
{
    public class HttpRequestLogMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<HttpRequestLogMiddleware> _logger;

        public HttpRequestLogMiddleware(RequestDelegate next, ILogger<HttpRequestLogMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var originalResponseBody = context.Response.Body;
            MemoryStream? responseBodyStream = null;

            using var activity = Activity.Current;

            try
            {
                var request = context.Request;

                // ✅ Capture Headers Properly
                var headersDict = request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString());
                var headersJson = JsonSerializer.Serialize(headersDict);

                // ✅ Capture Request Body
                string requestBody = string.Empty;
                if (request.ContentLength > 0)
                {
                    request.EnableBuffering();  // Allows re-reading request body
                    using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
                    requestBody = await reader.ReadToEndAsync();
                    request.Body.Position = 0; // Reset stream position
                }

                // ✅ Set OpenTelemetry Tags
                activity?.SetTag("api.method", request.Method);
                activity?.SetTag("api.path", request.Path);
                activity?.SetTag("api.request.headers", headersJson);
                activity?.SetTag("api.request.body", requestBody);

                // ✅ Ensure response body logging works without potential null issues
                responseBodyStream = new MemoryStream();
                context.Response.Body = responseBodyStream;

                await _next(context);  // Call next middleware

                stopwatch.Stop();

                // ✅ Capture Response Body
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

                _logger.LogError(ex, "API Error - {Method} {Path} failed after {ExecutionTime}ms",
                    context.Request.Method, context.Request.Path, stopwatch.ElapsedMilliseconds);

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsync("An error occurred while processing the request.");
            }
            finally
            {
                // ✅ Ensure response stream is restored safely
                if (responseBodyStream != null)
                {
                    context.Response.Body.Seek(0, SeekOrigin.Begin);
                    await responseBodyStream.CopyToAsync(originalResponseBody);
                }
                context.Response.Body = originalResponseBody; // Restore original response stream
            }
        }
    }
}
