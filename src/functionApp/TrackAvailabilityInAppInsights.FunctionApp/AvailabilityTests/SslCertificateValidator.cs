using Microsoft.Extensions.Logging;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace TrackAvailabilityInAppInsights.FunctionApp.AvailabilityTests
{
    /// <summary>
    /// Class to validate SSL certificates.
    /// </summary>
    public class SslCertificateValidator(ILoggerFactory loggerFactory)
    {
        private const int AllowedAmountOfDaysUntilCertificateExpiration = 30;

        private readonly ILogger _logger = loggerFactory.CreateLogger<SslCertificateValidator>();

        /// <summary>
        /// Validates that the remote certificate is valid and won't expire within the specified number of days.
        /// </summary>
        /// <remarks>
        /// This method can be used as a callback for the <see cref="HttpClientHandler.ServerCertificateCustomValidationCallback"/> property.
        /// </remarks>
        /// <returns>False if the certificate expired or is about to expire, else true.</returns>
        public bool Validate(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors policyErrors)
        {
            if (certificate is null)
            {
                _logger.LogError("Could not find an SSL server certificate associated to the endpoint");
                return false;
            }

            X509Certificate2 certificate2 = new(certificate);
            if (certificate2.NotAfter <= DateTime.Now.AddDays(AllowedAmountOfDaysUntilCertificateExpiration))
            {
                _logger.LogError("The SSL server certificate is close to its expiration date of: {ExpirationDate}", certificate2.NotAfter);
                return false;
            }

            if (policyErrors != SslPolicyErrors.None)
            {
                _logger.LogError("The SSL server certificate was not valid, due to the following policy errors: [{PolicyErrors}]", policyErrors);
                return false;
            }

            return true;
        }
    }
}
