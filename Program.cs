using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using TaskManager.data;
using TaskManager.Helpers;
using Microsoft.AspNetCore.Identity;
using TaskManager.Models;
using Microsoft.AspNetCore.Identity.UI.Services;

var builder = WebApplication.CreateBuilder(args);

// 🔧 Services
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(8, 0, 33))
    ));

builder.Services.AddHttpClient();

builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
});

// 🔐 Identity
builder.Services.AddIdentity<User, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
});



// 📧 EmailSender (concret)
builder.Services.AddTransient<IEmailSender, EmailSender>(); // ✅ Ajouté
builder.Services.AddHttpContextAccessor();
var app = builder.Build();

// 🔧 Middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication(); // ✅ Identity
app.UseAuthorization();

 app.MapControllerRoute(
      name: "default",
      pattern: "{controller=Account}/{action=Login}/{id?}");

 app.MapControllerRoute(
     name: "workspace",
     pattern: "Workspace/{action=Index}/{id?}",
     defaults: new { controller = "Workspace" });



app.MapControllerRoute(
    name: "tasks",
    pattern: "Tasks/{action=Index}/{id?}",
    defaults: new { controller = "Tasks" });

app.MapControllerRoute(
    name: "timesheets",
    pattern: "Timesheets/{action=Index}/{id?}",
    defaults: new { controller = "Timesheets" });

app.MapControllerRoute(
    name: "teams",
    pattern: "Teams/{action=Index}/{id?}",
    defaults: new { controller = "Teams" });

app.MapControllerRoute(
    name: "chatbot",
    pattern: "ChatBot/{action=Index}/{id?}",
    defaults: new { controller = "ChatBot" });

// 🔗 SignalR Hub
app.MapHub<ChatHub>("/chathub");

// 🌱 Seeder : rôles et utilisateurs
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var userManager = services.GetRequiredService<UserManager<User>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

    // Créer une instance du seeder
var seeder = new DatabaseSeeder(userManager, roleManager);
// Appeler la méthode
await seeder.SeedRolesAndUsersAsync();
}
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.Run();



