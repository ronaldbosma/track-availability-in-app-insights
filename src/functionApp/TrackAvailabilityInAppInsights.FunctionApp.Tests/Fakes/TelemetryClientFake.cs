using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace TrackAvailabilityInAppInsights.FunctionApp.Tests.Fakes
{
    /// <summary>
    /// A fake of <see cref="TelemetryClient"/> that can be used to mock telemetry calls.
    /// </summary>
    /// <remarks>
    /// Based on <seealso href="https://github.com/microsoft/ApplicationInsights-dotnet/blob/37cec526194b833f7cd676f25eafd985dd88d3fa/Test/CoreSDK.Test/TestFramework/Shared/StubTelemetryChannel.cs"/>
    /// </remarks>
    internal class TelemetryClientFake
    {
        private readonly TelemetryChannelFake _telemetryChannelFake = new();

        public TelemetryClientFake()
        {
            TelemetryConfiguration configuration = new()
            {
                TelemetryChannel = _telemetryChannelFake
            };
            configuration.TelemetryInitializers.Add(new OperationCorrelationTelemetryInitializer());

            Value = new(configuration);
        }

        /// <summary>
        /// Gets the faked <see cref="Value"/>.
        /// </summary>
        public TelemetryClient Value { get; init; }

        public void VerifyThatSuccessfulAvailabilityIsTrackedForTest(string expectedTestName)
        {
            Assert.AreEqual(1, _telemetryChannelFake.SentItems.Count);

            var item = _telemetryChannelFake.SentItems.Single() as AvailabilityTelemetry;
            Assert.IsNotNull(item, $"Sent item should be of type {nameof(AvailabilityTelemetry)}");

            Assert.AreEqual(expectedTestName, item.Name);
            Assert.IsTrue(item.Success);
            Assert.AreNotEqual(item.Duration, TimeSpan.Zero);

            Assert.IsTrue(_telemetryChannelFake.FlushWasCalled);
        }

        public void VerifyThatFailedAvailabilityIsTrackedForTest(string expectedTestName, string expectedExceptionMessage)
        {
            Assert.AreEqual(1, _telemetryChannelFake.SentItems.Count);

            var item = _telemetryChannelFake.SentItems.Single() as AvailabilityTelemetry;
            Assert.IsNotNull(item, $"Sent item should be of type {nameof(AvailabilityTelemetry)}");

            Assert.AreEqual(expectedTestName, item.Name);
            Assert.IsFalse(item.Success);
            Assert.AreEqual(expectedExceptionMessage, item.Message);
            Assert.AreNotEqual(item.Duration, TimeSpan.Zero);

            Assert.IsTrue(_telemetryChannelFake.FlushWasCalled);
        }

        /// <summary>
        /// Fake implementation of <see cref="ITelemetryChannel"/> that will intercept the telemetry requests.
        /// </summary>
        internal class TelemetryChannelFake : ITelemetryChannel
        {
            /// <summary>
            /// Gets a list of items that was sent to this channel.
            /// </summary>
            public IList<ITelemetry> SentItems { get; init; } = [];

            /// <summary>
            /// Gets an indication whether the <see cref="ITelemetryChannel.Flush"/> method was called.
            /// </summary>
            public bool FlushWasCalled { get; private set; } = false;

            public bool? DeveloperMode { get; set; }

            public string EndpointAddress { get; set; } = "https://dc.services.visualstudio.com/v2/track";

            public void Send(ITelemetry item)
            {
                SentItems.Add(item);
            }

            public void Flush()
            {
                FlushWasCalled = true;
            }

            public void Dispose()
            {
            }
        }
    }
}
