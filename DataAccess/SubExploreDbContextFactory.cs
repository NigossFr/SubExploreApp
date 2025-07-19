using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.Collections;
using System.Reflection;

namespace SubExplore.DataAccess
{
    /// <summary>
    /// Design-time factory for creating DbContext instances for EF Core migrations
    /// </summary>
    public class SubExploreDbContextFactory : IDesignTimeDbContextFactory<SubExploreDbContext>
    {
        public SubExploreDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<SubExploreDbContext>();
            
            // Use a simple connection string for design time
            // This should be replaced with your actual database connection details
            var connectionString = "server=localhost;port=3306;database=subexplore_dev;user=subexplore_user;password=seb09081980;CharSet=utf8mb4;";

            try
            {
                optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
            }
            catch
            {
                // Fallback to a known MySQL version if auto-detect fails
                optionsBuilder.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 21)));
            }

            return new SubExploreDbContext(optionsBuilder.Options);
        }

        private IConfiguration BuildConfiguration()
        {
            var builder = new ConfigurationBuilder();

            // Add appsettings.json from embedded resource
            var assembly = Assembly.GetExecutingAssembly();
            var appSettingsResourceName = "SubExplore.appsettings.json";
            
            using var stream = assembly.GetManifestResourceStream(appSettingsResourceName);
            if (stream != null)
            {
                builder.AddJsonStream(stream);
            }

            // Add environment variables (for production/CI scenarios)
            // Note: AddEnvironmentVariables requires Microsoft.Extensions.Configuration.EnvironmentVariables package
            // For design-time, we'll use simple environment variable reading
            try
            {
                var envVars = new Dictionary<string, string>();
                foreach (DictionaryEntry envVar in Environment.GetEnvironmentVariables())
                {
                    var key = envVar.Key?.ToString();
                    var value = envVar.Value?.ToString();
                    if (key?.StartsWith("SUBEXPLORE_") == true && !string.IsNullOrEmpty(value))
                    {
                        // Remove SUBEXPLORE_ prefix and add to configuration
                        var configKey = key.Substring("SUBEXPLORE_".Length);
                        envVars[configKey] = value;
                    }
                }
                if (envVars.Count > 0)
                {
                    builder.AddInMemoryCollection(envVars);
                }
            }
            catch
            {
                // Ignore environment variable errors at design time
            }

            // Try to add local development settings if they exist
            try
            {
                if (File.Exists("appsettings.json"))
                {
                    builder.AddJsonFile("appsettings.json", optional: true);
                }
                
                if (File.Exists("appsettings.Development.json"))
                {
                    builder.AddJsonFile("appsettings.Development.json", optional: true);
                }
            }
            catch
            {
                // Ignore file access errors at design time
            }

            return builder.Build();
        }
    }
}