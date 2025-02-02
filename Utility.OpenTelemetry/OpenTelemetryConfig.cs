using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Utility.OpenTelemetry
{
    /// <summary>
    /// Open telemtry 
    /// </summary>
    public static class OpenTelemetryConfig
    {
        /// <summary>Adds the custom open telemetry.</summary>
        /// <param name="services">The services.</param>
        /// <param name="configuration">The configuration.</param>
        /// <returns>
        ///   <br />
        /// </returns>
        /// <exception cref="System.InvalidOperationException">Azure Monitor connection string is missing in configuration.</exception>
        public static IServiceCollection AddCustomOpenTelemetry(this IServiceCollection services, IConfiguration configuration)
        {
            var serviceName = configuration["OpenTelemetry:ServiceName"] ?? "UnknowsService";
            var serviceVersion = configuration["OpenTelemetry:ServiceVersion"] ?? "1.0.0";
            var connectionString = configuration["OpenTelemetry:AzureMonitor:ConnectionString"];

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("Azure Monitor connection string is missing in configuration.");
            }

            services.AddOpenTelemetry()
                .WithTracing(tracing =>
                {
                    tracing
                        .SetResourceBuilder(ResourceBuilder.CreateDefault()
                            .AddService(serviceName, serviceVersion))
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddAzureMonitorTraceExporter(o =>
                        {
                            o.ConnectionString = connectionString;
                        });
                })
                .WithMetrics(metrics =>
                {
                    metrics
                        .SetResourceBuilder(ResourceBuilder.CreateDefault()
                            .AddService(serviceName, serviceVersion))
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddAzureMonitorMetricExporter(o =>
                        {
                            o.ConnectionString = connectionString;
                        });
                });

            // Add OpenTelemetry Logging
            services.AddLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddOpenTelemetry(options =>
                {
                    options.IncludeFormattedMessage = true; // Capture formatted messages
                    options.ParseStateValues = true; // Ensure structured logs
                    options.SetResourceBuilder(ResourceBuilder.CreateDefault()
                        .AddService(serviceName, serviceVersion));

                    options.AddConsoleExporter(); // For local debugging
                    options.AddAzureMonitorLogExporter(o =>
                    {
                        o.ConnectionString = connectionString;
                    });
                });
            });

            return services;
        }

        /// <summary>
        /// Uses the custom open telemetry.
        /// </summary>
        /// <param name="app">The application.</param>
        /// <returns></returns>
        public static IApplicationBuilder UseCustomOpenTelemetry(this IApplicationBuilder app)
        {
            app.UseMiddleware<HttpRequestLoggingMiddleware>(); // Add custom request logging middleware
            return app;
        }
    }
}
