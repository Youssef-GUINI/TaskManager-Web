using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace TaskManager.data
{
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            // Configuration pour lire appsettings.json
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

            // Récupérer la chaîne de connexion depuis appsettings.json
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            // Configuration MySQL avec les bonnes options
            optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), mySqlOptions =>
            {
                // Options MySQL pour améliorer les performances
                mySqlOptions.EnableRetryOnFailure(3);
            });

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}