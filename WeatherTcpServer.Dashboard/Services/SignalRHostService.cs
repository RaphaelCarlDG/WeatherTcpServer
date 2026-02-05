using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WeatherTcpServer.Dashboard.Hubs;
using WeatherTcpServer.Dashboard.Models;

namespace WeatherTcpServer.Dashboard.Services
{
    public sealed class SignalRHostService : IDisposable
    {
        private IHost? _host;
        private IHubContext<WeatherHub>? _hubContext;
        private readonly DbService _dbService;
        private readonly string _url;
        private bool _disposed;

        public event Action<string>? Log;

        public SignalRHostService(string url, DbService dbService)
        {
            _url = url;
            _dbService = dbService;
            Console.WriteLine($"[SignalRHost] Initialized with URL: {url}");
        }

        public async Task StartAsync()
        {
            try
            {
                var builder = WebApplication.CreateBuilder();

                // Register DbService for dependency injection
                builder.Services.AddSingleton(_dbService);

                // Add SignalR services with PascalCase JSON
                builder.Services.AddSignalR()
                    .AddJsonProtocol(options =>
                    {
                        options.PayloadSerializerOptions.PropertyNamingPolicy = null;
                    });

                // Add CORS for web clients
                builder.Services.AddCors(options =>
                {
                    options.AddDefaultPolicy(policy =>
                    {
                        policy.WithOrigins(
                                "https://localhost:44361",
                                "http://localhost:44361"
                              )
                              .AllowAnyHeader()
                              .AllowAnyMethod()
                              .AllowCredentials();
                    });
                });

                builder.WebHost.UseUrls(_url);

                var app = builder.Build();

                app.UseCors();

                // Get all devices
                app.MapGet("/api/devices", async (DbService db) =>
                {
                    var devices = await db.GetAllDevicesAsync();
                    return Results.Json(devices);
                });

                // Get single device
                app.MapGet("/api/devices/{id:int}", async (int id, DbService db) =>
                {
                    var device = await db.GetDeviceByIdAsync(id);
                    return device != null ? Results.Json(device) : Results.NotFound();
                });

                // Create new device
                app.MapPost("/api/devices", async (Device device, DbService db) =>
                {
                    try
                    {
                        // Validate required fields
                        if (string.IsNullOrWhiteSpace(device.Name))
                        {
                            return Results.BadRequest(new { message = "Device name is required" });
                        }

                        if (device.Name.Length > 100)
                        {
                            return Results.BadRequest(new { message = "Device name cannot exceed 100 characters" });
                        }

                        // Validate latitude
                        if (device.Latitude.HasValue && (device.Latitude.Value < -90 || device.Latitude.Value > 90))
                        {
                            return Results.BadRequest(new { message = "Latitude must be between -90 and 90" });
                        }

                        // Validate longitude
                        if (device.Longitude.HasValue && (device.Longitude.Value < -180 || device.Longitude.Value > 180))
                        {
                            return Results.BadRequest(new { message = "Longitude must be between -180 and 180" });
                        }

                        var createdDevice = await db.CreateDeviceAsync(device);

                        if (createdDevice != null)
                        {
                            return Results.Created($"/api/devices/{createdDevice.Id}", createdDevice);
                        }

                        return Results.Problem("Failed to create device");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[API] Error creating device: {ex.Message}");
                        return Results.Problem($"Error creating device: {ex.Message}");
                    }
                });

                // Update device
                app.MapPut("/api/devices/{id:int}", async (int id, Device device, DbService db) =>
                {
                    if (id != device.Id)
                        return Results.BadRequest("Device ID mismatch");

                    var success = await db.UpdateDeviceAsync(device);
                    return success ? Results.Ok(device) : Results.NotFound();
                });

                // Get hydro readings for device
                app.MapGet("/api/hydro/device/{deviceId:int}", async (int deviceId, DbService db) =>
                {
                    var readings = await db.GetHydroReadingsAsync(deviceId, limit: 144);
                    return Results.Json(readings);
                });

                // Get weather readings for device
                app.MapGet("/api/weather/device/{deviceId:int}", async (int deviceId, DbService db) =>
                {
                    var readings = await db.GetWeatherReadingsAsync(deviceId, limit: 144);
                    return Results.Json(readings);
                });

                // Get perceived weather readings for device
                app.MapGet("/api/perceivedweather/device/{deviceId:int}", async (int deviceId, DbService db) =>
                {
                    var readings = await db.GetPerceivedWeatherReadingsAsync(deviceId, limit: 144);
                    return Results.Json(readings);
                });

                // Get gas readings for device
                app.MapGet("/api/gas/device/{deviceId:int}", async (int deviceId, DbService db) =>
                {
                    var readings = await db.GetGasReadingsAsync(deviceId, limit: 144);
                    return Results.Json(readings);
                });

                // Get latest hydro reading
                app.MapGet("/api/hydro/device/{deviceId:int}/latest", async (int deviceId, DbService db) =>
                {
                    var reading = await db.GetLatestHydroReadingAsync(deviceId);
                    return reading != null ? Results.Json(reading) : Results.NotFound();
                });

                // Get latest weather reading
                app.MapGet("/api/weather/device/{deviceId:int}/latest", async (int deviceId, DbService db) =>
                {
                    var reading = await db.GetLatestWeatherReadingAsync(deviceId);
                    return reading != null ? Results.Json(reading) : Results.NotFound();
                });

                // Get latest perceived weather reading
                app.MapGet("/api/perceivedweather/device/{deviceId:int}/latest", async (int deviceId, DbService db) =>
                {
                    var reading = await db.GetLatestPerceivedWeatherReadingAsync(deviceId);
                    return reading != null ? Results.Json(reading) : Results.NotFound();
                });

                // Get latest gas reading
                app.MapGet("/api/gas/device/{deviceId:int}/latest", async (int deviceId, DbService db) =>
                {
                    var reading = await db.GetLatestGasReadingAsync(deviceId);
                    return reading != null ? Results.Json(reading) : Results.NotFound();
                });

                // Get all readings for a device in one call
                app.MapGet("/api/devices/{deviceId:int}/all-readings", async (int deviceId, DbService db) =>
                {
                    try
                    {
                        // Run all queries in parallel
                        var deviceTask = db.GetDeviceByIdAsync(deviceId);
                        var hydroTask = db.GetHydroReadingsAsync(deviceId, limit: 144);
                        var weatherTask = db.GetWeatherReadingsAsync(deviceId, limit: 144);
                        var perceivedWeatherTask = db.GetPerceivedWeatherReadingsAsync(deviceId, limit: 144);
                        var gasTask = db.GetGasReadingsAsync(deviceId, limit: 144);
                        var latestHydroTask = db.GetLatestHydroReadingAsync(deviceId);
                        var latestWeatherTask = db.GetLatestWeatherReadingAsync(deviceId);
                        var latestPerceivedTask = db.GetLatestPerceivedWeatherReadingAsync(deviceId);
                        var latestGasTask = db.GetLatestGasReadingAsync(deviceId);

                        await Task.WhenAll(
                            deviceTask, hydroTask, weatherTask, perceivedWeatherTask, gasTask,
                            latestHydroTask, latestWeatherTask, latestPerceivedTask, latestGasTask
                        );

                        var device = await deviceTask;
                        if (device == null)
                            return Results.NotFound();

                        var result = new
                        {
                            Device = device,
                            HydroReadings = await hydroTask,
                            WeatherReadings = await weatherTask,
                            PerceivedWeatherReadings = await perceivedWeatherTask,
                            GasReadings = await gasTask,
                            LatestHydro = await latestHydroTask,
                            LatestWeather = await latestWeatherTask,
                            LatestPerceivedWeather = await latestPerceivedTask,
                            LatestGas = await latestGasTask
                        };

                        return Results.Json(result);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[API] Error fetching all readings: {ex.Message}");
                        return Results.Problem($"Error fetching device readings: {ex.Message}");
                    }
                });

                // Map the SignalR hub
                app.MapHub<WeatherHub>("/weatherhub");

                _hubContext = app.Services.GetRequiredService<IHubContext<WeatherHub>>();

                _host = app;
                await _host.StartAsync();

                Log?.Invoke($"[SignalR] Hub started at {_url}/weatherhub");
                Log?.Invoke($"[HTTP] API endpoints available at {_url}/api/");
            }
            catch (Exception ex)
            {
                Log?.Invoke($"[SignalR] Error starting hub: {ex.Message}");
                throw;
            }
        }

