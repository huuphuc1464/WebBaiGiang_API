using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

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

        [ForeignKey("ScStudentId")]
        public Student Student { get; set; }

        [ForeignKey("ScClassId")]
        public Class Classes { get; set; }
    }
}
