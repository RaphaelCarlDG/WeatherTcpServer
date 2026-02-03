using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WeatherTcpServer.Dashboard.Data;
using WeatherTcpServer.Dashboard.Models;

namespace WeatherTcpServer.Dashboard.Services
{
    public sealed class DbService : IDisposable
    {
        private readonly string _connectionString;
        private bool _disposed;

        public DbService(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            Console.WriteLine($"[DbService] Initialized with connection string");
        }

        private WeatherDbContext CreateContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<WeatherDbContext>();
            optionsBuilder.UseSqlServer(_connectionString);
            return new WeatherDbContext(optionsBuilder.Options);
        }

        public async Task<bool> SaveWeatherReadingAsync(Weather reading)
        {
            Console.WriteLine($"[DbService] Saving Weather reading - DeviceId: {reading.DeviceId}, Temp: {reading.Temperature}");

            try
            {
                await using var context = CreateContext();

                // Set the timestamp before saving
                reading.ReadingTime = DateTime.Now;

                context.Weather.Add(reading);
                var result = await context.SaveChangesAsync();

                Console.WriteLine($"[DbService] Weather saved successfully. Rows affected: {result}");
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DbService] Error saving weather: {ex.GetType().Name} - {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[DbService] Inner exception: {ex.InnerException.Message}");
                }
                throw;
            }
        }

        public async Task<bool> SaveHeatIndexReadingAsync(PerceivedWeather reading)
        {
            Console.WriteLine($"[DbService] Saving HeatIndex reading - DeviceId: {reading.DeviceId}, HeatIndex: {reading.HeatIndex}");

            try
            {
                await using var context = CreateContext();

                // Set the timestamp before saving
                reading.ReadingTime = DateTime.Now;

                context.PerceivedWeather.Add(reading);
                var result = await context.SaveChangesAsync();

                Console.WriteLine($"[DbService] HeatIndex saved successfully. Rows affected: {result}");
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DbService] Error saving heat index: {ex.GetType().Name} - {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[DbService] Inner exception: {ex.InnerException.Message}");
                }
                throw;
            }
        }

        public async Task<bool> SaveHydroReadingAsync(Hydro reading)
        {
            Console.WriteLine($"[DbService] Saving Hydro reading - DeviceId: {reading.DeviceId}, WaterLevel: {reading.WaterLevel}");

            try
            {
                await using var context = CreateContext();

                // Set the timestamp before saving
                reading.ReadingTime = DateTime.Now;

                context.Hydro.Add(reading);
                var result = await context.SaveChangesAsync();

                Console.WriteLine($"[DbService] Hydro saved successfully. Rows affected: {result}");
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DbService] Error saving hydro: {ex.GetType().Name} - {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[DbService] Inner exception: {ex.InnerException.Message}");
                }
                throw;
            }
        }

        public async Task<bool> SaveGasReadingAsync(Gas reading)
        {
            Console.WriteLine($"[DbService] Saving Gas reading - DeviceId: {reading.DeviceId}, GasDetected: {reading.GasDetected}");

            try
            {
                await using var context = CreateContext();

                // Set the timestamp before saving
                reading.ReadingTime = DateTime.Now;

                context.Gas.Add(reading);
                var result = await context.SaveChangesAsync();

                Console.WriteLine($"[DbService] Gas saved successfully. Rows affected: {result}");
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DbService] Error saving gas: {ex.GetType().Name} - {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[DbService] Inner exception: {ex.InnerException.Message}");
                }
                throw;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Console.WriteLine("[DbService] Disposed");
                _disposed = true;
            }
        }
    }
}