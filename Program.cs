using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using TaskManager.data;
using TaskManager.Helpers;
using Microsoft.AspNetCore.Identity;
using TaskManager.Models;
using Microsoft.AspNetCore.Identity.UI.Services;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Http.Features; // Pour FormOptions

var builder = WebApplication.CreateBuilder(args);

// Configuration des services
builder.Services.AddControllersWithViews()
    .AddRazorOptions(options =>
    {
        // Configuration correcte des Areas
        options.AreaViewLocationFormats.Clear();
        options.AreaViewLocationFormats.Add("/Areas/{2}/Views/{1}/{0}.cshtml");
        options.AreaViewLocationFormats.Add("/Areas/{2}/Views/Shared/{0}.cshtml");
        options.AreaViewLocationFormats.Add("/Views/Shared/{0}.cshtml");

        options.ViewLocationFormats.Clear();
        options.ViewLocationFormats.Add("/Views/{1}/{0}.cshtml");
        options.ViewLocationFormats.Add("/Views/Shared/{0}.cshtml");
    });

// Configuration de la base de données avec MySQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
});

builder.Services.AddHttpClient();

// Configuration de l'identité
builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.User.RequireUniqueEmail = true;
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// ✨ Configuration de SignalR
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true; // Pour le debugging
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
    options.HandshakeTimeout = TimeSpan.FromSeconds(15);
    options.MaximumReceiveMessageSize = 32768; // 32KB par défaut
});

// Configuration des autorisations
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("Member", policy => policy.RequireRole("Member"));
});

// Configuration de l'antiforgery token (sécurité AJAX)
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
});

// Configuration de la localisation (pour les exports)
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[]
    {
        new CultureInfo("fr-FR"),
        new CultureInfo("en-US")
    };

    options.DefaultRequestCulture = new RequestCulture("fr-FR");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

// Services pour l'application
builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.AddScoped<DatabaseSeeder>();

// Configuration pour les fichiers volumineux (exports) - CORRECTION
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 52428800; // 50MB
    options.ValueLengthLimit = 52428800;
    options.ValueCountLimit = 1024;
});

// Configuration du logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
if (builder.Environment.IsDevelopment())
{
    builder.Logging.SetMinimumLevel(LogLevel.Debug);
}

var app = builder.Build();

// Configuration du pipeline middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Configuration de la localisation
app.UseRequestLocalization();

app.UseRouting();

// Authentification et autorisation
app.UseAuthentication();
app.UseAuthorization();

// Middleware pour éviter la mise en cache des réponses sensibles
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/Admin") ||
        context.Request.Path.StartsWithSegments("/Member"))
    {
        context.Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
        context.Response.Headers.Append("Pragma", "no-cache");
        context.Response.Headers.Append("Expires", "0");
    }
    await next();
});

// Configuration des routes
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "chatbot",
    pattern: "ChatBot/{action=Index}/{id?}",
    defaults: new { controller = "ChatBot" });

app.MapControllerRoute(
    name: "team",
    pattern: "Team/{action=Index}/{id?}",
    defaults: new { controller = "Team" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

// ✨ Configuration du hub SignalR
app.MapHub<ChatHub>("/chathub");

// Initialisation de la base de données avec gestion d'erreurs améliorée
Console.WriteLine("🔄 Démarrage de l'application TaskManager...");

try
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<ApplicationDbContext>();
        var logger = services.GetRequiredService<ILogger<Program>>();

        try
        {
            Console.WriteLine("🔄 Vérification de la connexion à la base de données...");

            // Test de connexion
            await context.Database.CanConnectAsync();
            Console.WriteLine("✅ Connexion à la base de données établie!");

            Console.WriteLine("🔄 Application des migrations...");
            await context.Database.MigrateAsync();
            Console.WriteLine("✅ Migrations appliquées avec succès!");

            Console.WriteLine("🌱 Initialisation des données de base...");
            var seeder = services.GetRequiredService<DatabaseSeeder>();
            await seeder.SeedRolesAndUsersAsync();
            Console.WriteLine("✅ Données initiales créées avec succès!");
        }
        catch (Exception dbEx)
        {
            logger.LogError(dbEx, "Erreur lors de l'initialisation de la base de données");
            Console.WriteLine($"❌ Erreur de base de données: {dbEx.Message}");

            // Fallback: utiliser EnsureCreated si les migrations échouent
            Console.WriteLine("🔄 Tentative de création directe de la base...");
            try
            {
                await context.Database.EnsureCreatedAsync();
                var seeder = services.GetRequiredService<DatabaseSeeder>();
                await seeder.SeedRolesAndUsersAsync();
                Console.WriteLine("✅ Base créée en mode fallback!");
            }
            catch (Exception fallbackEx)
            {
                logger.LogError(fallbackEx, "Échec de la création en mode fallback");
                Console.WriteLine($"❌ Erreur critique: {fallbackEx.Message}");
                Console.WriteLine("⚠️ L'application va démarrer sans initialisation de base de données");
            }
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Erreur lors de l'initialisation des services: {ex.Message}");
    Console.WriteLine("⚠️ L'application va quand même démarrer...");
}

Console.WriteLine("\n🚀 Application TaskManager prête!");
Console.WriteLine("🌐 URLs disponibles:");
Console.WriteLine("   • Local: https://localhost:5001");
Console.WriteLine("   • Réseau: http://localhost:5000");

Console.WriteLine("\n📋 Comptes de test disponibles:");
Console.WriteLine("   👑 Admin: youssefguini@sqli.com / Admin@123");
Console.WriteLine("   👑 Admin: bouchikhidoha@sqli.com / Admin@123");
Console.WriteLine("   👤 Membre: douaebouch@sqli.com / Password@123");

Console.WriteLine("\n📥 Fonctionnalités d'export:");
Console.WriteLine("   ✅ Export PDF avec iText7 (formatage avancé)");
Console.WriteLine("   ✅ Export Excel avec ClosedXML (coloration conditionnelle)");
Console.WriteLine("   ✅ Filtres et tri respectés dans les exports");
Console.WriteLine("   ✅ Sécurité: seules les tâches autorisées sont exportées");

Console.WriteLine("\n🔧 Fonctionnalités supplémentaires:");
Console.WriteLine("   ✅ Drag & Drop Kanban");
Console.WriteLine("   ✅ Gestion des équipes");
Console.WriteLine("   ✅ ChatBot intégré");
Console.WriteLine("   ✅ Notifications en temps réel avec SignalR"); // Mise à jour
Console.WriteLine("   ✅ WebSockets pour communication bidirectionnelle"); // Nouveau

app.Run();