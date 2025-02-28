using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace WebBaiGiangAPI.Models
{
    public class Lesson
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int LessonId { get; set; }

        [Required]
        public int LessonClassId { get; set; }

        [Required]
        public int LessonCourseId { get; set; }

        public string? LessonDescription { get; set; }

        [MaxLength(10)]
        public string? LessonChapter { get; set; }

        [MaxLength(10)]
        public string? LessonWeek { get; set; }

        [Required]
        [MaxLength(100)]
        public string LessonName { get; set; }

        [Required]
        public int LessonStatus { get; set; }

        [ForeignKey("LessonClassId")]
        public Class Classes { get; set; }

        [ForeignKey("LessonCourseId")]
        public Course Course { get; set; }
        public ICollection<LessonFile> LessonFiles { get; set; } = new List<LessonFile>();

    }
}
