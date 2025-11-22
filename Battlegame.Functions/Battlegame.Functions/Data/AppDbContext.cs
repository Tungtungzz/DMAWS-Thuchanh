using Microsoft.EntityFrameworkCore;
using Battlegame.Functions.Models;

namespace Battlegame.Functions.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<Player> Players => Set<Player>();
        public DbSet<Asset> Assets => Set<Asset>();
        public DbSet<PlayerAsset> PlayerAssets => Set<PlayerAsset>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // thêm index hoặc cấu hình nếu cần
            modelBuilder.Entity<Player>().HasIndex(p => p.PlayerName).IsUnique(false);
        }
    }
}
