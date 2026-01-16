using Microsoft.Extensions.Logging.Abstractions;
using TrackAvailabilityInAppInsights.LogicApp.Functions.Tests.Fakes;

namespace TrackAvailabilityInAppInsights.LogicApp.Functions.Tests
{
    [TestClass]
    public sealed class AvailabilityTestFunctionsTests
    {
        private readonly TelemetryClientFake _telemetryClientFake;
        private readonly AvailabilityTestFunctions _sut;

        public AvailabilityTestFunctionsTests()
        {
            _telemetryClientFake = new();
            _sut = new AvailabilityTestFunctions(_telemetryClientFake.Value, new NullLoggerFactory());
        }
        
        [TestMethod]
        public async Task TrackIsAvailable_TestNameIsNull_ArgumentNullExceptionThrown()
        {
            // Arrange
            string? testName = null;
            DateTimeOffset startTime = DateTimeOffset.UtcNow.AddSeconds(-10);

            // Act
            async Task act() => await _sut.TrackIsAvailable(testName!, startTime);

            // Assert
            await Assert.ThrowsAsync<ArgumentNullException>(act);
        }

        [TestMethod]
        public async Task TrackIsAvailable_ValidParameters_AvailabilitySuccessTracked()
        {
            // Arrange
            string testName = "A.Test";
            DateTimeOffset startTime = DateTimeOffset.UtcNow.AddSeconds(-10);

            // Act
            await _sut.TrackIsAvailable(testName, startTime);

            // Assert
            _telemetryClientFake.VerifyThatSuccessfulAvailabilityIsTrackedForTest(testName);
        }
        
        [TestMethod]
        public async Task TrackIsUnavailable_TestNameIsNull_ArgumentNullExceptionThrown()
        {
            // Arrange
            string? testName = null;
            DateTimeOffset startTime = DateTimeOffset.UtcNow.AddSeconds(-10);
            string message = "Service is down";

            // Act
            async Task act() => await _sut.TrackIsUnavailable(testName!, startTime, message);

            // Assert
            await Assert.ThrowsAsync<ArgumentNullException>(act);
        }

        [TestMethod]
        public async Task TrackIsUnavailable_ValidParameters_AvailabilityFailureTracked()
        {
            // Arrange
            string testName = "A.Test";
            DateTimeOffset startTime = DateTimeOffset.UtcNow.AddSeconds(-10);
            string message = "Service is down";

            // Act
            await _sut.TrackIsUnavailable(testName, startTime, message);

            // Assert
            _telemetryClientFake.VerifyThatFailedAvailabilityIsTrackedForTest(testName, message);
        }
    }
}
