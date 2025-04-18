using TrackAvailabilityInAppInsights.FunctionApp.AvailabilityTests;
using TrackAvailabilityInAppInsights.FunctionApp.Tests.Fakes;

namespace TrackAvailabilityInAppInsights.FunctionApp.Tests.AvailabilityTests
{
    [TestClass]
    public class AvailabilityTestTests
    {
        private const string TestName = "A.Test";

        private readonly TelemetryClientFake _telemetryClientFake = new();

        [TestMethod]
        public async Task ExecuteAsync_AvailabilityCheckSucceeds_AvailabilitySuccessTracked()
        {
            // Arrange
            static Task checkAvailabilityAsync() => Task.CompletedTask;

            AvailabilityTest sut = new(TestName, checkAvailabilityAsync, _telemetryClientFake.Value);

            // Act
            await sut.ExecuteAsync();

            // Assert
            _telemetryClientFake.VerifyThatSuccessfulAvailabilityIsTrackedForTest(TestName);
        }

        [TestMethod]
        public async Task ExecuteAsync_AvailabilityCheckFails_AvailabilityFailureTracked()
        {
            // Arrange
            Exception exception = new("Test exception");
            Task checkAvailabilityAsync() => throw exception;

            AvailabilityTest sut = new(TestName, checkAvailabilityAsync, _telemetryClientFake.Value);

            // Act
            async Task act() => await sut.ExecuteAsync();

            // Assert
            Exception actualException = await Assert.ThrowsExceptionAsync<Exception>(act);
            Assert.AreEqual(exception.Message, actualException.Message);

            _telemetryClientFake.VerifyThatFailedAvailabilityIsTrackedForTest(TestName, exception.Message);
        }
    }
}
