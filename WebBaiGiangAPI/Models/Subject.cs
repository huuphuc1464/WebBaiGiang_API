using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WebBaiGiangAPI.Models
{
    [Index(nameof(SubjectTitle), IsUnique = true)]
    public class Subject
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SubjectId { get; set; }

        [Required]
        public int SubjectClassId { get; set; }

        [Required]
        [MaxLength(255)]
        public string SubjectTitle { get; set; }

        public string? SubjectDescription { get; set; }

        [ForeignKey("SubjectClassId")]
        [JsonIgnore]
        public Class? Classes { get; set; }
        [JsonIgnore]
        public ICollection<Marks> Marks { get; set; } = new List<Marks>();

    }
}
