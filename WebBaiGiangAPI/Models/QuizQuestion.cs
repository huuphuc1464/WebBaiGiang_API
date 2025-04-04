using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WebBaiGiangAPI.Models
{
    public class QuizQuestion
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int QqId { get; set; }

        [Required]
        public int QqQuizId { get; set; }

        [Required]
        public string QqQuestion { get; set; }

        [Required]
        public string QqOption1 { get; set; }

        [Required]
        public string QqOption2 { get; set; }

        [Required]
        public string QqOption3 { get; set; }

        [Required]
        public string QqOption4 { get; set; }

        [Required]
        public string QqCorrect { get; set; }

        public string? QqDescription { get; set; }

        [JsonIgnore]
        [ForeignKey("QqQuizId")]
        public Quiz? Quiz { get; set; }
    }
}
