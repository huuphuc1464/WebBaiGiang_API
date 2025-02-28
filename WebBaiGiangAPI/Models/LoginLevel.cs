using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace WebBaiGiangAPI.Models
{
    public class LoginLevel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int LevelId { get; set; }

        [Required]
        [MaxLength(255)]
        public string LevelTitle { get; set; }

        [MaxLength(255)]
        public string LevelDescription { get; set; }
        public ICollection<Users> Users { get; set; } = new List<Users>();

    }
}
