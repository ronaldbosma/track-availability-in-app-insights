using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Logging;

namespace TrackAvailabilityInAppInsights.FunctionApp.AvailabilityTests
{
    /// <summary>
    /// Factory to create availability tests.
    /// </summary>
    /// <param name="telemetryClient">The telemetry client to publish the availability result to.</param>
    /// <param name="httpClientFactory">Factory to create an HTTP client.</param>
    /// <param name="loggerFactory">Factory to create a logger.</param>
    internal class AvailabilityTestFactory(TelemetryClient telemetryClient, IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory) : IAvailabilityTestFactory
    {
        /// <inheritdoc/>
        public IAvailabilityTest CreateAvailabilityTest(string name, Func<Task> checkAvailabilityAsync)
        {
            return new AvailabilityTest(name, checkAvailabilityAsync, telemetryClient);
        }

        /// <inheritdoc/>
        public IAvailabilityTest CreateAvailabilityTest(string name, string requestUri, string httpClientName)
        {
            return new HttpGetRequestAvailabilityTest(name, requestUri, telemetryClient, httpClientFactory, httpClientName, loggerFactory);
        }
    }
}
