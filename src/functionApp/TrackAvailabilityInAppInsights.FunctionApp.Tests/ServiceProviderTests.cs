using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TrackAvailabilityInAppInsights.FunctionApp.Tests
{
    [TestClass]
    public class ServiceProviderTests
    {
        /// <summary>
        /// This test will run for each Azure Function and check if its class can be resolved.
        /// If it fails, it will most likely mean that a dependency registration is missing.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(GetAzureFunctionClasses))]
        public void GetService_AllDependenciesAreRegistered_FunctionIsResolved(Type expectedAzureFunctionClass)
        {
            // Arrange
            ServiceCollection services = new();

            // Create a configuration with the required settings
            ConfigurationManager configuration = new();
            configuration["ApiManagement_gatewayUrl"] = "https://example.com";
            configuration["ApiManagement_subscriptionKey"] = "abcdefg1234567890";
            services.AddSingleton<IConfiguration>(configuration);

            // Register the Azure Function App depencencies
            services.RegisterDependencies(configuration);

            // The Azure Function class is automatically registered when the function runtime is running.
            // For the test, we need to register the Azure Function class explicitly.
            services.AddTransient(expectedAzureFunctionClass);

            var sut = services.BuildServiceProvider();

            // Act
            var result = sut.GetService(expectedAzureFunctionClass);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, expectedAzureFunctionClass);
        }

        /// <summary>
        /// Helper method to select all Azure Functions (which are classes that have a method with the Function attribute).
        /// </summary>
        public static IEnumerable<object[]> GetAzureFunctionClasses()
        {
            var functionClasses = typeof(Program).Assembly.GetTypes()
                .Where(t => 
                    t.IsClass && 
                    t.GetMethods().Any(m => m.GetCustomAttributes(typeof(FunctionAttribute), false).Length > 0)
                );

            foreach (var type in functionClasses)
            {
                yield return new object[] { type };
            }
        }
    }
}
