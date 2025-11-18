using System;
using System.Text.Json;

namespace CartService.Transversal.Classes.Models.Events
{
    public sealed class EventEnvelope
    {
        public string? EventType { get; init; }
        public Guid? ProductId { get; init; }
        public Guid? CategoryId { get; init; }
        public string RawJson { get; init; } = string.Empty;

        public static EventEnvelope TryParse(string json, Action<Exception,string>? logWarning = null)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                string? type = root.TryGetProperty("eventType", out var et) ? et.GetString() : null;
                Guid? productId = root.TryGetProperty("productId", out var pid) && pid.TryGetGuid(out var pGuid) ? pGuid : null;
                Guid? categoryId = root.TryGetProperty("categoryId", out var cid) && cid.TryGetGuid(out var cGuid) ? cGuid : null;
                return new EventEnvelope
                {
                    EventType = type,
                    ProductId = productId,
                    CategoryId = categoryId,
                    RawJson = json
                };
            }
            catch (JsonException ex)
            {
                logWarning?.Invoke(ex, json);
                return new EventEnvelope { RawJson = json }; // EventType null triggers unknown handling.
            }
        }
    }
}
