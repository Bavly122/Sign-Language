using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnTouch.Domain.Entities
{
    public class Message
    {
        public Guid Id { get; set; }

        public string SenderId { get; set; }
        public ApplicationUser Sender { get; set; }

        public string ReceiverId { get; set; }
        public ApplicationUser Receiver { get; set; }

        public string MessageType { get; set; } // Text / Video / Sign

        public string Content { get; set; }

        public string? VideoPath { get; set; }

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; } = false;

        public bool IsDelivered { get; set; } = false;
    }
}
