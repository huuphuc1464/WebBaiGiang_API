using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace WebBaiGiangAPI.Models
{
    public class CourseDTO
    {
        public int CourseId { get; set; }

        [Required]
        public int CourseDepartmentId { get; set; }

        [Required]
        [MaxLength(255)]
        public string CourseTitle { get; set; }

        [Required]
        public int CourseTotalSemester { get; set; }

        [MaxLength(100)]
        public string? CourseImage { get; set; }

        [MaxLength(100)]
        public string? CourseShortdescription { get; set; }

        public string? CourseDescription { get; set; }

        public DateTime? CourseUpdateAt { get; set; }
    }
}
