using Microsoft.EntityFrameworkCore;
using TaskManager.Models;

namespace TaskManager.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<TaskModel> Tasks { get; set; }
        public DbSet<User> Users { get; set; } // ✨ AJOUT DES UTILISATEURS

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuration des relations
            modelBuilder.Entity<TaskModel>()
                .HasOne(t => t.AssignedToUser)
                .WithMany(u => u.AssignedTasks)
                .HasForeignKey(t => t.AssignedToUserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<TaskModel>()
                .HasOne(t => t.CreatedByUser)
                .WithMany()
                .HasForeignKey(t => t.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Données de démarrage pour les utilisateurs
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    Name = "Youssef El Mansouri",
                    Email = "youssef@taskmanager.com",
                    Position = "Chef de Projet",
                    Department = "IT",
                    Bio = "Chef de projet expérimenté avec 5 ans d'expérience.",
                    AvatarUrl = "/images/avatars/youssef.jpg",
                    CreatedDate = DateTime.Now
                },
                new User
                {
                    Id = 2,
                    Name = "Amina Benali",
                    Email = "amina@taskmanager.com",
                    Position = "Développeuse Frontend",
                    Department = "IT",
                    Bio = "Spécialiste React et Vue.js",
                    AvatarUrl = "/images/avatars/amina.jpg",
                    CreatedDate = DateTime.Now
                },
                new User
                {
                    Id = 3,
                    Name = "Hassan Idrissi",
                    Email = "hassan@taskmanager.com",
                    Position = "Développeur Backend",
                    Department = "IT",
                    Bio = "Expert .NET et bases de données",
                    AvatarUrl = "/images/avatars/hassan.jpg",
                    CreatedDate = DateTime.Now
                },
                new User
                {
                    Id = 4,
                    Name = "Fatima Zahra",
                    Email = "fatima@taskmanager.com",
                    Position = "UX Designer",
                    Department = "Design",
                    Bio = "Créatrice d'expériences utilisateur exceptionnelles",
                    AvatarUrl = "/images/avatars/fatima.jpg",
                    CreatedDate = DateTime.Now
                },
                new User
                {
                    Id = 5,
                    Name = "Mohamed Alami",
                    Email = "mohamed@taskmanager.com",
                    Position = "Testeur QA",
                    Department = "Quality",
                    Bio = "Garantit la qualité de tous nos produits",
                    AvatarUrl = "/images/avatars/mohamed.jpg",
                    CreatedDate = DateTime.Now
                }
            );
        }
    }
}



//TaskModel.cs ApplicationDbContext.cs
//┌────────────────────┐         ┌──────────────────────────────┐
//│ public class TaskModel │◄────┤ public DbSet<TaskModel> Tasks│
//│ Id, Title, etc...       │     └──────────────────────────────┘
//└────────────────────┘
//        ▲
//        │
//        ▼
//Génère une table SQL appelée "Tasks"
