using TrackAvailabilityInAppInsights.FunctionApp.AvailabilityTests;
using TrackAvailabilityInAppInsights.FunctionApp.Tests.Fakes;

namespace TrackAvailabilityInAppInsights.FunctionApp.Tests.AvailabilityTests
{
    [TestClass]
    public class AvailabilityTestTests
    {
        private const string TestName = "A.Test";

        private readonly TelemetryChannelFake _telemetryChannelFake = new();

        [TestMethod]
        public async Task ExecuteAsync_AvailabilityCheckSucceeds_AvailabilitySuccessTracked()
        {
            // Arrange
            static Task checkAvailabilityAsync() => Task.CompletedTask;

            var sut = new AvailabilityTest(TestName, checkAvailabilityAsync, _telemetryChannelFake.CreateTelemetryClient());

            // Act
            await sut.ExecuteAsync();

            // Assert
            _telemetryChannelFake.VerifyThatSuccessfulAvailabilityIsTrackedForTest(TestName);
        }

        [TestMethod]
        public async Task ExecuteAsync_AvailabilityCheckFails_AvailabilityFailureTracked()
        {
            // Arrange
            var exception = new Exception("Test exception");
            Task checkAvailabilityAsync() => throw exception;

            var subject = new AvailabilityTest(TestName, checkAvailabilityAsync, _telemetryChannelFake.CreateTelemetryClient());

            // Act
            async Task act() => await subject.ExecuteAsync();

            // Assert
            var ex = await Assert.ThrowsExceptionAsync<Exception>(act);
            Assert.AreEqual(exception.Message, ex.Message);

            _telemetryChannelFake.VerifyThatFailedAvailabilityIsTrackedForTest(TestName, exception.Message);
        }
    }

}
