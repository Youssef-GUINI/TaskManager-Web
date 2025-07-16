using Microsoft.EntityFrameworkCore; //qui permet de manipuler des bases de données avec du code C#.
using TaskManager.Models; // ou le namespace de TaskModel.cs

namespace TaskManager.Data  // ← IMPORTANT : doit être exactement ça
{
    public class ApplicationDbContext : DbContext //Elle hérite de la classe DbContext d’Entity Framework
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<TaskModel> Tasks { get; set; }
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
