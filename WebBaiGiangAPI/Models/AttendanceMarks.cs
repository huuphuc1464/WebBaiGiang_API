using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WebBaiGiangAPI.Models
{
    public class AttendanceMarks
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int AttendanceMarksId { get; set; }

        [Required]
        public int StudentId { get; set; }

        [Required]
        public int ClassId { get; set; }

        [Required]
        public DateTime AttendanceDate { get; set; }

        [Required]
        [MaxLength(5)]
        public string AttendanceStatus { get; set; }

        [ForeignKey("StudentId")]
        [JsonIgnore]
        public Student? Student { get; set; }

        [ForeignKey("ClassId")]
        [JsonIgnore]
        public Class? Classes { get; set; }

    }
}
