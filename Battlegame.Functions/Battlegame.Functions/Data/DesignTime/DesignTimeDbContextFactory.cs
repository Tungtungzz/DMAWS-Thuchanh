// File: Data/DesignTime/DesignTimeDbContextFactory.cs
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Battlegame.Functions.Data;

namespace Battlegame.Functions.Design
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("local.settings.json", optional: true)
                .AddEnvironmentVariables();

            var config = builder.Build();

            var conn = config.GetConnectionString("BattlegameDb")
                       ?? config["ConnectionStrings:BattlegameDb"]
                       ?? "Server=localhost\\SQLEXPRESS;Database=BATTLEGAME;Trusted_Connection=True;";

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseSqlServer(conn);

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
