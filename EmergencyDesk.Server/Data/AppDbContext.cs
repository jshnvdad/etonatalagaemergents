using Microsoft.EntityFrameworkCore;

namespace EmergencyDesk.Server.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Staff> Staff => Set<Staff>();
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<Responder> Responders => Set<Responder>();
    public DbSet<Incident> Incidents => Set<Incident>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Staff>().HasIndex(s => s.Username).IsUnique();
        b.Entity<AppUser>().HasIndex(u => u.Email).IsUnique();
        b.Entity<Incident>().HasIndex(i => i.ClientId).IsUnique();

        // Store enums as readable text in MySQL ("Medical", "EnRoute", ...)
        b.Entity<Responder>().Property(r => r.Specialty).HasConversion<string>().HasMaxLength(20);
        b.Entity<Responder>().Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
        b.Entity<Incident>().Property(i => i.Type).HasConversion<string>().HasMaxLength(20);
        b.Entity<Incident>().Property(i => i.Status).HasConversion<string>().HasMaxLength(20);

        b.Entity<Incident>().HasOne(i => i.User).WithMany().HasForeignKey(i => i.UserId);
        b.Entity<Incident>().HasOne(i => i.AssignedResponder).WithMany()
            .HasForeignKey(i => i.AssignedResponderId).OnDelete(DeleteBehavior.SetNull);
    }
}
