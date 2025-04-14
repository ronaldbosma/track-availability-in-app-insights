using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Logging;

namespace TrackAvailabilityInAppInsights.FunctionApp.AvailabilityTests
{
    /// <summary>
    /// Factory to create availability tests.
    /// </summary>
    internal class AvailabilityTestFactory : IAvailabilityTestFactory
    {
        private readonly TelemetryClient _telemetryClient;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILoggerFactory _loggerFactory;

        /// <summary>
        /// Creates an instance of <see cref="AvailabilityTestFactory"/>.
        /// </summary>
        /// <param name="telemetryClient">The telemetry client to publish the availability result to.</param>
        /// <param name="httpClientFactory">Factory to create an HTTP client.</param>
        /// <param name="loggerFactory">Factory to create a logger.</param>
        public AvailabilityTestFactory(TelemetryClient telemetryClient, IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory)
        {
            _telemetryClient = telemetryClient;
            _httpClientFactory = httpClientFactory;
            _loggerFactory = loggerFactory;
        }

        /// <inheritdoc/>
        public IAvailabilityTest CreateAvailabilityTest(string name, Func<Task> checkAvailabilityAsync)
        {
            return new AvailabilityTest(name, checkAvailabilityAsync, _telemetryClient);
        }

        /// <inheritdoc/>
        public IAvailabilityTest CreateAvailabilityTest(string name, string requestUri, string httpClientName)
        {
            return new HttpGetRequestAvailabilityTest(name, requestUri, _telemetryClient, _httpClientFactory, httpClientName, _loggerFactory);
        }
    }
}
