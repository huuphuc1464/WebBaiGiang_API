using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

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
        [JsonIgnore]
        public Class? Classes { get; set; }

        [ForeignKey("CourseId")]
        [JsonIgnore]
        public Course? Course { get; set; }

        [JsonIgnore]
        public ICollection<TeacherClass> TeacherClasses { get; set; } = new List<TeacherClass>();

        [JsonIgnore]
        public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();

        [JsonIgnore]
        public ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
        [JsonIgnore]
        public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
    }
}
