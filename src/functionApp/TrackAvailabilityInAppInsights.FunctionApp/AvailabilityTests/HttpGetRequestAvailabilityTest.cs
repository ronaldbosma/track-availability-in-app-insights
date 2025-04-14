using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Logging;

namespace TrackAvailabilityInAppInsights.FunctionApp.AvailabilityTests
{
    /// <summary>
    /// Performs an HTTP GET request to check the availability of a resource.
    /// </summary>
    internal class HttpGetRequestAvailabilityTest : IAvailabilityTest
    {
        private readonly AvailabilityTest _availabilityTest;
        private readonly string _requestUri;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _httpClientName;
        private readonly ILogger _logger;

        /// <summary>
        /// Creates instance of <see cref="HttpGetRequestAvailabilityTest"/>.
        /// </summary>
        /// <param name="name">The name of the test.</param>
        /// <param name="requestUri">The request uri to check for availability.</param>
        /// <param name="telemetryClient">The telemetry client to publish the result to.</param>
        /// <param name="httpClientFactory">Factory to create an HTTP client.</param>
        /// <param name="loggerFactory">Factory to create a logger.</param>
        public HttpGetRequestAvailabilityTest(
            string name,
            string requestUri,
            TelemetryClient telemetryClient,
            IHttpClientFactory httpClientFactory,
            string httpClientName,
            ILoggerFactory loggerFactory)
        {
            _availabilityTest = new AvailabilityTest(name, CheckAvailabilityAsync, telemetryClient);

            _requestUri = requestUri;
            _httpClientFactory = httpClientFactory;
            _httpClientName = httpClientName;
            _logger = loggerFactory.CreateLogger<HttpGetRequestAvailabilityTest>();
        }

        /// <inheritdoc/>
        public async Task ExecuteAsync()
        {
            await _availabilityTest.ExecuteAsync();
        }

        /// <summary>
        /// Performs an HTTP GET on the request URI to check for availability.
        /// </summary>
        private async Task CheckAvailabilityAsync()
        {
            using var httpClient = _httpClientFactory.CreateClient(_httpClientName);

            _logger.LogInformation("Test availability of {Resource} on {BaseUrl}", _requestUri, httpClient.BaseAddress);

            var response = await httpClient.GetAsync(_requestUri);
            response.EnsureSuccessStatusCode();
        }
    }
}
