namespace MessangerBackend.Models
{
    public class Message
    {
        public int Id { get; set; }
        public string Body { get; set; } = string.Empty;
        public DateTime SendTime { get; set; } = DateTime.UtcNow;
        public bool IsRead { get; set; }
        public bool IsDelivered { get; set; }


        public int SenderId { get; set; }
        public int RecipientId { get; set; }


        public virtual User Sender { get; set; } = null!;
        public virtual User Recipient { get; set; } = null!;
    }
}
