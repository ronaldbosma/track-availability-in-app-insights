using System.Net;

namespace TrackAvailabilityInAppInsights.FunctionApp.Tests.Fakes
{
    /// <summary>
    /// A fake of <see cref="HttpMessageHandler"/> that can be used to stub and mock HTTP calls.
    /// </summary>
    internal class HttpMessageHandlerFake : HttpMessageHandler
    {
        private static readonly string _baseAddress = "https://localhost";

        private record HttpRequestKey(HttpMethod Method, string RelativeUrl);

        private readonly Dictionary<HttpRequestKey, HttpResponseMessage> _responseMessages = [];

        /// <summary>
        /// Gets a list of request messages that were sent to this HTTP message handler.
        /// </summary>
        public IList<HttpRequestMessage> SentRequestMessages { get; init; } = [];

        /// <inheritdoc />
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            SentRequestMessages.Add(request);

            // Remove the base address from the request URI to get the relative URL.
            var relativeRequestUrl = request.RequestUri!.ToString().Replace(_baseAddress, "");

            // Return the stubbed response message for the corresponding request URL.
            var requestKey = new HttpRequestKey(request.Method, relativeRequestUrl);
            var responseMessage = _responseMessages[requestKey];
            return Task.FromResult(responseMessage);
        }

        /// <summary>
        /// Stubs a reponse with <paramref name="statusCode"/> for a GET request on <paramref name="relativeUrl"/>.
        /// </summary>
        public void StubResponseForGetRequest(string relativeUrl, HttpStatusCode statusCode)
        {
            StubResponseForRequest(HttpMethod.Get, relativeUrl, new HttpResponseMessage(statusCode));
        }

        /// <summary>
        /// Stubs a <paramref name="responseMessage"/> for the given combination of <paramref name="httpMethod"/> and <paramref name="relativeUrl"/>.
        /// </summary>
        public void StubResponseForRequest(HttpMethod httpMethod, string relativeUrl, HttpResponseMessage responseMessage)
        {
            var key = new HttpRequestKey(httpMethod, relativeUrl);
            _responseMessages.Add(key, responseMessage);
        }

        /// <summary>
        /// Creates an HttpClient with this fake as the message handler.
        /// </summary>
        public HttpClient CreateHttpClient()
        {
            return new HttpClient(this)
            {
                BaseAddress = new Uri(_baseAddress)
            };
        }
    }
}
