using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace WebBaiGiangAPI.Models
{
    [Index(nameof(SyearTitle), IsUnique = true)]
    public class SchoolYear
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SyearId { get; set; }

        [Required]
        [MaxLength(255)]
        public string SyearTitle { get; set; }

        public string SyearDescription { get; set; }
        public ICollection<Class> Classes { get; set; } = new List<Class>();
    }
}
