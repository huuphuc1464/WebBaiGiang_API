using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WebBaiGiangAPI.Models
{
    public class QuizResult
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int QrId { get; set; }

        [Required]
        public int QrQuizId { get; set; }

        [Required]
        public int QrStudentId { get; set; }

        [Required]
        public int QrTotalQuestion { get; set; }

        [Required]
        public int QrAnswer { get; set; }

        [Required]
        public DateTime QrDate { get; set; }

        [JsonIgnore]
        [ForeignKey("QrQuizId")]
        public Quiz Quiz { get; set; }

        [JsonIgnore]
        [ForeignKey("QrStudentId")]
        public Student Student { get; set; }

        [JsonIgnore]
        public ICollection<QuizResultDetail> QuizResultDetails { get; set; } = new List<QuizResultDetail>();
    }
}
