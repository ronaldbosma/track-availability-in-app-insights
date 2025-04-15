namespace TrackAvailabilityInAppInsights.FunctionApp.Tests.Fakes
{
    /// <summary>
    /// A fake of <see cref="IHttpClientFactory"/>.
    /// </summary>
    internal class HttpClientFactoryFake : IHttpClientFactory
    {
        private readonly Dictionary<string, HttpClient> _httpClients = [];

        /// <inheritdoc />
        public HttpClient CreateClient(string name)
        {
            return _httpClients[name];
        }

        /// <summary>
        /// Adds a stubbed <see cref="HttpClient"/> to the factory with the specified name.
        /// </summary>
        /// <param name="name">The name of the HttpClient.</param>
        /// <param name="httpClient">The HttpClient instance to add.</param>
        public void StubHttpClient(string name, HttpClient httpClient)
        {
            _httpClients.Add(name, httpClient);
        }
    }
}
