using Microsoft.Extensions.Logging.Abstractions;
using TrackAvailabilityInAppInsights.FunctionApp.AvailabilityTests;
using TrackAvailabilityInAppInsights.FunctionApp.Tests.Fakes;

namespace TrackAvailabilityInAppInsights.FunctionApp.Tests.AvailabilityTests
{
    [TestClass]
    public class AvailabilityTestFactoryTests
    {
        [TestMethod]
        public void CreateAvailabilityTest_SpecifyTestNameAndCheckFunction_AvailabilityTestReturned()
        {
            //Arrange
            var sut = new AvailabilityTestFactory(new TelemetryChannelFake().CreateTelemetryClient(), new HttpClientFactoryFake(), new NullLoggerFactory());

            //Act
            var result = sut.CreateAvailabilityTest("A Test Name", () => Task.CompletedTask);

            //Assert
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType<AvailabilityTest>(result);
        }

        [TestMethod]
        public void CreateAvailabilityTest_SpecifyTestNameAndRequestUrlAndClientName_HttpGetRequestAvailabilityTestReturned()
        {
            //Arrange
            var sut = new AvailabilityTestFactory(new TelemetryChannelFake().CreateTelemetryClient(), new HttpClientFactoryFake(), new NullLoggerFactory());

            //Act
            var result = sut.CreateAvailabilityTest("A Test Name", "/health", "A Client Name");

            //Assert
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType<HttpGetRequestAvailabilityTest>(result);
        }
    }
}
