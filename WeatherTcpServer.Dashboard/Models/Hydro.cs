using System.ComponentModel.DataAnnotations.Schema;

namespace WeatherTcpServer.Dashboard.Models
{
    [Table("hydro")]
    public sealed class Hydro : ReadingBase
    {
        [Column("water_level")]
        public int WaterLevel { get; set; }
    }
}