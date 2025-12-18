using Asp.Versioning;
using CartService.API.Infrastructure.RabbitMq;
using CartService.BLL;
using CartService.BLL.Classes;
using CartService.DAL.Classes;
using CartService.Transversal.Classes.Mappings;
using CartService.Transversal.Interfaces.BLL;
using CartService.Transversal.Interfaces.DAL;
using Common.Utilities.Classes.Messaging.Options;
using Common.Utilities.Classes.Messaging.Publisher;
using Common.Utilities.Interfaces.Messaging.Events;
using LiteDB;
using RabbitMQ.Client;

namespace CartService.API.Extensions
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
        {
            // AutoMapper
            services.AddAutoMapper(typeof(MappingProfile));

            // Database
            services.AddSingleton<LiteDatabase>(sp =>
            {
                var config = sp.GetRequiredService<IConfiguration>();
                var dbPath = config["LiteDb:Path"] ?? "/data/ShoppingCart.db";
                return new LiteDatabase(dbPath);
            });

            // Repositories
            services.AddScoped<ICartRepository, CartRepository>();

            // Services
            services.AddScoped<ICartService, CartServiceBL>();
            services.AddScoped<ProductUpdateService>();
            services.AddHostedService<ProductUpdateListener>();

            // HTTP Client
            services.AddHttpClient();

            // RabbitMQ
            services.Configure<RabbitMqOptions>(configuration.GetSection("RabbitMq"));
            services.AddSingleton<IConnection>(sp =>
            {
                var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<RabbitMqOptions>>().Value;
                var factory = new ConnectionFactory
                {
                    HostName = settings.Host,
                    Port = settings.Port,
                    UserName = settings.User,
                    Password = settings.Password
                };
                return factory.CreateConnectionAsync("cart-service-listener").GetAwaiter().GetResult();
            });
            services.AddScoped<IProductEventFacade, ProductEventFacade>();

            return services;
        }

        public static IServiceCollection AddApiVersioningConfiguration(this IServiceCollection services)
        {
            var apiVersioningBuilder = services.AddApiVersioning(options =>
            {
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.ReportApiVersions = true;
            });

            apiVersioningBuilder.AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

            return services;
        }
    }
}
