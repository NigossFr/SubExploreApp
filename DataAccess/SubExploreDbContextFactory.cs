using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace SubExplore.DataAccess
{
    public class SubExploreDbContextFactory : IDesignTimeDbContextFactory<SubExploreDbContext>
    {
        public SubExploreDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<SubExploreDbContext>();
            
            // Configuration par défaut pour les migrations
            var connectionString = "Host=db.iguvwnyehojvxkyqzaoi.supabase.co;Database=postgres;Username=postgres;Password=your-supabase-password;Port=5432;SSL Mode=Require;Trust Server Certificate=true;";
            
            optionsBuilder.UseNpgsql(connectionString, options =>
            {
                options.UseNetTopologySuite(); // Pour PostGIS
            });

            return new SubExploreDbContext(optionsBuilder.Options);
        }
    }
}