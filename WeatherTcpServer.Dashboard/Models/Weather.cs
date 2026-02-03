using System.ComponentModel.DataAnnotations.Schema;

namespace WeatherTcpServer.Dashboard.Models
{
    [Table("weather")]
    public sealed class Weather : ReadingBase
    {
        [Column("temperature")]
        public decimal? Temperature { get; set; }

        [Column("pressure")]
        public decimal? Pressure { get; set; }

        [Column("humidity")]
        public decimal? Humidity { get; set; }

        [Column("altitude")]
        public decimal? Altitude { get; set; }

        [Column("dew_point")]
        public decimal? DewPoint { get; set; }
    }
}