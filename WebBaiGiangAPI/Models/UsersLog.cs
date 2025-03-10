using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WebBaiGiangAPI.Models
{
    public class UsersLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UlogId { get; set; }

        [Required]
        public int UlogUsersId { get; set; }

        [Required]
        [MaxLength(255)]
        public string UlogUsername { get; set; }

        [Required]
        public DateTime UlogLoginDate { get; set; }

        public DateTime? UlogLogoutDate { get; set; }

        [ForeignKey("UlogUsersId")]
        [JsonIgnore]
        public Users? Users { get; set; }
    }
}
