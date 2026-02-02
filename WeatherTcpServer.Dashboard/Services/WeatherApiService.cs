using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using WeatherTcpServer.Dashboard.Models;

namespace WeatherTcpServer.Dashboard.Services
{
    public sealed class WeatherApiService : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;
        private bool _disposed;

        public WeatherApiService(string baseUrl = "https://localhost:44363/api/")
        {
            Console.WriteLine($"[WeatherApiService] Initializing with base URL: {baseUrl}");

            // camelCase policy so the wire format matches what the API expects:
            //   {"deviceId":1,"temperature":25.3,...}
            // This is the same casing the Python script uses when it POSTs.
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(baseUrl),
                Timeout = TimeSpan.FromSeconds(10)
            };

            Console.WriteLine($"[WeatherApiService] HttpClient configured - BaseAddress: {_httpClient.BaseAddress}");
        }

        // Generic POST method for all reading types
        private async Task<bool> PostReadingAsync<T>(string endpoint, T reading)
        {
            var fullUrl = $"{_httpClient.BaseAddress}{endpoint}";
            Console.WriteLine($"[WeatherApiService] POST Request Starting");
            Console.WriteLine($"  Endpoint: {endpoint}");
            Console.WriteLine($"  Full URL: {fullUrl}");
            Console.WriteLine($"  Data Type: {typeof(T).Name}");
            Console.WriteLine($"  Payload: {JsonSerializer.Serialize(reading, _jsonOptions)}");

            try
            {
                var response = await _httpClient.PostAsJsonAsync(endpoint, reading, _jsonOptions);

                Console.WriteLine($"[WeatherApiService] Response Received");
                Console.WriteLine($"  Status Code: {(int)response.StatusCode} ({response.StatusCode})");
                Console.WriteLine($"  Success: {response.IsSuccessStatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"  Error Content: {errorContent}");
                }

                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException httpEx)
            {
                Console.WriteLine($"[WeatherApiService] HTTP Exception");
                Console.WriteLine($"  Message: {httpEx.Message}");
                Console.WriteLine($"  Status Code: {httpEx.StatusCode}");
                Console.WriteLine($"  Inner Exception: {httpEx.InnerException?.Message}");
                throw;
            }
            catch (TaskCanceledException tcEx)
            {
                Console.WriteLine($"[WeatherApiService] Timeout Exception");
                Console.WriteLine($"  Message: {tcEx.Message}");
                Console.WriteLine($"  Timeout: {_httpClient.Timeout.TotalSeconds}s");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WeatherApiService] Unexpected Exception");
                Console.WriteLine($"  Type: {ex.GetType().Name}");
                Console.WriteLine($"  Message: {ex.Message}");
                Console.WriteLine($"  Stack Trace: {ex.StackTrace}");
                throw;
            }
        }

        // Generic GET method for retrieving data
        private async Task<T?> GetAsync<T>(string endpoint)
        {
            var fullUrl = $"{_httpClient.BaseAddress}{endpoint}";
            Console.WriteLine($"[WeatherApiService] GET Request Starting");
            Console.WriteLine($"  Endpoint: {endpoint}");
            Console.WriteLine($"  Full URL: {fullUrl}");

            try
            {
                var response = await _httpClient.GetAsync(endpoint);

                Console.WriteLine($"[WeatherApiService] Response Received");
                Console.WriteLine($"  Status Code: {(int)response.StatusCode} ({response.StatusCode})");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
                    Console.WriteLine($"  Data Retrieved: Success");
                    return result;
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"  Error Content: {errorContent}");
                return default;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WeatherApiService] GET Exception: {ex.Message}");
                return default;
            }
        }

        // POST methods for readings
        public Task<bool> PostWeatherReadingAsync(WeatherReading reading)
            => PostReadingAsync("weather", reading);

        public Task<bool> PostHeatIndexReadingAsync(HeatIndexReading reading)
            => PostReadingAsync("perceivedweather", reading);

        public Task<bool> PostHydroReadingAsync(HydroReading reading)
            => PostReadingAsync("hydro", reading);

        public Task<bool> PostGasReadingAsync(GasReading reading)
            => PostReadingAsync("gas", reading);

        // GET methods for device-specific data
        public Task<List<WeatherReading>?> GetWeatherByDeviceAsync(string deviceId)
            => GetAsync<List<WeatherReading>>($"weather/devices/{deviceId}");

        public Task<WeatherReading?> GetLatestWeatherByDeviceAsync(string deviceId)
            => GetAsync<WeatherReading>($"weather/devices/{deviceId}/latest");

        public Task<List<HydroReading>?> GetHydroByDeviceAsync(string deviceId)
            => GetAsync<List<HydroReading>>($"hydro/devices/{deviceId}");

        public Task<HydroReading?> GetLatestHydroByDeviceAsync(string deviceId)
            => GetAsync<HydroReading>($"hydro/devices/{deviceId}/latest");

        public Task<List<GasReading>?> GetGasByDeviceAsync(string deviceId)
            => GetAsync<List<GasReading>>($"gas/devices/{deviceId}");

        public Task<GasReading?> GetLatestGasByDeviceAsync(string deviceId)
            => GetAsync<GasReading>($"gas/devices/{deviceId}/latest");

        public Task<List<HeatIndexReading>?> GetPerceivedWeatherByDeviceAsync(string deviceId)
            => GetAsync<List<HeatIndexReading>>($"perceivedweather/devices/{deviceId}");

        public Task<HeatIndexReading?> GetLatestPerceivedWeatherByDeviceAsync(string deviceId)
            => GetAsync<HeatIndexReading>($"perceivedweather/devices/{deviceId}/latest");

        // Device methods
        public Task<List<Device>?> GetDevicesAsync()
            => GetAsync<List<Device>>("devices");

        public Task<Device?> GetDeviceAsync(string deviceId)
            => GetAsync<Device>($"devices/{deviceId}");

        // Dispose pattern
        public void Dispose()
        {
            if (!_disposed)
            {
                Console.WriteLine("[WeatherApiService] Disposing HttpClient");
                _httpClient?.Dispose();
                _disposed = true;
            }
        }
    }
}