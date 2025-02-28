using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebBaiGiangAPI.Models
{
    public class TeacherClass
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int TcId { get; set; }

        [Required]
        public int TcUsersId { get; set; }

        [Required]
        public int TcClassId { get; set; }

        public string? TcDescription { get; set; }

        [ForeignKey("TcUsersId")]
        public Users User { get; set; } 

        [ForeignKey("TcClassId")]
        public Class Classes { get; set; }
        public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();

    }
}
