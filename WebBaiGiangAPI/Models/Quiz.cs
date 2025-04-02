using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace WebBaiGiangAPI.Models
{
    public class Quiz
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int QuizId { get; set; }

        [Required]
        public int QuizClassId { get; set; }

        [Required]
        public int QuizTeacherId { get; set; }

        [Required]
        [MaxLength(255)]
        public string QuizTitle { get; set; }

        public string? QuizDescription { get; set; }

        [Required]
        public DateTime QuizCreateAt { get; set; }

        [Required]
        public DateTime QuizUpdateAt { get; set; }

        [Required]
        public DateTime QuizStartAt { get; set; }

        [Required]
        public DateTime QuizEndAt { get; set; }

        [ForeignKey("QuizClassId")]
        public Class Classes { get; set; } 

        [ForeignKey("QuizTeacherId")]
        public Users Teacher { get; set; }
        public ICollection<QuizQuestion> QuizQuestions { get; set; } = new List<QuizQuestion>();
        public ICollection<QuizResult> QuizResults { get; set; } = new List<QuizResult>();

    }
}
