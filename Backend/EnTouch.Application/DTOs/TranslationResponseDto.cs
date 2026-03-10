using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnTouch.Application.DTOs
{
    public class TranslationResponseDto
    {
        public Guid Id { get; set; }
        public string Type { get; set; }
        public string? InputText { get; set; }
        public string? OutputText { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
