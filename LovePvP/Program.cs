
using GladiatorHub.Models;
using GladiatorHub.Services.Implementation;
using GladiatorHub.Services.Interface;
using Microsoft.Extensions.Configuration;




var builder = WebApplication.CreateBuilder(args);
// Add configuration from appsettings.json
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// Bind the BlizzardSettings from the configuration
builder.Services.Configure<BlizzardSettings>(builder.Configuration.GetSection("Blizzard"));
builder.Services.AddHttpClient();
// Add services to the container.
builder.Services.AddControllersWithViews();


builder.Services.AddScoped<IBlizzardApiService,BlizzardApiService>();
builder.Services.AddScoped<IPlayerService,PlayerService>();
builder.Services.AddScoped<ILeaderboardService,LeaderboardService>();
builder.Services.AddScoped<IGameDataService,GameDataService>();



var app = builder.Build();

// Configure the HTTP request pipeline.
//if (!app.Environment.IsDevelopment())
//{
//    app.UseExceptionHandler("/Home/Error");
//    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
//    app.UseHsts();
//}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
