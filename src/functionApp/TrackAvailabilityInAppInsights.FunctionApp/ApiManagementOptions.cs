using System.ComponentModel.DataAnnotations;

namespace TrackAvailabilityInAppInsights.FunctionApp
{
    public class ApiManagementOptions
    {
        public const string SectionKey = "ApiManagement";

        [Required(AllowEmptyStrings = false)]
        [Url]
        public string GatewayUrl { get; set; } = string.Empty;

        [Required(AllowEmptyStrings = false)]
        public string SubscriptionKey { get; set; } = string.Empty;

        [Required(AllowEmptyStrings = false)]
        public string StatusEndpoint { get; set; } = string.Empty;
    }
}
