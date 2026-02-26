using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnTouch.Domain.Entities
{
    public class DictionarySign
    {
        public Guid Id { get; set; }

        public string Word { get; set; }

        public string Language { get; set; }

        public string VideoPath { get; set; }

        public string? Description { get; set; }
    }
}
