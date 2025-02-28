using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

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
        public Semester Semester { get; set; }

        [ForeignKey("ClassSyearId")]
        public SchoolYear SchoolYear { get; set; }
        public ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
        public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
        public ICollection<TeacherClass> TeacherClasses { get; set; } = new List<TeacherClass>();
        public ICollection<Subject> Subjects { get; set; } = new List<Subject>();
        public ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
        public ICollection<AttendanceMarks> AttendanceMarks { get; set; } = new List<AttendanceMarks>();
        public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
        public ICollection<Announcement> Announcements { get; set; } = new List<Announcement>();
        public ICollection<Event> Events { get; set; } = new List<Event>();
        public ICollection<Files> Files { get; set; } = new List<Files>();
        public ICollection<StudentClass> StudentClasses { get; set; } = new List<StudentClass>();
        public ICollection<ClassCourse> ClassCourses { get; set; } = new List<ClassCourse>();

    }
}
