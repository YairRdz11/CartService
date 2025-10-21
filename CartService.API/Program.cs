using CartService.BLL.Classes;
using CartService.DAL.Classes;
using CartService.Transversal.Classes.Mappings;
using CartService.Transversal.Interfaces.BLL;
using CartService.Transversal.Interfaces.DAL;
using LiteDB;
using YairUtilities.ApiUtilities.Middleware;

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
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Using custom middleware
//app.UseGlobalExceptionHandling();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
