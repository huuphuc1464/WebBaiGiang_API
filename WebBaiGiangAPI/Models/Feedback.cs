using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace WebBaiGiangAPI.Models
{
    public class Feedback
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int FeedbackId { get; set; }

        [Required]
        public int FeedbackUsersId { get; set; }

        [Required]
        public int FeedbackClassId { get; set; }

        [Required]
        [MaxLength(255)]
        public string FeedbackContent { get; set; }

        [Required]
        public int FeedbackRate { get; set; }

        [Required]
        public DateTime FeedbackDate { get; set; }

        [Required]
        public int FeedbackStatus { get; set; }

        [ForeignKey("FeedbackUsersId")]
        [JsonIgnore]
        public Users? User { get; set; } 

        [ForeignKey("FeedbackClassId")]
        [JsonIgnore]
        public Class? Classes { get; set; }
    }
}
