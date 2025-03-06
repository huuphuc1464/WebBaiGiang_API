using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace WebBaiGiangAPI.Models
{
    [Index(nameof(CountryName), IsUnique = true)]
    public class Country
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CountryId { get; set; }

        [Required]
        [MaxLength(255)]
        public string CountryName { get; set; }
        [JsonIgnore]
        public ICollection<Users> Users { get; set; } = new List<Users>();

    }
}
