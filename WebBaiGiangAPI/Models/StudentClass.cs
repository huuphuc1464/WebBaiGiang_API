using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WebBaiGiangAPI.Models
{
    public class StudentClass
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ScId { get; set; }

        [Required]
        public int ScStudentId { get; set; }

        [Required]
        public int ScClassId { get; set; }

        public string? ScDescription { get; set; }

        public string? ScToken { get; set; }

        public DateTime ScCreateAt { get; set; }

        [Required]
        public int ScStatus { get; set; }

        [ForeignKey("ScStudentId")]
        [JsonIgnore]
        public Student? Student { get; set; }

        [ForeignKey("ScClassId")]
        [JsonIgnore]
        public Class? Classes { get; set; }
    }
}
