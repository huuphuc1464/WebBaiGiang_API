using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace WebBaiGiangAPI.Models
{
    public class BoMon
    {
        [Key]
        [StringLength(10)]
        [Column(TypeName = "varchar(10)")]
        public string MaBoMon { get; set; }

        [Required]
        [StringLength(10)]
        [Column(TypeName = "varchar(10)")]
        public string MaKhoa { get; set; }

        [Required]
        [StringLength(100)]
        public string TenBoMon { get; set; }

        [Required]
        [StringLength(1)]
        public string TrangThai { get; set; }

        // Foreign key
        [ForeignKey("MaKhoa")]
        public Khoa Khoa { get; set; }

        // Navigation properties
        public ICollection<HocPhan> HocPhans { get; set; }
        public ICollection<NguoiDung> NguoiDungs { get; set; }
    }
}
