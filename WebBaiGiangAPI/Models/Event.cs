using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WebBaiGiangAPI.Models
{
    public class Event
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int EventId { get; set; }

        [Required]
        public int EventClassId { get; set; }

        [Required]
        public int EventTeacherId { get; set; }

        [Required]
        [MaxLength(255)]
        public string EventTitle { get; set; }

        public string? EventDescription { get; set; }

        [Required]
        public DateTime EventDateStart { get; set; }

        [Required]
        public DateTime EventDateEnd { get; set; }
        public string? EventZoomLink { get; set; }
        public string? EventPassword { get; set; }

        [ForeignKey("EventClassId")]
        [JsonIgnore]
        public Class? Classes { get; set; }

        [ForeignKey("EventTeacherId")]
        [JsonIgnore]
        public Users? Teacher { get; set; }
    }
}
