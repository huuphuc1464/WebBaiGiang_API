using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace WebBaiGiangAPI.Models
{
    public class Submit
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SubmitId { get; set; }

        [Required]
        public int SubmitAssignmentId { get; set; }

        [Required]
        public int SubmitStudentId { get; set; }

        [Required]
        [MaxLength(100)]
        public string SubmitFile { get; set; }

        [Required]
        public DateTime SubmitDate { get; set; }

        public int SubmitStatus { get; set; }

        [ForeignKey("SubmitAssignmentId")]
        public Assignment Assignment { get; set; }

        [ForeignKey("SubmitStudentId")]
        public Student Student { get; set; }
    }
}
