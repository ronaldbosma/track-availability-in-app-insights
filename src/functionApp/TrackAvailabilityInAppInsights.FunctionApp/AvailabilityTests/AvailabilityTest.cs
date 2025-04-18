using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using System.Diagnostics;

namespace TrackAvailabilityInAppInsights.FunctionApp.AvailabilityTests
{
    /// <summary>
    /// Generic class for availability tests.
    /// </summary>
    internal class AvailabilityTest : IAvailabilityTest
    {
        private readonly string _name;
        private readonly Func<Task> _checkAvailabilityAsync;
        private readonly TelemetryClient _telemetryClient;

        /// <summary>
        /// Creates instance of <see cref="AvailabilityTest"/>.
        /// </summary>
        /// <param name="name">The name of the availability test.</param>
        /// <param name="checkAvailability">The function that checks the availability.</param>
        /// <param name="telemetryClient">The telemetry client to publish the result to.</param>
        public AvailabilityTest(string name, Func<Task> checkAvailabilityAsync, TelemetryClient telemetryClient)
        {
            _name = name;
            _checkAvailabilityAsync = checkAvailabilityAsync;
            _telemetryClient = telemetryClient;
        }

        /// <inheritdoc/>
        public async Task ExecuteAsync()
        {
            AvailabilityTelemetry availability = new()
            {
                Name = _name,
                RunLocation = Environment.GetEnvironmentVariable("REGION_NAME") ?? "Unknown",
                Success = false
            };

            Stopwatch stopwatch = new();
            stopwatch.Start();

            try
            {
                using (Activity activity = new("AvailabilityContext"))
                {
                    activity.Start();

                    // Connect the availability telemetry to the logging activity
                    availability.Id = activity.SpanId.ToString();
                    availability.Context.Operation.ParentId = activity.ParentSpanId.ToString();
                    availability.Context.Operation.Id = activity.RootId;

                    // Set start time of availability test
                    availability.Timestamp = DateTimeOffset.UtcNow;

                    await _checkAvailabilityAsync();
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

                _telemetryClient.TrackAvailability(availability);
                _telemetryClient.Flush();
            }
        }
    }
}
