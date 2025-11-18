namespace CartService.API.Infrastructure.RabbitMq
{
    public interface IProductEventFacade
    {
        Task<ProductEventProcessingResult> ProcessAsync(string rawJson, CancellationToken ct);
    }

    public sealed class ProductEventProcessingResult
    {
        public string? EventType { get; init; }
        public bool Success { get; init; }
        public int AffectedCarts { get; init; }
        public string? Error { get; init; }
    }
}
