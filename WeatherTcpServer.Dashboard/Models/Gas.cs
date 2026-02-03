using System.ComponentModel.DataAnnotations.Schema;

namespace WeatherTcpServer.Dashboard.Models
{
    [Table("gas")]
    public sealed class Gas : ReadingBase
    {
        [Column("gas_detected")]
        public bool? GasDetected { get; set; }
    }
}