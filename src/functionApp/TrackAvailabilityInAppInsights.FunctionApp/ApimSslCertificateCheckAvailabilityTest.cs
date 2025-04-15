using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using System.Configuration;
using TrackAvailabilityInAppInsights.FunctionApp.AvailabilityTests;

namespace TrackAvailabilityInAppInsights.FunctionApp
{
    /// <summary>
    /// Function to test if the SSL certificate of API Management nearly expires/has expired.
    /// </summary>
    public class ApimSslCertificateCheckAvailabilityTest(IAvailabilityTestFactory availabilityTestFactory, IConfiguration configuration, SslCertificateValidator sslCertificateValidator)
    {
        private const string TestName = "Azure Function - API Management SSL Certificate Check";
        private const string RequestUri = "/internal-status-0123456789abcdef";

        [Function(nameof(ApimSslCertificateCheckAvailabilityTest))]
        public async Task Run([TimerTrigger("0 * * * * *")] TimerInfo timerInfo)
        {
            var availabilityTest = availabilityTestFactory.CreateAvailabilityTest(TestName, CheckSslCertificateAsync);
            await availabilityTest.ExecuteAsync();
        }

        private async Task CheckSslCertificateAsync()
        {
            var apimBaseUrl = new Uri(configuration["ApiManagement_gatewayUrl"] ?? throw new ConfigurationErrorsException("Setting ApiManagement_gatewayUrl not specified"));
            using var handler = new HttpClientHandler() { ServerCertificateCustomValidationCallback = sslCertificateValidator.Validate };
            using var client = new HttpClient(handler) { BaseAddress = apimBaseUrl };
            
            await client.GetAsync(RequestUri);
        }
    }
}
