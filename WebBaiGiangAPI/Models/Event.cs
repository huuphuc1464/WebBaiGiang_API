using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

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

        [ForeignKey("EventClassId")]
        public Class Classes { get; set; }

        [ForeignKey("EventTeacherId")]
        public Users Teacher { get; set; }
    }
}
