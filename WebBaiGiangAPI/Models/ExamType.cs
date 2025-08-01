﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace WebBaiGiangAPI.Models
{
    public class ExamType
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int EtypeId { get; set; }

        [Required]
        [MaxLength(255)]
        public string EtypeTitle { get; set; }

        public string EtypeDescription { get; set; }
        [JsonIgnore]
        public ICollection<Exam> Exams { get; set; } = new List<Exam>();

    }
}
