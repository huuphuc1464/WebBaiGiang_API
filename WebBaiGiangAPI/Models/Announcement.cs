using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WebBaiGiangAPI.Models
{
    public class Announcement
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int AnnouncementId { get; set; }

        [Required]
        public int AnnouncementClassId { get; set; }

        [Required]
        public int AnnouncementTeacherId { get; set; }

        [Required]
        [MaxLength(255)]
        public string AnnouncementTitle { get; set; }

        public string? AnnouncementDescription { get; set; }

        [Required]
        public DateTime AnnouncementDate { get; set; }

        [ForeignKey("AnnouncementClassId")]
        [JsonIgnore]
        public Class? Classes { get; set; }

        [ForeignKey("AnnouncementTeacherId")]
        [JsonIgnore]
        public Users? Teacher { get; set; }
    }
}
