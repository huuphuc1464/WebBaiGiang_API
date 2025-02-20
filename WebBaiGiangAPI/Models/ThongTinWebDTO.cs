using System.ComponentModel.DataAnnotations;

namespace WebBaiGiangAPI.Models
{
    public class ThongTinWebDTO
    {
        [Required]
        [StringLength(100)]
        public string TenWeb { get; set; }

        [Required]
        [StringLength(150)]
        public string DiaChi { get; set; }

        [Required]
        [StringLength(10)]
        public string SDT { get; set; }

        [Required]
        [StringLength(50)]
        public string Email { get; set; }

        [Required]
        [StringLength(50)]
        public string Facebook { get; set; }

        [Required]
        [StringLength(50)]
        public string Gmail { get; set; }

        [Required]
        [StringLength(20)]
        public string Fax { get; set; }

        [Required]
        [StringLength(50)]
        public string Website { get; set; }
    }
}
