using Microsoft.Azure.Functions.Worker;

using TrackAvailabilityInAppInsights.FunctionApp.AvailabilityTests;

namespace TrackAvailabilityInAppInsights.FunctionApp
{
    /// <summary>
    /// Function to test the availability of the Backend API.
    /// </summary>
    public class BackendStatusAvailabilityTest(IAvailabilityTestFactory availabilityTestFactory)
    {
        private const string TestName = "Azure Function - Backend API Status";
        private const string RequestUri = "/backend/status";
        private const string HttpClientName = "apim";

        [Function(nameof(BackendStatusAvailabilityTest))]
        public async Task Run([TimerTrigger("%AVAILABILITY_TESTS_SCHEDULE%")] TimerInfo timerInfo)
        {
            var availabilityTest = availabilityTestFactory.CreateAvailabilityTest(TestName, RequestUri, HttpClientName);
            await availabilityTest.ExecuteAsync();
        }
    }
}