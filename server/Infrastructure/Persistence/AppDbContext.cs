using Microsoft.EntityFrameworkCore;
using Domain.Entities;

namespace Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<User>(e => {
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.Email).IsUnique();
            e.HasMany(u => u.Conversations).WithOne(c => c.User).HasForeignKey(c => c.UserId);
        });

        builder.Entity<Conversation>(e => {
            e.HasKey(c => c.Id);
            e.HasMany(c => c.Messages).WithOne(m => m.Conversation).HasForeignKey(m => m.ConversationId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Message>(e => {
            e.HasKey(m => m.Id);
            e.Property(m => m.Role).HasConversion<string>();
        });

        builder.Entity<Project>(e => {
            e.HasKey(p => p.Id);
            e.Property(p => p.Status).HasConversion<string>();
        });
    }
}
