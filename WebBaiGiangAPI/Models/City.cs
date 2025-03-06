using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace WebBaiGiangAPI.Models
{

    [Index(nameof(CityName), IsUnique = true)]
    public class City
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CityId { get; set; }

        [Required]
        [MaxLength(255)]
        public string CityName { get; set; }
        [JsonIgnore]
        public ICollection<Users> Users { get; set; } = new List<Users>();
    }
}
