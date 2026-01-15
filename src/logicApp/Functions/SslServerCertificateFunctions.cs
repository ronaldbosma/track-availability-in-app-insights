namespace TrackAvailabilityInAppInsights.LogicApp.Functions
{
    using System;
    using System.Net.Security;
    using System.Net.Sockets;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;
    using Microsoft.Azure.Functions.Extensions.Workflows;
    using Microsoft.Azure.Functions.Worker;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Contains functions related to SSL server certificates.
    /// </summary>
    public class SslServerCertificateFunctions
    {
        private readonly ILogger<SslServerCertificateFunctions> _logger;

        public SslServerCertificateFunctions(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<SslServerCertificateFunctions>();
        }

        [Function("GetSslServerCertificateExpirationInDays")]
        public async Task<int> GetSslServerCertificateExpirationInDays([WorkflowActionTrigger] string hostname, int port)
        {
            _logger.LogInformation("GetSslServerCertificateExpirationInDays function invoked with hostname: {Hostname}, port: {Port}", hostname, port);
            
            // Connect client to remote TCP host using provided hostname and port
            using var tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(hostname, port);

            // Create an SSL stream over the TCP connection and authenticate with the server
            // This will trigger the SSL/TLS handshake and allow us to access the server's certificate
            using var sslStream = new SslStream(tcpClient.GetStream());
            await sslStream.AuthenticateAsClientAsync(hostname);

            // Retrieve the remote server's SSL certificate from the authenticated connection
            var certificate = sslStream.RemoteCertificate;
            if (certificate != null)
            {
                // Calculate the remaining lifetime of the certificate in days
                var x509Certificate = new X509Certificate2(certificate);
                return (x509Certificate.NotAfter - DateTime.UtcNow).Days;
            }

            // Throw an exception if no certificate was found
            throw new Exception($"No SSL server certificate found for host {hostname} on port {port}. Unable to determine expiration.");
        }
    }
}