using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace WebBaiGiangAPI.Models
{
    public class Khoa
    {
        [Key]
        [StringLength(10)]
        [Column(TypeName = "varchar(10)")]
        public string MaKhoa { get; set; }

        [Required]
        [StringLength(100)]
        [Column(TypeName = "nvarchar(100)")]
        public string TenKhoa { get; set; }

        [Required]
        [StringLength(1)]
        public string TrangThai { get; set; }

        // Navigation properties
        public ICollection<BoMon> BoMons { get; set; }
        public ICollection<NguoiDung> NguoiDungs { get; set; }
    }
}
