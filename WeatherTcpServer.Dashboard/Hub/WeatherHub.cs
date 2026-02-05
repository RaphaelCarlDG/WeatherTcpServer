using Microsoft.AspNetCore.SignalR;
using WeatherTcpServer.Dashboard.Models;

namespace WeatherTcpServer.Dashboard.Hubs
{
    public class WeatherHub : Hub
    {
        // Clients can call these methods if needed
        public async Task Broadcast(object data)
        {
            await Clients.All.SendAsync("receiveUpdate", data);
        }

        public async Task BroadcastBme(Weather data)
        {
            await Clients.All.SendAsync("updateBme", data);
        }

        public async Task BroadcastHydro(Hydro data)
        {
            await Clients.All.SendAsync("updateHydro", data);
        }

        public async Task BroadcastMq2(Gas data)
        {
            await Clients.All.SendAsync("updateMq2", data);
        }

        public async Task BroadcastHeatIndex(PerceivedWeather data)
        {
            await Clients.All.SendAsync("updatePerceivedWeather", data);
        }

        public async Task Test()
        {
            await Clients.All.SendAsync("receiveUpdate", new
            {
                message = "Hello from SignalR",
                time = DateTime.Now
            });
        }
    }
}