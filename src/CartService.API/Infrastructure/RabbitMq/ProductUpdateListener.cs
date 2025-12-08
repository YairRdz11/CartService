using CartService.API.Infrastructure.RabbitMq; // For IProductEventFacade
using Common.Utilities.Classes.Messaging.Options;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace CartService.API.Infrastructure.RabbitMq
{
    /// <summary>
    /// Background service that listens for product/category updates and product deletions from RabbitMQ and
    /// delegates processing to <see cref="IProductEventFacade"/>.
    /// </summary>
    public class ProductUpdateListener : BackgroundService, IDisposable
    {
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
                await _channel.BasicConsumeAsync(_settings.Queue, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);
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
            await channel.ExchangeDeclareAsync(_settings.Exchange, ExchangeType.Fanout, durable: true, autoDelete: false, cancellationToken: token);
            await channel.QueueDeclareAsync(_settings.Queue, durable: true, exclusive: false, autoDelete: false, arguments: null, cancellationToken: token);
            await channel.QueueBindAsync(_settings.Queue, _settings.Exchange, routingKey: string.Empty, cancellationToken: token);
            if (_settings.PrefetchCount >0)
            {
                await channel.BasicQosAsync(0, _settings.PrefetchCount, global: false, cancellationToken: token);
            }
            _logger.LogInformation("Channel configured. Exchange: {Exchange}, Queue: {Queue}, Prefetch: {Prefetch}", _settings.Exchange, _settings.Queue, _settings.PrefetchCount);
            return channel;
        }

        private AsyncEventingBasicConsumer CreateConsumer(IChannel channel, CancellationToken token)
        {
            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (_, ea) => await ProcessMessageAsync(ea.Body, ea.DeliveryTag, token);
            return consumer;
        }

        private async Task ProcessMessageAsync(ReadOnlyMemory<byte> body, ulong deliveryTag, CancellationToken token)
        {
            try
            {
                var json = Encoding.UTF8.GetString(body.Span);
                using var scope = _scopeFactory.CreateScope();
                var facade = scope.ServiceProvider.GetRequiredService<IProductEventFacade>();
                var result = await facade.ProcessAsync(json, token);
                if (result.Success)
                {
                    _logger.LogInformation("Event processed Type={Type} AffectedCarts={Affected}", result.EventType, result.AffectedCarts);
                }
                else
                {
                    _logger.LogWarning("Event failed Type={Type} Error={Error} Raw={Raw}", result.EventType ?? "<null>", result.Error, json);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message.");
            }
            finally
            {
                if (_channel != null)
                {
                    try { await _channel.BasicAckAsync(deliveryTag, false, token); }
                    catch (Exception ackEx) { _logger.LogError(ackEx, "Failed to ACK DeliveryTag {Tag}", deliveryTag); }
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
