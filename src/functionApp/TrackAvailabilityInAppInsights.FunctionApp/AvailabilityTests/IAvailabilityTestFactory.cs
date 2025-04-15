namespace TrackAvailabilityInAppInsights.FunctionApp.AvailabilityTests
{
    /// <summary>
    /// Factory to create availability tests.
    /// </summary>
    public interface IAvailabilityTestFactory
    {
        /// <summary>
        /// Creates an availability test.
        /// </summary>
        /// <param name="name">The name of the availability test as it will be displayed in Application Insights.</param>
        /// <param name="checkAvailabilityAsync">The function that performs the actual availability check.</param>
        IAvailabilityTest CreateAvailabilityTest(string name, Func<Task> checkAvailabilityAsync);

        /// <summary>
        /// Creates a HTTP GET availability test that performs a GET request on the specified <paramref name="requestUri"/> 
        /// using the HTTP client with name <paramref name="httpClientName"/>.
        /// </summary>
        /// <param name="name">The name of the availability test as it will be displayed in Application Insights.</param>
        /// <param name="requestUri">The request uri to test.</param>
        /// <param name="httpClientName">The name of the HTTP client to use.</param>
        IAvailabilityTest CreateAvailabilityTest(string name, string requestUri, string httpClientName);
    }
}
