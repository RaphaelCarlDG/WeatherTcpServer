using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WeatherTcpServer.Dashboard.Models
{
    public abstract class ReadingBase
    {
        [JsonIgnore]  // Don't serialize to JSON when POSTing
        public int Id { get; set; }

        [Required]
        public int DeviceId { get; set; }

        [JsonIgnore]  // Don't serialize to JSON when POSTing
        public virtual Device? Device { get; set; }

        [JsonIgnore]  // Don't serialize to JSON when POSTing
        public DateTime ReadingTime { get; set; }
    }
}