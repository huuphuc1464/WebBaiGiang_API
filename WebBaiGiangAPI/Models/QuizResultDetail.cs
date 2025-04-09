using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WebBaiGiangAPI.Models
{
    public class QuizResultDetail
    {
        [Key]
        public int QrdId { get; set; }

        [Required]
        public int QrdResultId { get; set; }

        [Required]
        public int QrdQuestionId { get; set; }

        public string QrdStudentAnswer { get; set; }

        public bool QrdIsCorrect { get; set; }

        [JsonIgnore]    
        [ForeignKey("QrdResultId")]
        public QuizResult? QuizResult { get; set; }

        [JsonIgnore]
        [ForeignKey("QrdQuestionId")]
        public QuizQuestion? QuizQuestion { get; set; }
    }
}
