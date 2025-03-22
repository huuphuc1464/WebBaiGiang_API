using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WebBaiGiangAPI.Models
{
    public class StatusLearn
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SlId { get; set; }

        [Required]
        public int SlStudentId { get; set; }

        [Required]
        public int SlLessonId { get; set; }

        [Required]
        public bool SlStatus { get; set; }

        [Required]
        public DateTime SlLearnedDate { get; set; }

        [ForeignKey("SlStudentId")]
        [JsonIgnore]
        public Student? Students { get; set; }

        [ForeignKey("SlLessonId")]
        [JsonIgnore]
        public Lesson? Lesson { get; set; }
    }
}
