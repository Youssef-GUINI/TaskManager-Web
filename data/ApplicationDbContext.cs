using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using TaskManager.Models;

namespace TaskManager.data
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<TaskModel> Tasks { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; } // NOUVEAU: Messages de chat

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuration COMPLÈTE pour MySQL - clés ULTRA COURTES

            // 1. Table AspNetRoles
            modelBuilder.Entity<IdentityRole>(entity =>
            {
                entity.Property(e => e.Id).HasMaxLength(50);
                entity.Property(e => e.Name).HasMaxLength(50);
                entity.Property(e => e.NormalizedName).HasMaxLength(50);
                entity.Property(e => e.ConcurrencyStamp).HasMaxLength(50);
            });

            // 2. Table AspNetUsers (User)
            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(e => e.Id).HasMaxLength(50);
                entity.Property(e => e.Email).HasMaxLength(100);
                entity.Property(e => e.NormalizedEmail).HasMaxLength(100);
                entity.Property(e => e.UserName).HasMaxLength(100);
                entity.Property(e => e.NormalizedUserName).HasMaxLength(100);
                entity.Property(e => e.SecurityStamp).HasMaxLength(50);
                entity.Property(e => e.ConcurrencyStamp).HasMaxLength(50);
                entity.Property(e => e.PhoneNumber).HasMaxLength(50);
                entity.Property(e => e.FirstName).HasMaxLength(100);
                entity.Property(e => e.LastName).HasMaxLength(100);
                entity.Property(e => e.RecoveryEmail).HasMaxLength(100);
                entity.Property(e => e.Position).HasMaxLength(100);
                entity.Property(e => e.Department).HasMaxLength(100);
                entity.Property(e => e.Bio).HasMaxLength(500);
                entity.Property(e => e.AvatarUrl).HasMaxLength(200);
                entity.Property(e => e.ManagerId).HasMaxLength(50);

                // Configuration de la relation Manager/Employé (auto-référence)
                entity.HasOne(u => u.Manager)
                      .WithMany()
                      .HasForeignKey(u => u.ManagerId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Index pour optimiser les recherches par manager
                entity.HasIndex(u => u.ManagerId);
            });

            // 3. Table AspNetUserRoles
            modelBuilder.Entity<IdentityUserRole<string>>(entity =>
            {
                entity.Property(e => e.UserId).HasMaxLength(50);
                entity.Property(e => e.RoleId).HasMaxLength(50);
            });

            // 4. Table AspNetUserClaims
            modelBuilder.Entity<IdentityUserClaim<string>>(entity =>
            {
                entity.Property(e => e.UserId).HasMaxLength(50);
                entity.Property(e => e.ClaimType).HasMaxLength(100);
                entity.Property(e => e.ClaimValue).HasMaxLength(200);
            });

            // 5. Table AspNetRoleClaims
            modelBuilder.Entity<IdentityRoleClaim<string>>(entity =>
            {
                entity.Property(e => e.RoleId).HasMaxLength(50);
                entity.Property(e => e.ClaimType).HasMaxLength(100);
                entity.Property(e => e.ClaimValue).HasMaxLength(200);
            });

            // 6. Table AspNetUserLogins
            modelBuilder.Entity<IdentityUserLogin<string>>(entity =>
            {
                entity.Property(e => e.LoginProvider).HasMaxLength(50);
                entity.Property(e => e.ProviderKey).HasMaxLength(50);
                entity.Property(e => e.ProviderDisplayName).HasMaxLength(100);
                entity.Property(e => e.UserId).HasMaxLength(50);
                entity.HasKey(e => new { e.LoginProvider, e.ProviderKey });
            });

            // 7. Table AspNetUserTokens
            modelBuilder.Entity<IdentityUserToken<string>>(entity =>
            {
                entity.Property(e => e.UserId).HasMaxLength(50);
                entity.Property(e => e.LoginProvider).HasMaxLength(50);
                entity.Property(e => e.Name).HasMaxLength(50);
                entity.Property(e => e.Value).HasMaxLength(500);
                entity.HasKey(e => new { e.UserId, e.LoginProvider, e.Name });
            });

            // 8. Configuration de TaskModel
            modelBuilder.Entity<TaskModel>(entity =>
            {
                entity.Property(e => e.Title).HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.AssignedToUserId).HasMaxLength(50);
                entity.Property(e => e.CreatedByUserId).HasMaxLength(50);

                // Relations avec User
                entity.HasOne(t => t.AssignedToUser)
                      .WithMany(u => u.AssignedTasks)
                      .HasForeignKey(t => t.AssignedToUserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(t => t.CreatedByUser)
                      .WithMany()
                      .HasForeignKey(t => t.CreatedByUserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // 9. NOUVEAU: Configuration de ChatMessage
            modelBuilder.Entity<ChatMessage>(entity =>
            {
                entity.Property(e => e.SenderId).HasMaxLength(50);
                entity.Property(e => e.ReceiverId).HasMaxLength(50);
                entity.Property(e => e.Message).HasMaxLength(1000);

                // Relations avec User pour expéditeur et destinataire
                entity.HasOne(c => c.Sender)
                      .WithMany()
                      .HasForeignKey(c => c.SenderId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(c => c.Receiver)
                      .WithMany()
                      .HasForeignKey(c => c.ReceiverId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Index pour optimiser les recherches
                entity.HasIndex(c => new { c.SenderId, c.ReceiverId });
                entity.HasIndex(c => c.SentAt);
                entity.HasIndex(c => c.IsRead);
            });
        }
    }
}