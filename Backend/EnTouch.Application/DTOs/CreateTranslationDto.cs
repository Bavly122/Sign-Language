using EnTouch.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnTouch.Application.DTOs
{
    public class CreateTranslationDto
    {
        public TranslationType Type { get; set; }

        public string? InputText { get; set; }

        public string? InputVideoPath { get; set; }
    }
}
