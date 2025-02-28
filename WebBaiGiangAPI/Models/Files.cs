using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace WebBaiGiangAPI.Models
{
    public class Files
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int FilesId { get; set; }

        [Required]
        [MaxLength(255)]
        public string FilesTitle { get; set; }

        [Required]
        public int FilesClassId { get; set; }

        [Required]
        public int FilesTeacherId { get; set; }

        [Required]
        [MaxLength(255)]
        public string FilesFilename { get; set; }

        public string? FilesDescription { get; set; }

        [ForeignKey("FilesClassId")]
        public Class Classes { get; set; }

        [ForeignKey("FilesTeacherId")]
        public Users Teacher { get; set; }
    }
}
