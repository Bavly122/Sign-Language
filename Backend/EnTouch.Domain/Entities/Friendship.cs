using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnTouch.Domain.Entities
{
    public class Friendship
    {
        public Guid Id { get; set; }

        public string RequesterId { get; set; }
        public ApplicationUser Requester { get; set; }

        public string AddresseeId { get; set; }
        public ApplicationUser Addressee { get; set; }

        public string Status { get; set; } // Pending / Accepted / Rejected

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
