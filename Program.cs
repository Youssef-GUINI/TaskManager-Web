using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TaskManager.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Ajouter HttpClient pour les appels API (si nécessaire plus tard)
builder.Services.AddHttpClient();

var app = builder.Build();

// Configuration
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// Routes - IMPORTANT : L'ordre compte !
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Workspace}/{action=Index}/{id?}");

// Route spécifique pour ChatBot (optionnel, mais peut aider)
app.MapControllerRoute(
    name: "chatbot",
    pattern: "ChatBot/{action=Index}/{id?}",
    defaults: new { controller = "ChatBot" });

app.Run();