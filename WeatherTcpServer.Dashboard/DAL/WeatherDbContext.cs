using Microsoft.EntityFrameworkCore;

namespace WeatherTcpServer.Dashboard.Data
{
    public class WeatherDbContext : DbContext
    {
        public WeatherDbContext(DbContextOptions<WeatherDbContext> options)
            : base(options)
        {
        }

        public DbSet<Models.Device> Devices { get; set; }
        public DbSet<Models.Weather> Weather { get; set; }
        public DbSet<Models.PerceivedWeather> PerceivedWeather { get; set; }
        public DbSet<Models.Hydro> Hydro { get; set; }
        public DbSet<Models.Gas> Gas { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure table names (already set via [Table] attributes, but explicit here)
            modelBuilder.Entity<Models.Device>().ToTable("devices");
            modelBuilder.Entity<Models.Weather>().ToTable("weather");
            modelBuilder.Entity<Models.PerceivedWeather>().ToTable("perceived_weather");
            modelBuilder.Entity<Models.Hydro>().ToTable("hydro");
            modelBuilder.Entity<Models.Gas>().ToTable("gas");

            // Configure relationships
            modelBuilder.Entity<Models.Weather>()
                .HasOne(w => w.Device)
                .WithMany()
                .HasForeignKey(w => w.DeviceId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Models.PerceivedWeather>()
                .HasOne(p => p.Device)
                .WithMany()
                .HasForeignKey(p => p.DeviceId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Models.Hydro>()
                .HasOne(h => h.Device)
                .WithMany()
                .HasForeignKey(h => h.DeviceId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Models.Gas>()
                .HasOne(g => g.Device)
                .WithMany()
                .HasForeignKey(g => g.DeviceId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
