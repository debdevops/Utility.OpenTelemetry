using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Utility.OpenTelemetry.Test
{
    /// <summary>
    /// HttpRequestLogMiddleware unit test
    /// </summary>
    [TestClass]
    public class HttpRequestLogMiddlewareTests
    {
        /// <summary>
        /// The logger mock
        /// </summary>
        private Mock<Microsoft.Extensions.Logging.ILogger<HttpRequestLogMiddleware>> _loggerMock;
        /// <summary>
        /// The next
        /// </summary>
        private RequestDelegate _next;
        /// <summary>
        /// The middleware
        /// </summary>
        private HttpRequestLogMiddleware _middleware;

        /// <summary>
        /// Setups this instance.
        /// </summary>
        [TestInitialize]
        public void Setup()
        {
            _loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<HttpRequestLogMiddleware>>();
            _next = (HttpContext context) => Task.CompletedTask;
            _middleware = new HttpRequestLogMiddleware(_next, _loggerMock.Object);
        }

        /// <summary>
        /// Invokes the logs request and response.
        /// </summary>
        [TestMethod]
        public async Task Invoke_LogsRequestAndResponse()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Method = "GET";
            context.Request.Path = "/test";
            context.Request.Headers["Test-Header"] = "HeaderValue";
            context.Response.Body = new MemoryStream();

            // Act
            await _middleware.Invoke(context);

            // Assert
            _loggerMock.Verify(
                logger => logger.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("API Execution")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        /// <summary>
        /// Invokes the handles exception.
        /// </summary>
        [TestMethod]
        public async Task Invoke_HandlesException()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Method = "GET";
            context.Request.Path = "/test";
            context.Response.Body = new MemoryStream();

            _next = (HttpContext ctx) => throw new Exception("Test exception");
            _middleware = new HttpRequestLogMiddleware(_next, _loggerMock.Object);

            // Act
            await _middleware.Invoke(context);

            // Assert
            _loggerMock.Verify(
                logger => logger.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("API Error")),
                    It.Is<Exception>(ex => ex.Message == "Test exception"),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
    }
}
