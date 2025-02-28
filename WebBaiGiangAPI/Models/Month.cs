using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WebBaiGiangAPI.Models
{
    [Index(nameof(MonthTitle), IsUnique = true)]

    public class Month
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MonthId { get; set; }

        [Required]
        [MaxLength(255)]
        public string MonthTitle { get; set; }

        public ICollection<Exam> Exams { get; set; } = new List<Exam>();

    }
}
