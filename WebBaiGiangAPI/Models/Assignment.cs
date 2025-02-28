using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace WebBaiGiangAPI.Models
{
    public class Assignment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int AssignmentId { get; set; }

        [Required]
        [MaxLength(255)]
        public string AssignmentTitle { get; set; }

        [MaxLength(255)]
        public string? AssignmentFilename { get; set; }

        public string? AssignmentDescription { get; set; }

        public int? AssignmentTeacherId { get; set; }

        [Required]
        public int AssignmentClassId { get; set; }

        public DateTime? AssignmentDeadline { get; set; }

        [Required]
        public DateTime AssignmentCreateAt { get; set; }

        [Required]
        public DateTime AssignmentStart { get; set; }

        public int? AssignmentStatus { get; set; }

        [ForeignKey("AssignmentTeacherId")]
        public TeacherClass TeacherClass { get; set; }

        [ForeignKey("AssignmentClassId")]
        public Class Classes { get; set; }
        public ICollection<Submit> Submits { get; set; } = new List<Submit>();

    }
}
