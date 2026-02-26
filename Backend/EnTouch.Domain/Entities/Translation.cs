using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnTouch.Domain.Entities
{
    public enum TranslationType
    {
        SignToText = 0,
        TextToSign = 1
    }

    public enum TranslationStatus
    {
        Pending = 0,
        Processing = 1,
        Completed = 2,
        Failed = 3
    }

    public class Translation
    {
        public Guid Id { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public TranslationType Type { get; set; }

        public string? InputText { get; set; }
        public string? OutputText { get; set; }

        public string? InputVideoPath { get; set; }
        public string? OutputVideoPath { get; set; }

        public TranslationStatus Status { get; set; } = TranslationStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}