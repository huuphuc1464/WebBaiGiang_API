using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace WebBaiGiangAPI.Models
{
    [Index(nameof(StateName), IsUnique = true)]
    public class State
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int StateId { get; set; }

        [Required]
        [MaxLength(255)]
        public string StateName { get; set; }

        public ICollection<Users> Users { get; set; } = new List<Users>();
    }
}
