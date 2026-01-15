using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Options;
using TrackAvailabilityInAppInsights.FunctionApp.AvailabilityTests;

namespace TrackAvailabilityInAppInsights.FunctionApp
{
    /// <summary>
    /// Function to test if the SSL certificate of API Management nearly expires/has expired.
    /// </summary>
    public class ApimSslCertificateCheckAvailabilityTest(IAvailabilityTestFactory availabilityTestFactory, IOptions<ApiManagementOptions> apimOptions, SslCertificateValidator sslCertificateValidator)
    {
        private const string TestName = "Azure Function - API Management SSL Certificate Check";

        [Function(nameof(ApimSslCertificateCheckAvailabilityTest))]
        public async Task Run([TimerTrigger("0 * * * * *")] TimerInfo timerInfo)
        {
            var availabilityTest = availabilityTestFactory.CreateAvailabilityTest(TestName, CheckSslCertificateAsync);
            await availabilityTest.ExecuteAsync();
        }

        private async Task CheckSslCertificateAsync()
        {
            using HttpClientHandler handler = new() { ServerCertificateCustomValidationCallback = sslCertificateValidator.Validate };
            using HttpClient client = new(handler) { BaseAddress = new Uri(apimOptions.Value.GatewayUrl) };

            await client.GetAsync(apimOptions.Value.StatusEndpoint);
        }
    }
}
