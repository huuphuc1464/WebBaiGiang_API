using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace WebBaiGiangAPI.Models
{
    public class Course
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
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

        [JsonIgnore]
        [ForeignKey("CourseDepartmentId")]
        public Department? Department { get; set; }
        [JsonIgnore]
        public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
        [JsonIgnore]
        public ICollection<ClassCourse> ClassCourses { get; set; } = new List<ClassCourse>();

    }
}
