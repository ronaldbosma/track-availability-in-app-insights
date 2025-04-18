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

            AvailabilityTest sut = new(TestName, checkAvailabilityAsync, _telemetryChannelFake.CreateTelemetryClient());

            // Act
            await sut.ExecuteAsync();

            // Assert
            _telemetryChannelFake.VerifyThatSuccessfulAvailabilityIsTrackedForTest(TestName);
        }

        [TestMethod]
        public async Task ExecuteAsync_AvailabilityCheckFails_AvailabilityFailureTracked()
        {
            // Arrange
            Exception exception = new("Test exception");
            Task checkAvailabilityAsync() => throw exception;

            AvailabilityTest sut = new(TestName, checkAvailabilityAsync, _telemetryChannelFake.CreateTelemetryClient());

            // Act
            async Task act() => await sut.ExecuteAsync();

            // Assert
            Exception actualException = await Assert.ThrowsExceptionAsync<Exception>(act);
            Assert.AreEqual(exception.Message, actualException.Message);

            _telemetryChannelFake.VerifyThatFailedAvailabilityIsTrackedForTest(TestName, exception.Message);
        }
    }
}
