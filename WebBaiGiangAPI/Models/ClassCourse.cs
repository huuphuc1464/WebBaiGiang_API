using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace WebBaiGiangAPI.Models
{
    public class ClassCourse
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CcId { get; set; }

        [Required]
        public int ClassId { get; set; }

        [Required]
        public int CourseId { get; set; }

        public string? CcDescription { get; set; }

        [ForeignKey("ClassId")]
        public Class Classes { get; set; }

        [ForeignKey("CourseId")]
        public Course Course { get; set; }
    }
}
