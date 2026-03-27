using System.Diagnostics;

using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;

using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace TrackAvailabilityInAppInsights.FunctionApp.Tests.Fakes;

/// <summary>
/// A fake of <see cref="TelemetryClient"/> that can be used to mock telemetry calls.
/// </summary>
/// <remarks>
/// Based on <seealso href="https://github.com/microsoft/ApplicationInsights-dotnet/blob/main/BASE/Test/Microsoft.ApplicationInsights.Test/Microsoft.ApplicationInsights.Tests/TelemetryClientTest.cs"/>
/// </remarks>
internal class TelemetryClientFake
{
    private readonly List<LogRecord> _logItems = [];
    private readonly List<OpenTelemetry.Metrics.Metric> _metricItems = [];
    private readonly List<Activity> _activityItems = [];

    public TelemetryClientFake()
    {
        var configuration = new TelemetryConfiguration
        {
            ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000000"
        };
        configuration.ConfigureOpenTelemetryBuilder(b => b
            .WithLogging(l => l.AddInMemoryExporter(_logItems))
            .WithMetrics(m => m.AddInMemoryExporter(_metricItems))
            .WithTracing(t => t.AddInMemoryExporter(_activityItems)));

        // TelemetryClient is sealed, so we can't inherit from it. Instead, we expose it as a property for use in calling code.
        Value = new(configuration);
    }

    /// <summary>
    /// Gets the faked <see cref="TelemetryClient"/>.
    /// </summary>
    public TelemetryClient Value { get; init; }

    public void VerifyThatSuccessfulAvailabilityIsTrackedForTest(string expectedTestName)
    {
        VerifyThatAvailabilityIsTrackedForTest(expectedTestName, expectedSuccess: true);
    }

    public void VerifyThatFailedAvailabilityIsTrackedForTest(string expectedTestName, string expectedExceptionMessage)
    {
        var logItem = VerifyThatAvailabilityIsTrackedForTest(expectedTestName, expectedSuccess: false);

        AssertHasAttributeWithValue(logItem.Attributes!, "microsoft.availability.message", expectedExceptionMessage);
        Assert.AreEqual(expectedExceptionMessage, logItem.FormattedMessage);
    }

    private LogRecord VerifyThatAvailabilityIsTrackedForTest(string expectedTestName, bool expectedSuccess)
    {
        Assert.HasCount(1, _logItems);

        var logItem = _logItems[0];
        Assert.IsNotNull(logItem.Attributes);
        
        AssertHasAttributeWithValue(logItem.Attributes, "microsoft.availability.name", expectedTestName);
        AssertHasAttributeWithValue(logItem.Attributes, "microsoft.availability.success", expectedSuccess.ToString());

        var duration = GetAttribute(logItem.Attributes, "microsoft.availability.duration");
        Assert.AreNotEqual(duration.Value, TimeSpan.Zero.ToString());

        return logItem;
    }

    private static void AssertHasAttributeWithValue(IReadOnlyList<KeyValuePair<string, object?>> attributes, string key, string expectedValue)
    {
        var attribute = GetAttribute(attributes, key);
        Assert.AreEqual(expectedValue, attribute.Value, $"Unexpected value for attribute with key '{key}'");
    }

    private static KeyValuePair<string, object?> GetAttribute(IReadOnlyList<KeyValuePair<string, object?>> attributes, string key)
    {
        var attribute = attributes.SingleOrDefault(a => a.Key == key);
        Assert.IsNotNull(attribute.Key, $"Attribute with key '{key}' not found");

        return attribute;
    }
}