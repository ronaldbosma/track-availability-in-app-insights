using System.Net;

namespace TrackAvailabilityInAppInsights.FunctionApp.Tests.Fakes
{
    /// <summary>
    /// A fake of <see cref="HttpClient"/> that can be used to stub and mock HTTP calls.
    /// </summary>
    internal class HttpClientFake : HttpClient
    {
        private readonly HttpMessageHandlerFake _httpMessageHandlerFake;

        public HttpClientFake() : this(new HttpMessageHandlerFake())
        {
        }

        private HttpClientFake(HttpMessageHandlerFake httpMessageHandler) : base(httpMessageHandler)
        {
            _httpMessageHandlerFake = httpMessageHandler;
            BaseAddress = new Uri("https://localhost");
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
            var key = new HttpRequestKey(httpMethod, new Uri(BaseAddress!, relativeUrl));
            _httpMessageHandlerFake.StubbedResponseMessages.Add(key, responseMessage);
        }

        /// <summary>
        /// Record to uniquely identify a request message based on its HTTP method and URL.
        /// </summary>
        private record HttpRequestKey(HttpMethod Method, Uri Url);

        private class HttpMessageHandlerFake : HttpMessageHandler
        {
            /// <summary>
            /// Gets a list of request messages that were sent to this HTTP message handler.
            /// </summary>
            public IList<HttpRequestMessage> SentRequestMessages { get; init; } = [];

            /// <summary>
            /// Gets a dictionary of response messages that are stubbed for specific request URLs.
            /// </summary>
            public Dictionary<HttpRequestKey, HttpResponseMessage> StubbedResponseMessages { get; init; } = [];

            /// <inheritdoc />
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                // Store the request message for later verification.
                SentRequestMessages.Add(request);

                // Return the stubbed response message for the corresponding request URL.
                var requestKey = new HttpRequestKey(request.Method, request.RequestUri!);
                var responseMessage = StubbedResponseMessages[requestKey];
                return Task.FromResult(responseMessage);
            }
        }
    }
}