namespace TrackAvailabilityInAppInsights.LogicApp.Workflows.Tests;

[TestClass]
public class EpicFailure
{
    [TestMethod]
    public void TestThatShouldFail()
    {
        Assert.Fail("This test should fail to test how workflow handles this");
    }
}
