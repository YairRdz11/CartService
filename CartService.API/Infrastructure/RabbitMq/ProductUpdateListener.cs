using CartService.Transversal.Classes.Messages;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using CartService.BLL; 

namespace CartService.API.Infrastructure.RabbitMq
{
    public class ProductUpdateListener : BackgroundService, IDisposable
    {
        private readonly IConnection _connection;
        private readonly RabbitMqSettings _settings;
        private readonly IServiceScopeFactory _scopeFactory; // added
        private readonly ILogger<ProductUpdateListener> _logger;
        private IChannel _channel;

        public ProductUpdateListener(
            IConnection connection,
            IOptions<RabbitMqSettings> options,
            IServiceScopeFactory scopeFactory, // replaced ProductUpdateService
            ILogger<ProductUpdateListener> logger)
        {
            _connection = connection;
            _settings = options.Value;
            _scopeFactory = scopeFactory; // assign
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _channel = await _connection.CreateChannelAsync();

            // Declare exchange (fanout)
            await _channel.ExchangeDeclareAsync(_settings.Exchange, type: ExchangeType.Fanout, durable: true, autoDelete: false, cancellationToken: stoppingToken);

            // Declare (or ensure) the configured queue, then bind it to the exchange
            await _channel.QueueDeclareAsync(queue: _settings.Queue, durable: true, exclusive: false, autoDelete: false, arguments: null, cancellationToken: stoppingToken);
            await _channel.QueueBindAsync(queue: _settings.Queue, exchange: _settings.Exchange, routingKey: string.Empty, cancellationToken: stoppingToken);

            // Optional: set QoS / prefetch
            if (_settings.PrefetchCount >0)
            {
                await _channel.BasicQosAsync(prefetchSize:0, prefetchCount: _settings.PrefetchCount, global: false, cancellationToken: stoppingToken);
            }

            _logger.LogInformation("ProductUpdateListener started. Queue: {Queue}", _settings.Queue);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                try
                {
                    var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                    var msg = JsonSerializer.Deserialize<ProductUpdatedMessage>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (msg == null)
                    {
                        _logger.LogWarning("Received null or invalid message. Raw: {Raw}", json);
                        await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                        return;
                    }

                    using var scope = _scopeFactory.CreateScope();
                    var updateService = scope.ServiceProvider.GetRequiredService<ProductUpdateService>();
                    var affected = updateService.ApplyProductUpdate(msg.ProductId, msg.Name, msg.Price, msg.CategoryId);
                    _logger.LogInformation("Processed product update {ProductId}. Carts affected: {Affected}", msg.ProductId, affected);

                    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message");
                    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                }
            };

            // Consume from the configured queue
            await _channel.BasicConsumeAsync(queue: _settings.Queue, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);

            // Keep the task alive until cancellation
            try
            {
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // normal shutdown
            }
        }

        public override void Dispose()
        {
            _channel?.CloseAsync();
            _channel?.DisposeAsync();
            base.Dispose();
        }
    }
}
