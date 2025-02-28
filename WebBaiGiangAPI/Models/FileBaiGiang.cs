using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebBaiGiangAPI.Models
{
    public class FileBaiGiang
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(10)]
        [Column(TypeName = "varchar(10)")]
        public string MaBaiGiang {  get; set; }

        [Required]
        [StringLength(10)]
        [Column(TypeName = "varchar(50)")]
        public string DuongDan { get; set; }

        [Required]
        [StringLength(10)]
        [Column(TypeName = ("nvarchar(10)"))]
        public string LoaiFile { get; set; }

        [ForeignKey("MaBaiGiang")]
        public BaiGiang? BaiGiang { get; set; }

    }
}
