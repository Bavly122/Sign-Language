using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnTouch.Application.DTOs
{
    public class AuthResponse
    {
        public string Token { get; set; }
        public string RefreshToken { get; set; }
        public string Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public bool IsDeaf { get; set; }
        public bool IsMute { get; set; }
        public string PreferredLanguage { get; set; }
    }
}
