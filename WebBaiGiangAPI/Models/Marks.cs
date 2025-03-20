using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace WebBaiGiangAPI.Models
{
    public class Marks
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MarksId { get; set; }

        public int? MarksExamId { get; set; }

        public int? MarksStudentId { get; set; }

        public int? MarksSubjectId { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? MarksWritten { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? MarksPractical { get; set; }

        public int? MarksSemesterId { get; set; }

        public string? MarksDescription { get; set; }

        [ForeignKey("MarksExamId")]
        public Exam Exam { get; set; }

        [ForeignKey("MarksStudentId")]
        public Student Student { get; set; }

        [ForeignKey("MarksSubjectId")]
        public Subject Subject { get; set; }

        [ForeignKey("MarksSemesterId")]
        public Semester Semester { get; set; }
    }
}
