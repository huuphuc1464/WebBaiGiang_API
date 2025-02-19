using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace WebBaiGiangAPI.Models
{
    public class HocPhan
    {
        [Key]
        [StringLength(10)]
        [Column(TypeName = "varchar(10)")]
        public string MaHocPhan { get; set; }

        [Required]
        [StringLength(10)]
        [Column(TypeName = "varchar(10)")]
        public string MaBoMon { get; set; }

        [Required]
        [StringLength(10)]
        [Column(TypeName = "varchar(10)")]
        public string MaGiangVien { get; set; }

        [Required]
        [StringLength(10)]
        [Column(TypeName = "varchar(10)")]
        public string MaBaiGiang { get; set; }

        [Required]
        [StringLength(10)]
        [Column(TypeName = "varchar(10)")]
        public string MaBaiTap { get; set; }

        [Required]
        [StringLength(100)]
        public string TenHocPhan { get; set; }

        [Required]
        [StringLength(50)]
        public string AnhDaiDien { get; set; }

        [StringLength(100)]
        public string MoTaNgan { get; set; }

        [StringLength(255)]
        public string MoTaChiTiet { get; set; }

        [Required]
        public int DiemDanhGia { get; set; }

        [Required]
        public int SoLuongSinhVien { get; set; }

        [Required]
        public DateTime LanCapNhatCuoi { get; set; }

        [StringLength(50)]
        public string FileDeCuong { get; set; }

        [Required]
        public DateTime NgayBatDau { get; set; }

        [Required]
        [StringLength(50)]
        public string HinhThucHoc { get; set; }

        [Required]
        [StringLength(255)]
        public string NoiDung { get; set; }

        [Required]
        public int SoTiet { get; set; }

        [Required]
        public int SoTinChi { get; set; }

        [Required]
        [StringLength(10)]
        [Column(TypeName = "varchar(10)")]
        public string LoaiHocPhan { get; set; }

        [Required]
        [StringLength(1)]
        public string TrangThai { get; set; }

        // Foreign keys & Navigation properties
        [ForeignKey("MaBoMon")]
        public BoMon BoMon { get; set; }

        [ForeignKey("MaGiangVien")]
        public NguoiDung GiangVien { get; set; }

        [ForeignKey("MaBaiGiang")]
        public BaiGiang BaiGiang { get; set; }

        [ForeignKey("MaBaiTap")]
        public BaiTap BaiTap { get; set; }

        // Một học phần có thể liên quan đến nhiều lớp
        public ICollection<Lop> Lops { get; set; }
    }
}
