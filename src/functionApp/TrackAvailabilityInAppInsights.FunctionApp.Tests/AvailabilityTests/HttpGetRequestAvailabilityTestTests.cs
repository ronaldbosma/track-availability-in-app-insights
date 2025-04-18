using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
using TrackAvailabilityInAppInsights.FunctionApp.AvailabilityTests;
using TrackAvailabilityInAppInsights.FunctionApp.Tests.Fakes;

namespace TrackAvailabilityInAppInsights.FunctionApp.Tests.AvailabilityTests
{
    [TestClass]
    public class HttpGetRequestAvailabilityTestTests
    {
        private const string name = "TestName";
        private const string HttpClientName = "apim";

        private readonly TelemetryChannelFake _telemetryChannelFake = new();
        private readonly HttpClientFactoryFake _httpClientFactory = new();
        private readonly HttpClientFake _httpClientFake = new();

        public HttpGetRequestAvailabilityTestTests()
        {
            _httpClientFactory.StubHttpClient(HttpClientName, _httpClientFake);
        }

        [TestMethod]
        public async Task ExecuteAsync_HttpClientReturnsSuccess_AvailabilitySuccessTracked()
        {
            // Arrange
            const string requestUri = "/healthy";

            // Stub that the HttpClient returns a success status code when checking the availability
            _httpClientFake.StubResponseForGetRequest(requestUri, HttpStatusCode.OK);

            HttpGetRequestAvailabilityTest sut = new(name, requestUri, _telemetryChannelFake.CreateTelemetryClient(),
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
            const string requestUri = "/unhealthy";

            // Stub that the HttpClient returns a 503 Service Unavailable when checking the availability
            _httpClientFake.StubResponseForGetRequest(requestUri, HttpStatusCode.ServiceUnavailable);

            HttpGetRequestAvailabilityTest sut = new(name, requestUri, _telemetryChannelFake.CreateTelemetryClient(),
                _httpClientFactory, HttpClientName, new NullLoggerFactory());

            // Act
            async Task act() => await sut.ExecuteAsync();

            // Assert
            Exception actualException = await Assert.ThrowsExceptionAsync<HttpRequestException>(act);
            Assert.AreEqual("Response status code does not indicate success: 503 (Service Unavailable).", actualException.Message);

            _telemetryChannelFake.VerifyThatFailedAvailabilityIsTrackedForTest(name, "Response status code does not indicate success: 503 (Service Unavailable).");
        }
    }
}
