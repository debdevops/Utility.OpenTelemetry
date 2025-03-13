using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Logs;
using Microsoft.Extensions.Configuration;
using Azure.Monitor.OpenTelemetry.Exporter;

namespace Utility.OpenTelemetry
{
    /// <summary>
    /// OpenTelemetry Configuration for logging, tracing, and metrics.
    /// </summary>
    public static class OpenTelemetryConfig
    {
        /// <summary>
        /// Configures OpenTelemetry for the application.
        /// </summary>
        public static IServiceCollection AddCustomOpenTelemetry(this IServiceCollection services, IConfiguration configuration)
        {
            var serviceName = configuration["OpenTelemetry:ServiceName"] ?? "UnknownService";
            var serviceVersion = configuration["OpenTelemetry:ServiceVersion"] ?? "1.0.0";
            var connectionString = configuration["OpenTelemetry:AzureMonitor:ConnectionString"];

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("Azure Monitor connection string is missing in configuration.");
            }

            // Configure OpenTelemetry resource with service name and version
            services.Configure<OpenTelemetryLoggerOptions>(options =>
            {
                options.IncludeScopes = true; // Enables structured logging
                options.IncludeFormattedMessage = true;
                options.ParseStateValues = true;
            });

            services.AddOpenTelemetry()
                .ConfigureResource(resource => resource
                    .AddService(serviceName: serviceName, serviceVersion: serviceVersion))
                .WithTracing(tracing => tracing
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;  // Capture exceptions
                        options.Filter = (httpContext) => httpContext.Request.Path != "/health"; // Ignore health checks
                    })
                    .AddHttpClientInstrumentation()
                    .AddConsoleExporter()  // Development only
                    .AddAzureMonitorTraceExporter(o => o.ConnectionString = connectionString))
                .WithMetrics(metrics => metrics
                    .AddRuntimeInstrumentation() // ✅ Fix: Collects CPU, GC, ThreadPool
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddConsoleExporter());

            services.AddLogging(logging =>
            {
                logging.AddOpenTelemetry(options =>
                {
                    options.IncludeFormattedMessage = true;
                    options.ParseStateValues = true;
                    options.IncludeScopes = true;  // ✅ Ensure logs capture custom dimensions
                    options.AddAzureMonitorLogExporter(o => o.ConnectionString = connectionString);
                });
            });


            return services;
        }
    }
}
