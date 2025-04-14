using Microsoft.Azure.Functions.Worker;
using TrackAvailabilityInAppInsights.FunctionApp.AvailabilityTests;

namespace TrackAvailabilityInAppInsights.FunctionApp
{
    /// <summary>
    /// Function to test the availability of the Backend API.
    /// </summary>
    public class BackendStatusAvailabilityTest
    {
        private readonly IAvailabilityTestFactory _availabilityTestFactory;

        public BackendStatusAvailabilityTest(IAvailabilityTestFactory availabilityTestFactory)
        {
            _availabilityTestFactory = availabilityTestFactory;
        }

        [Function(nameof(BackendStatusAvailabilityTest))]
        public async Task Run([TimerTrigger("0 * * * * *")] TimerInfo timerInfo)
        {
            const string TestName = "Backend API Status Test";
            const string RequestUri = "/backend/status";
            const string HttpClientName = "apim";

            var availabilityTest = _availabilityTestFactory.CreateAvailabilityTest(TestName, RequestUri, HttpClientName);
            await availabilityTest.ExecuteAsync();
        }
    }

}
