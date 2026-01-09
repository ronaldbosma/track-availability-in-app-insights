using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using System.Diagnostics;

namespace TrackAvailabilityInAppInsights.FunctionApp.AvailabilityTests
{
    /// <summary>
    /// Generic class for availability tests.
    /// </summary>
    /// <param name="name">The name of the availability test.</param>
    /// <param name="checkAvailability">The function that checks the availability.</param>
    /// <param name="telemetryClient">The telemetry client to publish the result to.</param>
    internal class AvailabilityTest(string name, Func<Task> checkAvailabilityAsync, TelemetryClient telemetryClient) : IAvailabilityTest
    {
        /// <inheritdoc/>
        public async Task ExecuteAsync()
        {
            AvailabilityTelemetry availability = new()
            {
                Name = name,
                RunLocation = Environment.GetEnvironmentVariable("REGION_NAME") ?? "Unknown",
                Success = false,
                // Sets the start time of availability test
                Timestamp = DateTimeOffset.UtcNow
            };

            Stopwatch stopwatch = new();
            stopwatch.Start();

            try
            {
                // Create activity to enable distributed tracing and correlation of the telemetry in App Insights
                using (Activity activity = new("AvailabilityContext"))
                {
                    activity.Start();

                    // Connect the availability telemetry to the logging activity
                    availability.Id = activity.SpanId.ToString();
                    availability.Context.Operation.ParentId = activity.ParentSpanId.ToString();
                    availability.Context.Operation.Id = activity.RootId;

                    await checkAvailabilityAsync();
                }

                availability.Success = true;
            }
            catch (Exception ex)
            {
                availability.Message = ex.Message;
                availability.Properties.Add("Exception", ex.ToString());
                throw;
            }
            finally
            {
                stopwatch.Stop();
                availability.Duration = stopwatch.Elapsed;

                telemetryClient.TrackAvailability(availability);
                telemetryClient.Flush();
            }
        }
    }
}
