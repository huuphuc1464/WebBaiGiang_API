using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace WebBaiGiangAPI.Models
{
    public class Quyen
    {
        [Key]
        [StringLength(10)]
        [Column(TypeName = "varchar(10)")]
        public string MaQuyen { get; set; }

        [Required]
        [StringLength(100)]
        public string TenQuyen { get; set; }

        [Required]
        [StringLength(1)]
        public string TrangThai { get; set; }

        // Navigation property
        public ICollection<NguoiDung> NguoiDungs { get; set; }
    }
}
