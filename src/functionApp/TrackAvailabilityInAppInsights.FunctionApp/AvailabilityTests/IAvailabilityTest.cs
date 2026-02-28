namespace TrackAvailabilityInAppInsights.FunctionApp.AvailabilityTests
{
    public interface IAvailabilityTest
    {
        /// <summary>
        /// Executes the availability test.
        /// </summary>
        public Task ExecuteAsync();
    }
}