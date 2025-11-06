

using BlockedCountries.Business.Configuration;
using BlockedCountries.Business.Services;
using BlockedCountries.Data.Repositories;
using Hangfire;
using Hangfire.MemoryStorage;
using BlockedCountries.Api.Jobs;
using Serilog;
using BlockedCountries.Api.Handlers;

var builder = WebApplication.CreateBuilder(args);

// Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();
builder.Host.UseSerilog(Log.Logger);

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

// Caching
builder.Services.AddMemoryCache();

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


    client.DefaultRequestHeaders.UserAgent.ParseAdd("BlockedCountries/1.0 (+https://example.local)");
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
})
    .AddHttpMessageHandler<GeolocationRateLimitHandler>();


builder.Services.AddSingleton<GeolocationRateLimitHandler>();
builder.Services.AddScoped<ICountryManagementService, CountryManagementService>();
builder.Services.AddScoped<IIpBlockingService, IpBlockingService>();
builder.Services.AddScoped<IBlockedAttemptService, BlockedAttemptService>();



builder.Services.AddHangfire(config =>
{
    config.UseSimpleAssemblyNameTypeSerializer();
    config.UseRecommendedSerializerSettings();
    config.UseMemoryStorage();
});
builder.Services.AddHangfireServer();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Blocked Countries API v1");
        c.RoutePrefix = "swagger"; 
    });
}



// Serilog request logging
app.UseSerilogRequestLogging();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Register recurring jobs
var recurringJobs = app.Services.GetRequiredService<IRecurringJobManager>();
recurringJobs.AddOrUpdate<TemporalBlockCleanupJob>(
    "temporal-block-cleanup",
    job => job.Run(),
    "*/5 * * * *",
    TimeZoneInfo.Utc
);

app.Run();
