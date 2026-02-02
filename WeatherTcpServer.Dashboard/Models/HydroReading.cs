using System;

namespace WeatherTcpServer.Dashboard.Models;

public sealed class HydroReading : ReadingBase
{
    public int WaterLevel { get; set; } 
}
