using FinanceBot.Models;
using Microsoft.EntityFrameworkCore;

namespace FinanceBot.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Chat> Chats { get; set; }
    public DbSet<Spending> Spendings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Chat>(entity =>
        {
            entity.ToTable("Chat");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TelegramChatId).IsUnique();
            entity.HasIndex(e => e.ReportToken).IsUnique();
            entity.Property(e => e.ReportToken).HasMaxLength(64).IsRequired();
            entity.Property(e => e.StartedAt)
                .HasConversion(
                    v => v.ToUniversalTime(),
                    v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        });

        modelBuilder.Entity<Spending>(entity =>
        {
            entity.ToTable("Spending");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ChatId);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.Category).HasMaxLength(64).IsRequired();
            entity.Property(e => e.Note).HasMaxLength(500);
            entity.Property(e => e.SpentAt)
                .HasConversion(
                    v => v.ToUniversalTime(),
                    v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
            entity.HasIndex(e => new { e.SpentAt, e.ChatId });
            entity.HasOne(e => e.Chat) // ReferenceNavigationBuilder<Spending, Chat>
                .WithMany(e => e.Spendings)
                .HasForeignKey(e => e.ChatId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}