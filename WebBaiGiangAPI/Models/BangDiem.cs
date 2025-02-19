using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace WebBaiGiangAPI.Models
{
    public class BangDiem
    {
        [Key]
        [StringLength(10)]
        [Column(TypeName = "varchar(10)")]
        public string MaBangDiem { get; set; }

        [Required]
        [StringLength(10)]
        [Column(TypeName = "varchar(10)")]
        public string MaLop { get; set; }

        [Required]
        [StringLength(10)]
        [Column(TypeName = "varchar(10)")]
        public string MaSinhVien { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ChuyenCan { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal HeSo1 { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal HeSo2 { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ThiLan1 { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? ThiLan2 { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TBKT { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TongKetLan1 { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? TongKetLan2 { get; set; }

        // Foreign keys
        [ForeignKey("MaLop")]
        public Lop Lop { get; set; }

        [ForeignKey("MaSinhVien")]
        public NguoiDung SinhVien { get; set; }
    }
}
