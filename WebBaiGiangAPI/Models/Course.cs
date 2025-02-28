using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebBaiGiangAPI.Models
{
    public class Course
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CourseId { get; set; }

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

        public ICollection<Lesson> Lessons { get; set; }

        public ICollection<ClassCourse> ClassCourses { get; set; } = new List<ClassCourse>();

    }
}
