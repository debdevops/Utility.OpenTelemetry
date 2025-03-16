using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Logs;
using Microsoft.Extensions.Logging;

namespace Utility.OpenTelemetry.Test
{
    [TestClass]
    public class OpenTelemetryConfigTests
    {
        private Mock<IConfiguration> _configurationMock;
        private IServiceCollection _services;

        [TestInitialize]
        public void Setup()
        {
            _configurationMock = new Mock<IConfiguration>();
            _services = new ServiceCollection();
        }

        [TestMethod]
        public void AddCustomOpenTelemetry_ConfiguresServicesCorrectly()
        {
            // Arrange
            _configurationMock.Setup(c => c["OpenTelemetry:ServiceName"]).Returns("TestService");
            _configurationMock.Setup(c => c["OpenTelemetry:ServiceVersion"]).Returns("1.0.0");
            _configurationMock.Setup(c => c["OpenTelemetry:AzureMonitor:ConnectionString"]).Returns("InstrumentationKey=00000000-0000-0000-0000-000000000000");

            // Act
            _services.AddCustomOpenTelemetry(_configurationMock.Object);
            var serviceProvider = _services.BuildServiceProvider();

            // Assert
            var tracerProvider = serviceProvider.GetService<TracerProvider>();
            Assert.IsNotNull(tracerProvider, "TracerProvider should be configured.");

            var meterProvider = serviceProvider.GetService<MeterProvider>();
            Assert.IsNotNull(meterProvider, "MeterProvider should be configured.");

            var loggerProvider = serviceProvider.GetService<ILoggerProvider>();
            Assert.IsNotNull(loggerProvider, "LoggerProvider should be configured.");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void AddCustomOpenTelemetry_ThrowsException_WhenConnectionStringIsMissing()
        {
            // Arrange
            _configurationMock.Setup(c => c["OpenTelemetry:ServiceName"]).Returns("TestService");
            _configurationMock.Setup(c => c["OpenTelemetry:ServiceVersion"]).Returns("1.0.0");
            _configurationMock.Setup(c => c["OpenTelemetry:AzureMonitor:ConnectionString"]).Returns(string.Empty);

            // Act
            _services.AddCustomOpenTelemetry(_configurationMock.Object);
        }
    }
}
