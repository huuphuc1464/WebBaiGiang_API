using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Collections;

namespace WebBaiGiangAPI.Models
{
    [Index(nameof(DepartmentCode), IsUnique = true)]
    public class Department
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int DepartmentId { get; set; }

        [Required]
        [MaxLength(255)]
        public string DepartmentTitle { get; set; }

        [Required]
        public string DepartmentDescription { get; set; }

        [Required]
        [MaxLength(50)]
        public string DepartmentCode { get; set; }
        [JsonIgnore]
        public ICollection<Users> Users { get; set; } = new List<Users>();
        [JsonIgnore]
        public ICollection<Course> Courses { get; set; } = new List<Course>();
    }
}
