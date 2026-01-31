using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using TemporalDashboard.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Configure HttpClient for API client
builder.Services.AddHttpClient<ApiClient>(client =>
{
    var apiUrl = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7001";
    client.BaseAddress = new Uri(apiUrl);
    client.Timeout = TimeSpan.FromMinutes(5); // Allow time for large file uploads
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
