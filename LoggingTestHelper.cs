using Microsoft.Extensions.Logging;

namespace SubExplore
{
    /// <summary>
    /// Helper class to test logging configuration and ensure spot addition issues are visible
    /// </summary>
    public static class LoggingTestHelper
    {
        public static void TestLogLevels(ILogger logger)
        {
            // These should now be filtered out (too verbose)
            logger.LogTrace("This trace message should be filtered out");
            logger.LogDebug("This debug message should be filtered out");
            logger.LogInformation("This info message should be filtered out");
            
            // These should be visible (important for spot addition issues)
            logger.LogWarning("SPOT_ADD_WARNING: This warning should be visible");
            logger.LogError("SPOT_ADD_ERROR: This error should be visible");
            logger.LogCritical("SPOT_ADD_CRITICAL: This critical message should be visible");
            
            // Test non-spot related errors (should still be visible)
            logger.LogError("Non-spot related error should be visible");
            logger.LogWarning("Non-spot related warning should be visible");
        }
        
        public static void TestSpotAdditionLogScenarios(ILogger logger)
        {
            // Simulate typical spot addition error scenarios that should be visible
            logger.LogError("SPOT_ADD_API_ERROR: Failed to connect to Supabase API");
            logger.LogError("SPOT_ADD_VALIDATION: Multiple validation errors - Name: True, Location: False, Type: True");
            logger.LogWarning("SPOT_ADD_WARNING: GPS accuracy low, using approximate position");
            logger.LogError("SPOT_ADD_CREATE_ERROR: Failed to create spot on attempt 2/3");
            logger.LogCritical("SPOT_ADD_CRITICAL: Total system failure during spot creation");
        }
    }
}