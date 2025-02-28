using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace WebBaiGiangAPI.Models
{
    public class Message
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MessageId { get; set; }

        public int? MessageSenderId { get; set; }

        public int? MessageReceiverId { get; set; }

        [MaxLength(50)]
        public string? MessageType { get; set; }

        [MaxLength(50)]
        public string? MessageSenderType { get; set; }

        public DateTime? MessageDate { get; set; }

        [MaxLength(255)]
        public string? MessageSubject { get; set; }

        public string? MessageContent { get; set; }

        [ForeignKey("MessageSenderId")]
        public Users Sender { get; set; }

        [ForeignKey("MessageReceiverId")]
        public Users Receiver { get; set; }
    }
}
