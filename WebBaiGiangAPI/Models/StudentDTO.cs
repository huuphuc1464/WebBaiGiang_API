using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebBaiGiangAPI.Models
{
    [Index(nameof(StudentCode), IsUnique = true)]
    [Index(nameof(UsersUsername), IsUnique = true)]
    [Index(nameof(UsersEmail), IsUnique = true)]
    [Index(nameof(UsersMobile), IsUnique = true)]

    public class StudentDTO
    {
        //[Key]
        //[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UsersId { get; set; }

        [Required]
        [MaxLength(100)]
        public string UsersName { get; set; }

        [Required]
        [MaxLength(255)]
        public string UsersUsername { get; set; }

        [MaxLength(255)]
        public string? UsersPassword { get; set; }

        [Required]
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

        [MaxLength(3)]
        public string? UserGender { get; set; }

        [Required]
        [MaxLength(10)]
        public string StudentCode { get; set; }

        [MaxLength(50)]
        public string? StudentRollno { get; set; }

        [MaxLength(255)]
        public string? StudentFatherName { get; set; }

        public string? StudentDetails { get; set; }
    }
}
