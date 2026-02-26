using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnTouch.Domain.Entities
{
    public class UserSettings
    {
        public Guid Id { get; set; }

        public string UserId { get; set; }

        public ApplicationUser User { get; set; }

        public bool DarkMode { get; set; }

        public string AccessibilityLanguage { get; set; }

        public string PreferredLanguage { get; set; }
    }
}
