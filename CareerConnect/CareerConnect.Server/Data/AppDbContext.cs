using CareerConnect.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace CareerConnect.Server.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Rol> Roles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configurare index pentru email unic
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Configurare relație User - Rol
            modelBuilder.Entity<User>()
                .HasOne(u => u.Rol)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RolId)
                .OnDelete(DeleteBehavior.Restrict);

            // Seed roles
            modelBuilder.Entity<Rol>().HasData(
                new Rol { Id = 1, Nume = "admin" },
                new Rol { Id = 2, Nume = "angajat" },
                new Rol { Id = 3, Nume = "angajator" }
            );
        }
    }
}