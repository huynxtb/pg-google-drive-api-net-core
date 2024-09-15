using GoogleDriveAPI.Models;
using GoogleDriveAPI.Service;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.Configure<GoogleDriveApiOptionModel>(configuration.GetSection("GoogleAPI:Drive"));
builder.Services.AddScoped<IGoogleDriveService, GoogleDriveService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();