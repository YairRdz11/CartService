using CartService.BLL;
using CartService.Transversal.Classes.Messages;
using CartService.Transversal.Classes.Models.Events;
using System.Text.Json;

namespace CartService.API.Infrastructure.RabbitMq
{
    internal static class EventNames
    {
        public const string ProductDeleted = "ProductDeletedEvent";
        public const string ProductUpdated = "ProductUpdatedEvent";
        public const string CategoryUpdated = "CategoryUpdatedEvent";
    }

    public class ProductEventFacade : IProductEventFacade
    {
        private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
        private readonly ProductUpdateService _updateService;
        private readonly ILogger<ProductEventFacade> _logger;

        public ProductEventFacade(ProductUpdateService updateService, ILogger<ProductEventFacade> logger)
        {
            _updateService = updateService;
            _logger = logger;
        }

        public Task<ProductEventProcessingResult> ProcessAsync(string rawJson, CancellationToken ct)
        {
            var envelope = EventEnvelope.TryParse(rawJson, (ex, raw) => _logger.LogWarning(ex, "Malformed JSON payload. Raw={Raw}", raw));
            var type = envelope.EventType;
            if (type is null)
            {
                return Task.FromResult(new ProductEventProcessingResult { Success = false, Error = "Missing eventType", EventType = null });
            }

            ProductEventProcessingResult result = type switch
            {
                EventNames.ProductDeleted => HandleProductDeleted(envelope),
                EventNames.ProductUpdated => HandleProductUpdated(rawJson),
                EventNames.CategoryUpdated => HandleCategoryUpdated(rawJson),
                _ => new ProductEventProcessingResult { EventType = type, Success = false, Error = "Unhandled eventType" }
            };

            return Task.FromResult(result);
        }

         private ProductEventProcessingResult HandleProductDeleted(EventEnvelope env)
         {
            if (!env.ProductId.HasValue)
            {
                return new ProductEventProcessingResult { EventType = EventNames.ProductDeleted, Success = false, Error = "productId missing" };
            }
            var affected = _updateService.ApplyProductDeletion(env.ProductId.Value);
            return new ProductEventProcessingResult { EventType = EventNames.ProductDeleted, Success = true, AffectedCarts = affected };
         }

         private ProductEventProcessingResult HandleProductUpdated(string rawJson)
         {
            var msg = JsonSerializer.Deserialize<ProductUpdatedMessage>(rawJson, _jsonOptions);
            if (msg == null)
            {
                return new ProductEventProcessingResult { EventType = EventNames.ProductUpdated, Success = false, Error = "Invalid payload" };
            }
            var affected = _updateService.ApplyProductUpdate(msg.ProductId, msg.Name, msg.Price, msg.CategoryId);
            return new ProductEventProcessingResult { EventType = EventNames.ProductUpdated, Success = true, AffectedCarts = affected };
         }

         private ProductEventProcessingResult HandleCategoryUpdated(string rawJson)
         {
            var msg = JsonSerializer.Deserialize<CategoryUpdatedMessage>(rawJson, _jsonOptions);
            if (msg == null)
            {
                return new ProductEventProcessingResult { EventType = EventNames.CategoryUpdated, Success = false, Error = "Invalid payload" };
            }
            _logger.LogInformation("Category updated CategoryId={CategoryId} Name={Name}", msg.CategoryId, msg.Name);
            return new ProductEventProcessingResult { EventType = EventNames.CategoryUpdated, Success = true, AffectedCarts = 0 };
         }
    }
}
