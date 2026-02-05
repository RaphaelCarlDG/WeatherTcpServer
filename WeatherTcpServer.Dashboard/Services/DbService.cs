using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

        #region Save Methods (using EF Core)

        public async Task<bool> SaveWeatherReadingAsync(Weather reading)
        {
            Console.WriteLine($"[DbService] Saving Weather reading - DeviceId: {reading.DeviceId}, Temp: {reading.Temperature}");

            try
            {
                await using var context = CreateContext();
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

        #endregion

        #region Device Methods

        public async Task<List<Device>> GetAllDevicesAsync()
        {
            var devices = new List<Device>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(
                "SELECT id, name, latitude, longitude, created_at FROM devices ORDER BY id",
                connection);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                devices.Add(new Device
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Latitude = reader.IsDBNull(2) ? null : reader.GetDecimal(2),
                    Longitude = reader.IsDBNull(3) ? null : reader.GetDecimal(3),
                    CreatedAt = reader.GetDateTime(4)
                });
            }

            return devices;
        }

        public async Task<Device?> GetDeviceByIdAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(
                "SELECT id, name, latitude, longitude, created_at FROM devices WHERE id = @Id",
                connection);
            command.Parameters.AddWithValue("@Id", id);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Device
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Latitude = reader.IsDBNull(2) ? null : reader.GetDecimal(2),
                    Longitude = reader.IsDBNull(3) ? null : reader.GetDecimal(3),
                    CreatedAt = reader.GetDateTime(4)
                };
            }

            return null;
        }

        #endregion

        #region Hydro Methods

        public async Task<List<Hydro>> GetHydroReadingsAsync(int deviceId, int limit = 144)
        {
            var readings = new List<Hydro>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(@"
                SELECT TOP (@Limit) id, device_id, water_level, reading_time 
                FROM hydro
                WHERE device_id = @DeviceId 
                ORDER BY reading_time DESC", connection);

            command.Parameters.AddWithValue("@DeviceId", deviceId);
            command.Parameters.AddWithValue("@Limit", limit);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                readings.Add(new Hydro
                {
                    Id = reader.GetInt32(0),
                    DeviceId = reader.GetInt32(1),
                    WaterLevel = reader.GetInt32(2),
                    ReadingTime = reader.GetDateTime(3)
                });
            }

            readings.Reverse(); // Return in chronological order
            return readings;
        }

        public async Task<Hydro?> GetLatestHydroReadingAsync(int deviceId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(@"
                SELECT TOP 1 id, device_id, water_level, reading_time 
                FROM hydro 
                WHERE device_id = @DeviceId 
                ORDER BY reading_time DESC", connection);

            command.Parameters.AddWithValue("@DeviceId", deviceId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Hydro
                {
                    Id = reader.GetInt32(0),
                    DeviceId = reader.GetInt32(1),
                    WaterLevel = reader.GetInt32(2),
                    ReadingTime = reader.GetDateTime(3)
                };
            }

            return null;
        }

        #endregion

        #region Weather Methods

        public async Task<List<Weather>> GetWeatherReadingsAsync(int deviceId, int limit = 144)
        {
            var readings = new List<Weather>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(@"
                SELECT TOP (@Limit) id, device_id, temperature, pressure, humidity, dew_point, altitude, reading_time 
                FROM weather 
                WHERE device_id = @DeviceId 
                ORDER BY reading_time DESC", connection);

            command.Parameters.AddWithValue("@DeviceId", deviceId);
            command.Parameters.AddWithValue("@Limit", limit);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                readings.Add(new Weather
                {
                    Id = reader.GetInt32(0),
                    DeviceId = reader.GetInt32(1),
                    Temperature = reader.GetDecimal(2),
                    Pressure = reader.GetDecimal(3),
                    Humidity = reader.GetDecimal(4),
                    DewPoint = reader.GetDecimal(5),
                    Altitude = reader.GetDecimal(6),
                    ReadingTime = reader.GetDateTime(7)
                });
            }

            readings.Reverse();
            return readings;
        }

        public async Task<Weather?> GetLatestWeatherReadingAsync(int deviceId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(@"
                SELECT TOP 1 id, device_id, temperature, pressure, humidity, dew_point, altitude, reading_time 
                FROM weather 
                WHERE device_id = @DeviceId 
                ORDER BY reading_time DESC", connection);

            command.Parameters.AddWithValue("@DeviceId", deviceId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Weather
                {
                    Id = reader.GetInt32(0),
                    DeviceId = reader.GetInt32(1),
                    Temperature = reader.GetDecimal(2),
                    Pressure = reader.GetDecimal(3),
                    Humidity = reader.GetDecimal(4),
                    DewPoint = reader.GetDecimal(5),
                    Altitude = reader.GetDecimal(6),
                    ReadingTime = reader.GetDateTime(7)
                };
            }

            return null;
        }

        #endregion

        #region Perceived Weather Methods

        public async Task<List<PerceivedWeather>> GetPerceivedWeatherReadingsAsync(int deviceId, int limit = 144)
        {
            var readings = new List<PerceivedWeather>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(@"
                SELECT TOP (@Limit) id, device_id, heat_index, reading_time 
                FROM perceived_weather 
                WHERE device_id = @DeviceId 
                ORDER BY reading_time DESC", connection);

            command.Parameters.AddWithValue("@DeviceId", deviceId);
            command.Parameters.AddWithValue("@Limit", limit);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                readings.Add(new PerceivedWeather
                {
                    Id = reader.GetInt32(0),
                    DeviceId = reader.GetInt32(1),
                    HeatIndex = reader.GetDecimal(2),
                    ReadingTime = reader.GetDateTime(3)
                });
            }

            readings.Reverse();
            return readings;
        }

        public async Task<PerceivedWeather?> GetLatestPerceivedWeatherReadingAsync(int deviceId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(@"
                SELECT TOP 1 id, device_id, heat_index, reading_time 
                FROM perceived_weather 
                WHERE device_id = @DeviceId 
                ORDER BY reading_time DESC", connection);

            command.Parameters.AddWithValue("@DeviceId", deviceId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new PerceivedWeather
                {
                    Id = reader.GetInt32(0),
                    DeviceId = reader.GetInt32(1),
                    HeatIndex = reader.GetDecimal(2),
                    ReadingTime = reader.GetDateTime(3)
                };
            }

            return null;
        }

        #endregion

        #region Gas Methods

        public async Task<List<Gas>> GetGasReadingsAsync(int deviceId, int limit = 144)
        {
            var readings = new List<Gas>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(@"
                SELECT TOP (@Limit) id, device_id, gas_detected, reading_time 
                FROM gas 
                WHERE device_id = @DeviceId 
                ORDER BY reading_time DESC", connection);

            command.Parameters.AddWithValue("@DeviceId", deviceId);
            command.Parameters.AddWithValue("@Limit", limit);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                readings.Add(new Gas
                {
                    Id = reader.GetInt32(0),
                    DeviceId = reader.GetInt32(1),
                    GasDetected = reader.GetBoolean(2),
                    ReadingTime = reader.GetDateTime(3)
                });
            }

            readings.Reverse();
            return readings;
        }

        public async Task<Gas?> GetLatestGasReadingAsync(int deviceId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(@"
                SELECT TOP 1 id, device_id, gas_detected, reading_time 
                FROM gas 
                WHERE device_id = @DeviceId 
                ORDER BY reading_time DESC", connection);

            command.Parameters.AddWithValue("@DeviceId", deviceId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Gas
                {
                    Id = reader.GetInt32(0),
                    DeviceId = reader.GetInt32(1),
                    GasDetected = reader.GetBoolean(2),
                    ReadingTime = reader.GetDateTime(3)
                };
            }

            return null;
        }

        #endregion

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