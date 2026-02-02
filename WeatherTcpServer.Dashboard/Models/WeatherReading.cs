using System;

namespace WeatherTcpServer.Dashboard.Models;

public sealed class WeatherReading : ReadingBase
{
    public double Temperature { get; set; }
    public double Pressure { get; set; }
    public double Humidity { get; set; }
    public double Altitude { get; set; }
    public double DewPoint { get; set; }
}
