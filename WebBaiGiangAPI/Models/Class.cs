using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WebBaiGiangAPI.Models
{
    public class Class
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ClassId { get; set; }
        
        [Required]
        [MaxLength(255)]
        public string ClassTitle { get; set; }

        public string? ClassDescription { get; set; }

        [Required]
        public int ClassSemesterId { get; set; }

        [Required]
        public int ClassSyearId { get; set; }

        [Required]
        public DateTime ClassUpdateAt { get; set; }

        [ForeignKey("ClassSemesterId")]
        [JsonIgnore]
        public Semester? Semester { get; set; }

        [ForeignKey("ClassSyearId")]
        [JsonIgnore]
        public SchoolYear? SchoolYear { get; set; }
        [JsonIgnore]
        public ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
        //[JsonIgnore]
        //public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
        [JsonIgnore]
        public ICollection<Subject> Subjects { get; set; } = new List<Subject>();
        [JsonIgnore]
        public ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
        [JsonIgnore]
        public ICollection<AttendanceMarks> AttendanceMarks { get; set; } = new List<AttendanceMarks>();
        [JsonIgnore]
        public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
        [JsonIgnore]
        public ICollection<Announcement> Announcements { get; set; } = new List<Announcement>();
        [JsonIgnore]
        public ICollection<Event> Events { get; set; } = new List<Event>();
        [JsonIgnore]
        public ICollection<Files> Files { get; set; } = new List<Files>();
        [JsonIgnore]
        public ICollection<StudentClass> StudentClasses { get; set; } = new List<StudentClass>();
        [JsonIgnore]
        public ICollection<ClassCourse> ClassCourses { get; set; } = new List<ClassCourse>();

    }
}
