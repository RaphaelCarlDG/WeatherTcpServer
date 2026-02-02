using System;

namespace WeatherTcpServer.Dashboard.Models;

public sealed class GasReading : ReadingBase
{
    public bool? GasDetected { get; set; }
}
