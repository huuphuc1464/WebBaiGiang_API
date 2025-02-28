using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WebBaiGiangAPI.Models
{
    [Index(nameof(StudentCode), IsUnique = true)]
    public class Student
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int StudentId { get; set; }

        [Required]
        [MaxLength(10)]
        public string StudentCode { get; set; }

        [MaxLength(50)]
        public string? StudentRollno { get; set; }

        [MaxLength(255)]
        public string? StudentFatherName { get; set; }

        public string? StudentDetails { get; set; }

        [ForeignKey("StudentId")]
        public Users Users { get; set; }

        public ICollection<QuizResult> QuizResults { get; set; } = new List<QuizResult>();
        
        public ICollection<Marks> Marks { get; set; } = new List<Marks>();

        public ICollection<AttendanceMarks> AttendanceMarks { get; set; } = new List<AttendanceMarks>();
       
        public ICollection<Submit> Submits { get; set; } = new List<Submit>();

        public ICollection<StudentClass> StudentClasses { get; set; } = new List<StudentClass>();

    }
}
