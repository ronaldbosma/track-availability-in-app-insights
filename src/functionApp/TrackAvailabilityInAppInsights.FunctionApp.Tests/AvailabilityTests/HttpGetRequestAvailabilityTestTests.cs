using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
using TrackAvailabilityInAppInsights.FunctionApp.AvailabilityTests;
using TrackAvailabilityInAppInsights.FunctionApp.Tests.Fakes;

namespace TrackAvailabilityInAppInsights.FunctionApp.Tests.AvailabilityTests
{
    [TestClass]
    public class HttpGetRequestAvailabilityTestTests
    {
        private const string HttpClientName = "apim";

        private readonly TelemetryChannelFake _telemetryChannelFake;
        private readonly HttpClientFactoryFake _httpClientFactory;
        private readonly HttpMessageHandlerFake _httpMessageHandlerFake;

        public HttpGetRequestAvailabilityTestTests()
        {
            _telemetryChannelFake = new TelemetryChannelFake();

            _httpMessageHandlerFake = new HttpMessageHandlerFake();
            _httpClientFactory = new HttpClientFactoryFake();
            _httpClientFactory.StubHttpClient(HttpClientName, _httpMessageHandlerFake.CreateHttpClient());
        }

        [TestMethod]
        public async Task ExecuteAsync_HttpClientReturnsSuccess_AvailabilitySuccessTracked()
        {
            // Arrange
            var name = "TestName";
            var requestUri = "/healthy";

            // Stub that the HttpClient returns a success status code when checking the availability
            _httpMessageHandlerFake.StubResponseForGetRequest(requestUri, HttpStatusCode.OK);

            var sut = new HttpGetRequestAvailabilityTest(name, requestUri, _telemetryChannelFake.CreateTelemetryClient(),
                _httpClientFactory, HttpClientName, new NullLoggerFactory());

            // Act
            await sut.ExecuteAsync();

            // Assert
            _telemetryChannelFake.VerifyThatSuccessfulAvailabilityIsTrackedForTest(name);
        }

        [TestMethod]
        public async Task ExecuteAsync_HttpClientReturnsError_AvailabilityFailureTracked()
        {
            // Arrange
            var name = "TestName";
            var requestUri = "/unhealthy";

            // Stub that the HttpClient returns a 503 Service Unavailable when checking the availability
            _httpMessageHandlerFake.StubResponseForGetRequest(requestUri, HttpStatusCode.ServiceUnavailable);

            var sut = new HttpGetRequestAvailabilityTest(name, requestUri, _telemetryChannelFake.CreateTelemetryClient(),
                _httpClientFactory, HttpClientName, new NullLoggerFactory());

            // Act
            async Task act() => await sut.ExecuteAsync();

            // Assert
            var ex = await Assert.ThrowsExceptionAsync<HttpRequestException>(act);
            Assert.AreEqual("Response status code does not indicate success: 503 (Service Unavailable).", ex.Message);

            _telemetryChannelFake.VerifyThatFailedAvailabilityIsTrackedForTest(name, "Response status code does not indicate success: 503 (Service Unavailable).");
        }
    }

}
