
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskManager.Models;

namespace TaskManager.data
{
    public class DatabaseSeeder
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public DatabaseSeeder(UserManager<User> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task SeedRolesAndUsersAsync()
        {
            Console.WriteLine("🌱 Seeder lancé...");
            string[] roles = { "Admin", "Membre" };

            foreach (var role in roles)
                if (!await _roleManager.RoleExistsAsync(role))
                    await _roleManager.CreateAsync(new IdentityRole(role));

            var admins = new List<(string FirstName, string LastName, string Email, string RecoveryEmail, string Phone, string Position, string Department, string Bio, string AvatarUrl)>
            {
                ("Youssef", "Guini", "youssefguini@sqli.com", "youssefguini2@gmail.com", "0612345678", "RH", "1er Etage", "Utilise Blazor, EF Core","/images/avatars/youssef.jpg"),
                ("Doha", "Bouchikhi", "bouchikhidoha@sqli.com", "bouchikhidoha2@gmail.com", "0640671338", "Chef de projet", ".NET", "Gestion des recrutements","/images/avatars/doha.jpg")
            };

            var membres = new List<(string FirstName, string LastName, string Email, string RecoveryEmail, string Phone, string Position, string Department, string Bio, string AvatarUrl, string ManagerEmail)>
            {
                ("Douae", "Bouch", "douaebouch@sqli.com", "dohabouchikhi9@gmail.com", "0611111222", "Stagiaire", "Symfony", "Développement Symfony, Twig","/images/avatars/default.jpg", "bouchikhidoha@sqli.com"),
                ("Nouha", "Benahmed", "nouha@sqli.com", "nouha.recovery@gmail.com", "0611111111", "Stagiaire", "Symfony", "Développement Symfony, Twig","/images/avatars/default.jpg", "youssefguini@sqli.com"),
                ("Zineb", "Didine", "zineb@sqli.com", "zineb.recovery@gmail.com", "0622222222", "Stagiaire", "IT", "Infrastructure et réseaux","/images/avatars/default.jpg", "bouchikhidoha@sqli.com"),
                ("Fatima", "Zahra", "fatima@sqli.com", "fatima@gmail.com", "0633333333", "Stagiaire", "Java", "Spring Boot, Hibernate","/images/avatars/default.jpg", "bouchikhidoha@sqli.com"),
                ("Mohamed", "Alami", "mohamed@sqli.com", "mohamed@gmail.com", "0644444444", "Stagiaire", ".NET", "C#, Razor Pages","/images/avatars/default.jpg", "youssefguini@sqli.com")
            };

            // Créer d'abord tous les admins
            foreach (var admin in admins)
                await CreateUser(admin, "Admin", "Admin@123", null);

            // Puis créer les membres avec leur manager
            foreach (var membre in membres)
            {
                var manager = await _userManager.FindByEmailAsync(membre.ManagerEmail);
                await CreateUser(membre, "Membre", "Password@123", manager?.Id);
            }
        }

        private async Task CreateUser(
            (string FirstName, string LastName, string Email, string RecoveryEmail, string Phone, string Position, string Department, string Bio, string AvatarUrl) data,
            string role, string password, string managerId)
        {
            var (firstName, lastName, email, recoveryEmail, phone, position, department, bio, avatarUrl) = data;

            if (await _userManager.FindByEmailAsync(email) == null)
            {
                var user = new User
                {
                    UserName = email,
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    RecoveryEmail = recoveryEmail,
                    PhoneNumber = phone,
                    Position = position,
                    Department = department,
                    Bio = bio,
                    EmailConfirmed = true,
                    CreatedDate = DateTime.Now,
                    AvatarUrl = avatarUrl,
                    ManagerId = managerId
                };

                var result = await _userManager.CreateAsync(user, password);
                if (result.Succeeded)
                {
                    Console.WriteLine($"✅ Utilisateur créé : {email}");
                    await _userManager.AddToRoleAsync(user, role);
                }
                else
                {
                    Console.WriteLine($"❌ Échec création utilisateur : {email}");
                    foreach (var error in result.Errors)
                    {
                        Console.WriteLine($"   - {error.Code}: {error.Description}");
                    }
                }
            }
        }

        private async Task CreateUser(
            (string FirstName, string LastName, string Email, string RecoveryEmail, string Phone, string Position, string Department, string Bio, string AvatarUrl, string ManagerEmail) data,
            string role, string password, string managerId)
        {
            var (firstName, lastName, email, recoveryEmail, phone, position, department, bio, avatarUrl, _) = data;
            await CreateUser((firstName, lastName, email, recoveryEmail, phone, position, department, bio, avatarUrl), role, password, managerId);
        }
    }
}