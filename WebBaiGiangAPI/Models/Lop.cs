using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace WebBaiGiangAPI.Models
{
    public class Lop
    {
        [Key]
        [StringLength(10)]
        [Column(TypeName = "varchar(10)")]
        public string MaLop { get; set; }

        [Required]
        [StringLength(10)]
        [Column(TypeName = "varchar(10)")]
        public string MaHocPhan { get; set; }

        [Required]
        public int HocKy { get; set; }

        [Required]
        [StringLength(100)]
        public string TenLop { get; set; }

        [Required]
        [StringLength(30)]
        public string PhongHoc { get; set; }

        [Required]
        public int NamHoc { get; set; }

        [Required]
        [StringLength(1)]
        public string TrangThaiLop { get; set; }

        [Required]
        [StringLength(1)]
        public string TrangThaiHoc { get; set; }

        // Foreign key
        [ForeignKey("MaHocPhan")]
        public HocPhan HocPhan { get; set; }

        // Navigation properties
        public ICollection<BaiGiang> BaiGiangs { get; set; }
        public ICollection<DangKyLopHoc> DangKyLopHocs { get; set; }
        public ICollection<DiemDanh> DiemDanhs { get; set; }
        public ICollection<BangDiem> BangDiems { get; set; }
        public ICollection<DanhGia> DanhGias { get; set; }
        public ICollection<BaiTap> BaiTaps { get; set; }
    }
}
