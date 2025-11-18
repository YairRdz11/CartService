using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CartService.Transversal.Classes.Models.Events
{
    public sealed class EventEnvelope
    {
        public string? EventType { get; init; }
        public Guid? ProductId { get; init; }
        public Guid? CategoryId { get; init; }
        public string RawJson { get; init; } = string.Empty;
    }
}
