using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace TrackAvailabilityInAppInsights.FunctionApp.Tests.Fakes
{
    /// <summary>
    /// A fake of <see cref="ITelemetryChannel"/>.
    /// Based on <seealso href="https://github.com/microsoft/ApplicationInsights-dotnet/blob/37cec526194b833f7cd676f25eafd985dd88d3fa/Test/CoreSDK.Test/TestFramework/Shared/StubTelemetryChannel.cs"/>
    /// </summary>
    internal class TelemetryChannelFake : ITelemetryChannel
    {
        public TelemetryChannelFake()
        {
            SentItems = [];
            FlushWasCalled = false;
            EndpointAddress = "https://dc.services.visualstudio.com/v2/track";
        }

        /// <summary>
        /// Gets a list of items that was sent to this channel.
        /// </summary>
        public IList<ITelemetry> SentItems { get; init; }

        /// <summary>
        /// Gets an indication whether the <see cref="ITelemetryChannel.Flush"/> method was called.
        /// </summary>
        public bool FlushWasCalled { get; private set; }

        /// <inheritdoc />
        public bool? DeveloperMode { get; set; }

        /// <inheritdoc />
        public string EndpointAddress { get; set; }

        /// <inheritdoc />
        public void Send(ITelemetry item)
        {
            SentItems.Add(item);
        }

        /// <inheritdoc />
        public void Flush()
        {
            FlushWasCalled = true;
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }

        /// <summary>
        /// Creates a <see cref="TelemetryClient"/> that uses this channel.
        /// </summary>
        public TelemetryClient CreateTelemetryClient()
        {
            var configuration = new TelemetryConfiguration
            {
                TelemetryChannel = this
            };
            configuration.TelemetryInitializers.Add(new OperationCorrelationTelemetryInitializer());

            return new TelemetryClient(configuration);
        }

        public void VerifyThatSuccessfulAvailabilityIsTrackedForTest(string expectedTestName)
        {
            Assert.AreEqual(1, SentItems.Count);

            var item = SentItems.Single() as AvailabilityTelemetry;
            Assert.IsNotNull(item, $"Sent item should be of type {nameof(AvailabilityTelemetry)}");

            Assert.AreEqual(expectedTestName, item.Name);
            Assert.IsTrue(item.Success);
            Assert.AreNotEqual(item.Duration, TimeSpan.Zero);

            Assert.IsTrue(FlushWasCalled);
        }

        public void VerifyThatFailedAvailabilityIsTrackedForTest(string expectedTestName, string expectedExceptionMessage)
        {
            Assert.AreEqual(1, SentItems.Count);

            var item = SentItems.Single() as AvailabilityTelemetry;
            Assert.IsNotNull(item, $"Sent item should be of type {nameof(AvailabilityTelemetry)}");

            Assert.AreEqual(expectedTestName, item.Name);
            Assert.IsFalse(item.Success);
            Assert.AreEqual(expectedExceptionMessage, item.Message);
            Assert.AreNotEqual(item.Duration, TimeSpan.Zero);

            Assert.IsTrue(FlushWasCalled);
        }
    }

}
