using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WebBaiGiangAPI.Models
{
    [Index(nameof(UsersUsername), IsUnique = true)]
    [Index(nameof(UsersEmail), IsUnique = true)]
    [Index(nameof(UsersMobile), IsUnique = true)]
    public class UsersDTO
    {
        public int UsersId { get; set; }

        [Required]
        public int UsersRoleId { get; set; }

        //[Required]
        [MaxLength(100)]
        public string UsersName { get; set; }

        //[Required]
        [MaxLength(255)]
        public string UsersUsername { get; set; }

        [MaxLength(255)]
        public string? UsersPassword { get; set; }

        //[Required]
        [MaxLength(255)]
        public string UsersEmail { get; set; }

        [MaxLength(10)]
        public string? UsersMobile { get; set; }

        public DateOnly? UsersDob { get; set; }

        [MaxLength(255)]
        public string? UsersImage { get; set; }

        [MaxLength(255)]
        public string? UsersAdd { get; set; }

        public int? UsersCity { get; set; }
        public int? UsersState { get; set; }
        public int? UsersCountry { get; set; }

        public int UsersDepartmentId { get; set; }

        [Required]
        public int UserLevelId { get; set; }

        [MaxLength(3)]
        public string? UserGender { get; set; }
    }
}
