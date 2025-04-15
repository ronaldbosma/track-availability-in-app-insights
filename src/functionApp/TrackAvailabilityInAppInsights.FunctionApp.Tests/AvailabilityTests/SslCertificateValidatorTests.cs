using Microsoft.Extensions.Logging.Abstractions;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using TrackAvailabilityInAppInsights.FunctionApp.AvailabilityTests;

namespace TrackAvailabilityInAppInsights.FunctionApp.Tests.AvailabilityTests
{
    [TestClass]
    public class SslCertificateValidatorTests
    {
        // This is a valid certificate that won't expire for a long time (2125-04-15)
        private static readonly X509Certificate2 ValidCertificate = X509CertificateLoader.LoadCertificate(Convert.FromBase64String("MIIDRDCCAiygAwIBAgIQRXQkpUZjV7xDHZtqTdc+KTANBgkqhkiG9w0BAQsFADApMScwJQYDVQQDDB5BdXRvbWF0ZWQgVGVzdCBJbnRlcm1lZGlhdGUgQ0EwIBcNMjUwNDE1MTUzNjIyWhgPMjEyNTA0MTUxNTQ2MjJaMBMxETAPBgNVBAMMCElzIFZhbGlkMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAzNUgbW8bItjYGpsaQso5cYV43Na5ZQkZN2mTQxMc4Fks6bYG4AA/vpPs2gpLFvxiVq1F/3syarta+6weRswXkgCaRxKtVU2l6XsN0UxEJJW2dBf+XVzgkaGhSpwyGwrEtE6dM6F4Ows5AaEr6j1uP1ev66o1eb+0J6UkbiuXHauUkcVyRdV9Ob/IhMViPp6UPMppcIb4IzvJF0z2upFQ3C1/hLnwJ3x2Kh5Qx3KXcni/XVmCSVziSaup+eHJgELFzZ1rZXmzuSGEpEevh2b5s/tK4TYRwqG8TslDoJP+BZuAb1EGA+qEFd1acd5+jjD4dhaYCOEy8Cns9I9DD2+dyQIDAQABo3wwejAOBgNVHQ8BAf8EBAMCBaAwEwYDVR0RBAwwCoIISXMgVmFsaWQwEwYDVR0lBAwwCgYIKwYBBQUHAwIwHwYDVR0jBBgwFoAUrcrDUOu0Wp4MS1F9AzXmwJK/mrIwHQYDVR0OBBYEFO3vws7zLZ+lo6Y/HcpJ3tvp6coLMA0GCSqGSIb3DQEBCwUAA4IBAQB6YB+mbRb43iSAZ4SEQx/prt9bPDpOXT8KbC4GZsOIg/ZU2qPKPEYrdpk4CGFqBC5MuSizZao5V773gpFXQRT71L8RmwIbxDUyw7UiO3MTJ4vDx9enerKZsxtiuEmYtwP37vLROk8DRivk/CAp793TpmkgwjHD95hovUBXYkz19pKFGeoeCt13b91ViuJDFGP3KYoX0tmjR7Fh4afDjh663erZiLcGFEqza1fx8rEOwm5DwFoaaKEaaogt1UgyfUoxaLBxOCKSTO3Jz0dRnUyEwGR7vTYh3A98C13o2kxdChaHiEiMy8Qw9Q9ea2nSmbbRZAHQFQ4VkVRvUXSA2P8q"));

        private readonly SslCertificateValidator _sut = new(new NullLoggerFactory());

        [TestMethod]
        public void Validate_ValidCertificate_ReturnsTrue()
        {
            // Act
            var result = _sut.Validate(this, ValidCertificate, null, SslPolicyErrors.None);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void Validate_CertificateIsNull_ReturnsFalse()
        {
            // Arrange
            X509Certificate? certificate = null;

            // Act
            var result = _sut.Validate(this, certificate, null, SslPolicyErrors.None);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void Validate_CertificateExpired_ReturnsFalse()
        {
            // Arrange
            var clientCertificateBytes = Convert.FromBase64String("MIIDRjCCAi6gAwIBAgIQXkJkMA5/prlDcsH2OJqWszANBgkqhkiG9w0BAQsFADApMScwJQYDVQQDDB5BdXRvbWF0ZWQgVGVzdCBJbnRlcm1lZGlhdGUgQ0EwHhcNMjUwNDE1MTUzNjIyWhcNMjUwNDE2MTU0NjIyWjAVMRMwEQYDVQQDDApJcyBFeHBpcmVkMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA6KQEZO+HZgrZ4OnU/1F0CyjogM7e2AXnjxAqjzcMsrpruZKP4Ax58Ogz39/uIzsMbFXqK5rJSFojIHZvt3v9BRDeps4zpLbjw3+BcnP5oPM6KOlojJl/ZyVnRu9gwPXwwSUlv4GOPZbViQZhjkq4NZawKlmE/UfNUN2mVAaBBpCMezOamQZLjWloguyPQjy2nvf1yq5R/ftzdysT1uWprnrb2z7HjdNfoy4ks+mlqMTmr6V1LqQwqIVOVmimWKIvnObFvnLgKS8D7voz2VhaCurYUW9JrPyWN7sMYvg5c6NM+TTQ5oyRPuE8dNVl5Tc1GLwBsBtAjpd0M6rIDSPcJQIDAQABo34wfDAOBgNVHQ8BAf8EBAMCBaAwFQYDVR0RBA4wDIIKSXMgRXhwaXJlZDATBgNVHSUEDDAKBggrBgEFBQcDAjAfBgNVHSMEGDAWgBStysNQ67RangxLUX0DNebAkr+asjAdBgNVHQ4EFgQU7JVbv98ijioOELZjKKXUGTithTgwDQYJKoZIhvcNAQELBQADggEBAGfipB9Pe1DW6qyaUomEDzRzbP8xxSgn/yFwwno7aXnBcffAXhxkaNRykJC0aapYPa8arpLDQaamfguiwK9hN/52Dk2kbFAc4VYrX7vsno+KZMM+pUFH6e+4JJlBYieaMczSBdEp7VnBSxYWh11d1sQv+O9aVewmxe90flq5G8RL7zEdo6Sap1oudXp21sRRzGYl7+qnbs+QBvAt3cz02GWTGNZcAyC9opn0BwbRRU8Mri1Q1Q7ExLjcatYQLpXeN+hrCddmrQB6QgPlCH335CaETLbxRi/+OgcyC3jemwDBl9vXW7r10IYQwwVjySxlPc0XjWtnulnlXKvbkazDAwU=");
            var expiredCertificate = X509CertificateLoader.LoadCertificate(clientCertificateBytes);

            // Act
            var result = _sut.Validate(this, expiredCertificate, null, SslPolicyErrors.None);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void Validate_HasPolicyErrors_ReturnsFalse()
        {
            // Arrange
            SslPolicyErrors policyErrors = SslPolicyErrors.RemoteCertificateChainErrors;

            // Act
            var result = _sut.Validate(this, ValidCertificate, null, policyErrors);

            // Assert
            Assert.IsFalse(result);
        }
    }
}
