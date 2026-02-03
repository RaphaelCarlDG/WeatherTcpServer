using System.ComponentModel.DataAnnotations.Schema;

namespace WeatherTcpServer.Dashboard.Models
{
    [Table("perceived_weather")]
    public sealed class PerceivedWeather : ReadingBase
    {
        [Column("heat_index")]
        public double? HeatIndex { get; set; }
    }
}