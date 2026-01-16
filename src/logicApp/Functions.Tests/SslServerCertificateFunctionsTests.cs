using Microsoft.Extensions.Logging.Abstractions;

namespace TrackAvailabilityInAppInsights.LogicApp.Functions.Tests
{
    [TestClass]
    public sealed class SslServerCertificateFunctionsTests
    {
        private readonly SslServerCertificateFunctions _sut = new SslServerCertificateFunctions(new NullLoggerFactory());

        [TestMethod]
        public async Task GetSslServerCertificateExpirationInDays_ValidHttpsEndpoint_ReturnsPositiveDays()
        {
            // Arrange
            string hostname = "www.microsoft.com";
            int port = 443;

            // Act
            int daysUntilExpiration = await _sut.GetSslServerCertificateExpirationInDays(hostname, port);

            // Assert
            Assert.IsGreaterThan(0, daysUntilExpiration, "Certificate should not be expired");
        }

        [TestMethod]
        public async Task GetSslServerCertificateExpirationInDays_InvalidHostname_ThrowsException()
        {
            // Arrange
            string hostname = "invalid.hostname.that.does.not.exist.example";
            int port = 443;

            // Act
            async Task act() => await _sut.GetSslServerCertificateExpirationInDays(hostname, port);

            // Assert
            Exception actualException = await Assert.ThrowsAsync<Exception>(act);
            Assert.AreEqual("No such host is known.", actualException.Message);
        }
    }
}