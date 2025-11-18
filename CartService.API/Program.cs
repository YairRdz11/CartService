using Asp.Versioning;
using CartService.API.Infrastructure.RabbitMq;
using CartService.BLL;
using CartService.BLL.Classes;
using CartService.DAL.Classes;
using CartService.Transversal.Classes.Mappings;
using CartService.Transversal.Interfaces.BLL;
using CartService.Transversal.Interfaces.DAL;
using Common.ApiUtilities.Middleware;
using Common.Utilities.Classes.Messaging.Options;
using LiteDB;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.


// Register LiteDB as singleton
builder.Services.AddSingleton<LiteDatabase>(_ =>
{
    var dbPath = "ShoppingCart.db";
    return new LiteDatabase(dbPath);
});

// Register AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// Register Business Logic and Data Access Layer services
builder.Services.AddScoped<ICartService, CartServiceBL>();
builder.Services.AddScoped<ICartRepository, CartRepository>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "Cart API", Version = "v1" });
});

builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMq"));

builder.Services.AddSingleton<IConnection>(sp =>
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
builder.Services.AddScoped<IProductEventFacade, ProductEventFacade>();

builder.Services.AddScoped<ProductUpdateService>();
builder.Services.AddHostedService <ProductUpdateListener>();

var apiVersioningBuilder = builder.Services.AddApiVersioning(options =>
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

var app = builder.Build();

// Using custom middleware
app.UseGlobalExceptionHandling();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(c =>
    {
        c.RouteTemplate = "swagger/{documentName}/swagger.json";
    });

    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("v1/swagger.json", "Project API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
