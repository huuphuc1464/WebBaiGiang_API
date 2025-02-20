using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace WebBaiGiangAPI.Models
{
    public class BaiGiang
    {
        [Key]
        [StringLength(10)]
        [Column(TypeName = "varchar(10)")]
        public string MaBaiGiang { get; set; }
        [Required]
        [StringLength(10)]
        [Column(TypeName = "varchar(10)")]
        public string MaHocPhan { get; set; }
        [Required]
        [StringLength(10)]
        [Column(TypeName = "varchar(10)")]
        public string MaLop { get; set; }

        [Required]
        [StringLength(50)]
        public string HinhAnh { get; set; }

        [StringLength(255)]
        public string MoTa { get; set; }

        [Required]
        [StringLength(10)]
        [Column(TypeName = "varchar(10)")]
        public string Chuong { get; set; }

        [Required]
        [StringLength(100)]
        public string TenBaiGiang { get; set; }

        [Required]
        [StringLength(50)]
        public string Link { get; set; }

        [Required]
        [StringLength(50)]
        public string Video { get; set; }

        [Required]
        [StringLength(255)]
        public string NoiDung { get; set; }

        [Required]
        [StringLength(1)]
        public string TrangThai { get; set; }

        // Foreign key
        [ForeignKey("MaLop")]
        public Lop Lop { get; set; }

        [ForeignKey("MaHocPhan")]
        public HocPhan HocPhan { get; set; }
    }
}
