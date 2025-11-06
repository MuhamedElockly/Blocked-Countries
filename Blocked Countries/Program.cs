using BlockedCountries.Api.Middleware;
using BlockedCountries.Api.Services;
using BlockedCountries.Business.Configuration;
using BlockedCountries.Business.Services;
using BlockedCountries.Data.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Blocked Countries API",
        Version = "v1",
        Description = "API for managing blocked countries and IP address validation using geolocation services"
    });
});

// Configuration
builder.Services.Configure<GeolocationApiConfig>(
    builder.Configuration.GetSection(GeolocationApiConfig.SectionName));

// Data Layer - Repositories
builder.Services.AddSingleton<ICountryRepository, CountryRepository>();
builder.Services.AddSingleton<IBlockedAttemptRepository, BlockedAttemptRepository>();

// Business Layer - Services
builder.Services.AddHttpClient<IGeolocationService, GeolocationService>(client =>
{
    var config = builder.Configuration.GetSection(GeolocationApiConfig.SectionName).Get<GeolocationApiConfig>();
    if (config != null)
    {
        client.BaseAddress = new Uri(config.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(30);
    }
});
builder.Services.AddScoped<ICountryManagementService, CountryManagementService>();
builder.Services.AddScoped<IIpBlockingService, IpBlockingService>();
builder.Services.AddScoped<IBlockedAttemptService, BlockedAttemptService>();
builder.Services.AddScoped<IIpValidationService, IpValidationService>();

// Background Service
builder.Services.AddHostedService<TemporalBlockCleanupService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Blocked Countries API v1");
        c.RoutePrefix = "swagger"; 
    });
}

// Configure custom middleware to extract user IP address
app.UseClientIpMiddleware();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
