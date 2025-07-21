using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TaskManager.Data;
using TaskManager.Hubs; // ← LIGNE AJOUTÉE
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Services existants
builder.Services.AddControllersWithViews();

// ✨ CHANGEMENT : MySQL au lieu de SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(8, 0, 33)) // Spécifiez votre version MySQL
    ));
builder.Services.AddHttpClient();

// ✨ AJOUT SIGNALR
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
});

var app = builder.Build();

// Configuration existante
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// Routes existantes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Workspace}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "chatbot",
    pattern: "ChatBot/{action=Index}/{id?}",
    defaults: new { controller = "ChatBot" });

// ✨ HUB SIGNALR
app.MapHub<ChatHub>("/chathub");

app.Run();