using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WebBaiGiangAPI.Models
{
    public class Semester
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SemesterId { get; set; }

        [Required]
        [MaxLength(255)]
        public string SemesterTitle { get; set; }

        public string? SemesterDescription { get; set; }

        [Required]
        public DateTime SemesterStart { get; set; }

        [Required]
        public DateTime SemesterEnd { get; set; }

        [JsonIgnore]
        public ICollection<Class> Classes { get; set; } = new List<Class>();
        [JsonIgnore]
        public ICollection<Marks> Marks { get; set; } = new List<Marks>();

    }
}
