using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WebBaiGiangAPI.Models
{
    public class Exam
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ExamId { get; set; }

        [Required]
        [MaxLength(255)]
        public string ExamTitle { get; set; }

        [Required]
        public int ExamEtypeId { get; set; }

        [Required]
        public int ExamMonth { get; set; }

        public string? ExamDescription { get; set; }

        [ForeignKey("ExamEtypeId")]
        [JsonIgnore]
        public ExamType? ExamType { get; set; } 

        [ForeignKey("ExamMonth")]
        [JsonIgnore]
        public Month? Month { get; set; }
        [JsonIgnore]
        public ICollection<Marks> Marks { get; set; } = new List<Marks>();

    }
}
