using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace WebBaiGiangAPI.Models
{
    public class BaiTap
    {
        [Key]
        [StringLength(10)]
        [Column(TypeName = "varchar(10)")]
        public string MaBaiTap { get; set; }

        [Required]
        [StringLength(10)]
        [Column(TypeName = "varchar(10)")]
        public string MaLop { get; set; }

        [Required]
        [StringLength(10)]
        [Column(TypeName = "varchar(10)")]
        public string MaNguoiTao { get; set; }

        [Required]
        [StringLength(100)]
        public string TenBaiTap { get; set; }

        [Required]
        [StringLength(255)]
        public string NoiDung { get; set; }

        [StringLength(50)]
        public string LinkBaiTap { get; set; }

        [StringLength(50)]
        public string FileBaiTap { get; set; }

        public DateTime? HanNop { get; set; }

        [Required]
        public DateTime NgayTao { get; set; }

        [Required]
        public DateTime NgayBatDau { get; set; }

        [StringLength(1)]
        public string TrangThai { get; set; }

        // Foreign keys
        [ForeignKey("MaLop")]
        public Lop Lop { get; set; }

        [ForeignKey("MaNguoiTao")]
        public NguoiDung NguoiTao { get; set; }

        public ICollection<NopBaiTap> NopBaiTaps { get; set; }
    }
}
