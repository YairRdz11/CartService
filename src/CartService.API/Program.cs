using CartService.API.Extensions;
using Common.ApiUtilities.Middleware;
using Common.Utilities.Classes.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Configure services
builder.Services.AddControllers();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorizationPolicies();
builder.Services.AddSwaggerDocumentation(builder.Configuration);
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddApiVersioningConfiguration();

var app = builder.Build();

// Using custom middleware
app.UseGlobalExceptionHandling();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerDocumentation();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
