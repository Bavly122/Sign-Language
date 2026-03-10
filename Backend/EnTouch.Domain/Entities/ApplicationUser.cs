using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace EnTouch.Domain.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; }

        public bool IsDeaf { get; set; }

        public bool IsMute { get; set; }

        public string? ProfileImageUrl { get; set; }

        public string PreferredLanguage { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastSeen { get; set; }
    }
}