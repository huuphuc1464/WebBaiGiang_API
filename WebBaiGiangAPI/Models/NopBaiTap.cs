using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace WebBaiGiangAPI.Models
{
    public class NopBaiTap
    {
        [Key]
        [StringLength(10)]
        [Column(TypeName = "varchar(10)")]
        public string MaNopBai { get; set; }

        [Required]
        [StringLength(10)]
        [Column(TypeName = "varchar(10)")]
        public string MaBaiTap { get; set; }

        [Required]
        [StringLength(10)]
        [Column(TypeName = "varchar(10)")]
        public string MaSinhVien { get; set; }

        [Required]
        [StringLength(50)]
        public string FileNop { get; set; }

        [Required]
        public DateTime NgayNop { get; set; }

        [Required]
        [StringLength(1)]
        public string TrangThai { get; set; }

        // Foreign keys
        [ForeignKey("MaBaiTap")]
        public BaiTap BaiTap { get; set; }

        [ForeignKey("MaSinhVien")]
        public NguoiDung SinhVien { get; set; }
    }
}
