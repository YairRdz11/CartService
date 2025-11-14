namespace CartService.API.Infrastructure.RabbitMq
{
    public class RabbitMqSettings
    {
        public string HostName { get; set; } = "localhost";
        public int Port { get; set; } = 5672;
        public string UserName { get; set; } = "guest";
        public string Password { get; set; } = "guest";
        public string Exchange { get; set; } = "catalog.product.exchange";
        public string Queue { get; set; } = "cart.product-updates.q";
        public string RoutingKey { get; set; } = "product.updated";
        public ushort PrefetchCount { get; set; } = 20;
    }
}
