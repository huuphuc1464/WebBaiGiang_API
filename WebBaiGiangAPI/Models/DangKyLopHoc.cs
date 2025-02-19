using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace WebBaiGiangAPI.Models
{
    public class DangKyLopHoc
    {
        [Key]
        [StringLength(10)]
        [Column(TypeName = "varchar(10)")]
        public string MaDangKy { get; set; }

        [Required]
        [StringLength(10)]
        [Column(TypeName = "varchar(10)")]
        public string MaSinhVien { get; set; }

        [Required]
        [StringLength(10)]
        [Column(TypeName = "varchar(10)")]
        public string MaLop { get; set; }

        [Required]
        public DateTime NgayDangKy { get; set; }

        [Required]
        [StringLength(1)]
        public string TrangThai { get; set; }

        // Foreign keys
        [ForeignKey("MaSinhVien")]
        public NguoiDung SinhVien { get; set; }

        [ForeignKey("MaLop")]
        public Lop Lop { get; set; }
    }
}
