using System;
namespace WeatherTcpServer.Dashboard.Models;

public sealed class HeatIndexReading : ReadingBase
{
    public double HeatIndex { get; set; }
}
