using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WeatherTcpServer.Dashboard.Models
{
    [Table("devices")]
    public class Device
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("latitude")]
        public decimal? Latitude { get; set; }

        [Column("longitude")]
        public decimal? Longitude { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}