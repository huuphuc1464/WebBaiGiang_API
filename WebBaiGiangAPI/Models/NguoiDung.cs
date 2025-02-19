using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace WebBaiGiangAPI.Models
{
    public class NguoiDung
    {
        [Key]
        [StringLength(10)]
        [Column(TypeName = "varchar(10)")]
        public string MaNguoiDung { get; set; }

        [Required]
        [StringLength(10)]
        [Column(TypeName = "varchar(10)")]
        public string MaQuyen { get; set; }

        [Required]
        [StringLength(10)]
        [Column(TypeName = "varchar(10)")]
        public string MaKhoa { get; set; }

        [Required]
        [StringLength(10)]
        [Column(TypeName = "varchar(10)")]
        public string MaBoMon { get; set; }

        [Required]
        [StringLength(50)]
        public string Email { get; set; }

        [Required]
        [StringLength(255)]
        public string Password { get; set; }

        [Required]
        [StringLength(50)]
        public string HoTen { get; set; }

        [Required]
        [StringLength(30)]
        public string Lop { get; set; }

        [Required]
        [StringLength(150)]
        public string DiaChi { get; set; }

        [Required]
        [StringLength(50)]
        public string AnhDaiDien { get; set; }

        [StringLength(10)]
        public string MSSV { get; set; }

        [Required]
        [StringLength(10)]
        public string SDT { get; set; }

        [Required]
        [StringLength(3)]
        public string GioiTinh { get; set; }

        [Required]
        public DateOnly NgaySinh { get; set; }

        [StringLength(1)]
        public string TrangThai { get; set; }

        // Foreign keys & Navigation properties
        [ForeignKey("MaKhoa")]
        public Khoa Khoa { get; set; }

        [ForeignKey("MaBoMon")]
        public BoMon BoMon { get; set; }

        [ForeignKey("MaQuyen")]
        public Quyen Quyen { get; set; }

        public ICollection<DanhGia> DanhGias { get; set; }
        public ICollection<DangKyLopHoc> DangKyLopHocs { get; set; }
        public ICollection<NopBaiTap> NopBaiTaps { get; set; }
        public ICollection<DiemDanh> DiemDanhs { get; set; }
        public ICollection<BangDiem> BangDiems { get; set; }
    }
}
