using TemporalDashboard.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Register services
builder.Services.AddSingleton<WorkflowDiscoveryService>();

// Configure CORS for Blazor app
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorApp", policy =>
    {
        policy.WithOrigins(
                "http://localhost:8000",
                "https://localhost:8000",
                "http://localhost:5000",
                "http://web:8080"
              )
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors("AllowBlazorApp");

app.UseAuthorization();

app.MapControllers();

app.Run();
