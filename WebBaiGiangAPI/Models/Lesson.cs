using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WebBaiGiangAPI.Models
{
    public class Lesson
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int LessonId { get; set; }

        //[Required]
        //public int LessonClassId { get; set; }

        //[Required]
        //public int LessonCourseId { get; set; }

        [Required]
        public int LessonTeacherId { get; set; }

        [Required]
        public int LessonClassCourseId { get; set; }

        public string? LessonDescription { get; set; }

        [MaxLength(10)]
        public string? LessonChapter { get; set; }

        public int? LessonWeek { get; set; }

        [Required]
        [MaxLength(100)]
        public string LessonName { get; set; }

        [Required]
        public bool LessonStatus { get; set; }

        //public bool LessonCourseStatus { get; set; }

        [Required]
        public DateTime LessonCreateAt { get; set; }

        [Required]
        public DateTime LessonUpdateAt { get; set; }

        //[ForeignKey("LessonClassId")]
        //[JsonIgnore]
        //public Class? Classes { get; set; }

        //[ForeignKey("LessonCourseId")]
        //[JsonIgnore]
        //public Course? Course { get; set; }

        [ForeignKey("LessonClassCourseId")]
        [JsonIgnore]
        public ClassCourse? ClassCourse { get; set; }

        [ForeignKey("LessonTeacherId")]
        [JsonIgnore]
        public Users? Teachers { get; set; }

        [JsonIgnore]
        public ICollection<LessonFile> LessonFiles { get; set; } = new List<LessonFile>();

        [JsonIgnore]
        public ICollection<StatusLearn> StatusLearns { get; set; } = new List<StatusLearn>();
    }
}
