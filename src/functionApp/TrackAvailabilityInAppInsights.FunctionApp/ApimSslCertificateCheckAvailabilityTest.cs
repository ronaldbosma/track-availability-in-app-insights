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

        [Function(nameof(ApimSslCertificateCheckAvailabilityTest))]
        public async Task Run([TimerTrigger("0 * * * * *")] TimerInfo timerInfo)
        {
            var availabilityTest = availabilityTestFactory.CreateAvailabilityTest(TestName, CheckSslCertificateAsync);
            await availabilityTest.ExecuteAsync();
        }

        private async Task CheckSslCertificateAsync()
        {
            Uri apimBaseUrl = new(configuration["ApiManagement_gatewayUrl"] ?? throw new ConfigurationErrorsException("Setting ApiManagement_gatewayUrl not specified"));
            using HttpClientHandler handler = new() { ServerCertificateCustomValidationCallback = sslCertificateValidator.Validate };
            using HttpClient client = new(handler) { BaseAddress = apimBaseUrl };

            string apimStatusEndpoint = configuration["ApiManagement_statusEndpoint"] ?? throw new ConfigurationErrorsException("Setting ApiManagement_statusEndpoint not specified");
            await client.GetAsync(apimStatusEndpoint);
        }
    }
}
