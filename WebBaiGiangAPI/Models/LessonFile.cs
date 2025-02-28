using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace WebBaiGiangAPI.Models
{
    public class LessonFile
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int LfId { get; set; }

        [Required]
        public int LfLessonId { get; set; }

        [Required]
        [MaxLength(255)]
        public string LfFilename { get; set; }

        [Required]
        [MaxLength(100)]
        public string LfType { get; set; }

        [ForeignKey("LfLessonId")]
        public Lesson Lesson { get; set; }
    }
}
