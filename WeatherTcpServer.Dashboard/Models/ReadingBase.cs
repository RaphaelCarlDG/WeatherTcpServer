using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WeatherTcpServer.Dashboard.Models
{
    public abstract class ReadingBase
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("device_id")]
        public int DeviceId { get; set; }

        [ForeignKey("DeviceId")]
        public virtual Device? Device { get; set; }

        [Column("reading_time")]
        public DateTime ReadingTime { get; set; } = DateTime.Now;
    }
}