        public async Task StopAsync()
        {
            if (_host != null)
            {
                try
                {
                    await _host.StopAsync();
                    Log?.Invoke("[SignalR] Hub stopped");
                }
                catch (Exception ex)
                {
                    Log?.Invoke($"[SignalR] Error stopping hub: {ex.Message}");
                }
            }
        }

        public async Task BroadcastWeatherAsync(Weather data)
        {
            if (_hubContext != null)
            {
                try
                {
                    await _hubContext.Clients.All.SendAsync("updateBme", data);
                    Console.WriteLine($"[SignalR] Weather data broadcasted to all clients");
                }
                catch (Exception ex)
                {
                    Log?.Invoke($"[SignalR] Error broadcasting weather: {ex.Message}");
                }
            }
        }

        public async Task BroadcastHeatIndexAsync(PerceivedWeather data)
        {
            if (_hubContext != null)
            {
                try
                {
                    await _hubContext.Clients.All.SendAsync("updatePerceivedWeather", data);
                    Console.WriteLine($"[SignalR] Heat index data broadcasted to all clients");
                }
                catch (Exception ex)
                {
                    Log?.Invoke($"[SignalR] Error broadcasting heat index: {ex.Message}");
                }
            }
        }

        public async Task BroadcastHydroAsync(Hydro data)
        {
            if (_hubContext != null)
            {
                try
                {
                    await _hubContext.Clients.All.SendAsync("updateHydro", data);
                    Console.WriteLine($"[SignalR] Hydro data broadcasted to all clients");
                }
                catch (Exception ex)
                {
                    Log?.Invoke($"[SignalR] Error broadcasting hydro: {ex.Message}");
                }
            }
        }

        public async Task BroadcastGasAsync(Gas data)
        {
            if (_hubContext != null)
            {
                try
                {
                    await _hubContext.Clients.All.SendAsync("updateMq2", data);
                    Console.WriteLine($"[SignalR] Gas data broadcasted to all clients");
                }
                catch (Exception ex)
                {
                    Log?.Invoke($"[SignalR] Error broadcasting gas: {ex.Message}");
                }
            }
        }

        public bool IsRunning => _host != null;

        public void Dispose()
        {
            if (!_disposed)
            {
                _host?.Dispose();
                Console.WriteLine("[SignalRHost] Disposed");
                _disposed = true;
            }
        }
    }
}