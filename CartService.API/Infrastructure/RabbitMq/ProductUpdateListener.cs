using CartService.BLL;
using CartService.Transversal.Classes.Messages;
using Common.Utilities.Classes.Messaging.Options;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace CartService.API.Infrastructure.RabbitMq
{
    /// <summary>
    /// Background service that listens for product/category updates and product deletions from RabbitMQ and
    /// applies the changes to carts through <see cref="ProductUpdateService"/>.
    /// </summary>
    public class ProductUpdateListener : BackgroundService, IDisposable
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly IConnection _connection;
        private readonly RabbitMqOptions _settings;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ProductUpdateListener> _logger;
        private IChannel? _channel;

        public ProductUpdateListener(
            IConnection connection,
            IOptions<RabbitMqOptions> options,
            IServiceScopeFactory scopeFactory,
            ILogger<ProductUpdateListener> logger)
        {
            _connection = connection;
            _settings = options.Value;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _channel = await CreateAndConfigureChannelAsync(stoppingToken);

                var consumer = CreateConsumer(_channel, stoppingToken);

                await _channel.BasicConsumeAsync(
                    queue: _settings.Queue,
                    autoAck: false,
                    consumer: consumer,
                    cancellationToken: stoppingToken);

                _logger.LogInformation("ProductUpdateListener consuming. Queue: {Queue}", _settings.Queue);

                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Fatal error starting ProductUpdateListener.");
            }
        }

        private async Task<IChannel> CreateAndConfigureChannelAsync(CancellationToken token)
        {
            var channel = await _connection.CreateChannelAsync();

            await channel.ExchangeDeclareAsync(
                exchange: _settings.Exchange,
                type: ExchangeType.Fanout,
                durable: true,
                autoDelete: false,
                cancellationToken: token);

            await channel.QueueDeclareAsync(
                queue: _settings.Queue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: token);

            await channel.QueueBindAsync(
                queue: _settings.Queue,
                exchange: _settings.Exchange,
                routingKey: string.Empty,
                cancellationToken: token);

            if (_settings.PrefetchCount >0)
            {
                await channel.BasicQosAsync(
                    prefetchSize:0,
                    prefetchCount: _settings.PrefetchCount,
                    global: false,
                    cancellationToken: token);
            }

            _logger.LogInformation(
                "Channel configured. Exchange: {Exchange}, Queue: {Queue}, Prefetch: {Prefetch}",
                _settings.Exchange,
                _settings.Queue,
                _settings.PrefetchCount);

            return channel;
        }

        private AsyncEventingBasicConsumer CreateConsumer(IChannel channel, CancellationToken token)
        {
            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (_, ea) =>
            {
                await ProcessMessageAsync(ea.Body, ea.DeliveryTag, token);
            };
            return consumer;
        }

        private async Task ProcessMessageAsync(ReadOnlyMemory<byte> body, ulong deliveryTag, CancellationToken token)
        {
            try
            {
                var json = Encoding.UTF8.GetString(body.Span);

                // Parse eventType
                string? eventType = null;
                Guid? productId = null;
                Guid? categoryId = null;
                try
                {
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("eventType", out var etProp))
                    {
                        eventType = etProp.GetString();
                    }
                    if (root.TryGetProperty("productId", out var pidProp) && pidProp.TryGetGuid(out var pid))
                    {
                        productId = pid;
                    }
                    if (root.TryGetProperty("categoryId", out var cidProp) && cidProp.TryGetGuid(out var cid))
                    {
                        categoryId = cid;
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Malformed JSON message. Raw: {Raw}", json);
                }

                using var scope = _scopeFactory.CreateScope();
                var updateService = scope.ServiceProvider.GetRequiredService<ProductUpdateService>();

                switch (eventType)
                {
                    case "ProductDeletedEvent" when productId.HasValue:
                    {
                        var affectedDeletion = updateService.ApplyProductDeletion(productId.Value);
                        _logger.LogInformation("Deletion processed. ProductId: {ProductId}. Affected carts: {Affected}", productId, affectedDeletion);
                        break;
                    }
                    case "ProductUpdatedEvent":
                    {
                        var msg = JsonSerializer.Deserialize<ProductUpdatedMessage>(json, _jsonOptions);
                        if (msg == null)
                        {
                            _logger.LogWarning("Invalid ProductUpdatedEvent discarded. Raw: {Raw}", json);
                            break; // Ack anyway
                        }
                        var affectedUpdate = updateService.ApplyProductUpdate(msg.ProductId, msg.Name, msg.Price, msg.CategoryId);
                        _logger.LogInformation("Product update processed. ProductId: {ProductId}. Affected carts: {Affected}", msg.ProductId, affectedUpdate);
                        break;
                    }
                    case "CategoryUpdatedEvent":
                    {
                        // Currently no cart impact unless dependent logic is added; log only
                        var catMsg = JsonSerializer.Deserialize<CategoryUpdatedMessage>(json, _jsonOptions);
                        if (catMsg == null)
                        {
                            _logger.LogWarning("Invalid CategoryUpdatedEvent discarded. Raw: {Raw}", json);
                            break;
                        }
                        _logger.LogInformation("Category update received. CategoryId: {CategoryId}, Name: {Name}", catMsg.CategoryId, catMsg.Name);
                        break;
                    }
                    default:
                    {
                        if (eventType != null)
                        {
                            _logger.LogWarning("Unhandled eventType {EventType}. Raw: {Raw}", eventType, json);
                        }
                        else
                        {
                            _logger.LogWarning("Missing eventType. Raw: {Raw}", json);
                        }
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing product message.");
            }
            finally
            {
                if (_channel != null)
                {
                    try
                    {
                        await _channel.BasicAckAsync(deliveryTag, multiple: false, cancellationToken: token);
                    }
                    catch (Exception ackEx)
                    {
                        _logger.LogError(ackEx, "Failed to ACK message DeliveryTag {Tag}", deliveryTag);
                    }
                }
            }
        }

        public override void Dispose()
        {
            try
            {
                if (_channel != null)
                {
                    _channel.CloseAsync();
                    _channel.DisposeAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error disposing channel.");
            }
            base.Dispose();
        }
    }
}
