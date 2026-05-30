using SignalWatch.Api.Models;
using SignalWatch.Api.Services;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT");

if (!string.IsNullOrWhiteSpace(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<BrightDataOptions>(
    builder.Configuration.GetSection("BrightData"));

builder.Services.Configure<AiOptions>(
    builder.Configuration.GetSection("AI"));

builder.Services.AddHttpClient<BrightDataService>();
builder.Services.AddHttpClient<AiAnalysisService>();

builder.Services.AddScoped<SignalOrchestratorService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("SignalWatchCors", policy =>
    {
        var allowedOrigins = builder.Configuration
            .GetSection("AllowedOrigins")
            .Get<string[]>() ?? Array.Empty<string>();

        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("SignalWatchCors");

app.UseAuthorization();

app.MapControllers();

app.Run();