using ArcanumBudget.Api.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ArcanumBudget.Api.Data;

// Inherits IdentityDbContext so ASP.NET Identity's tables (users, roles, etc.)
// come along for free and live in the same database as everything else.
public class AppDbContext : IdentityDbContext<AppUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Household> Households => Set<Household>();
    public DbSet<HouseholdMember> HouseholdMembers => Set<HouseholdMember>();
    public DbSet<PlaidItem> PlaidItems => Set<PlaidItem>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Recommendation> Recommendations => Set<Recommendation>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<HouseholdMember>(e =>
        {
            e.HasOne(m => m.Household)
             .WithMany(h => h.Members)
             .HasForeignKey(m => m.HouseholdId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(m => m.User)
             .WithMany(u => u.HouseholdMemberships)
             .HasForeignKey(m => m.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            // No cascade here: UserId already cascades on the same table (AppUser),
            // and SQL Server disallows multiple cascade paths to one table.
            e.HasOne(m => m.InvitedByUser)
             .WithMany()
             .HasForeignKey(m => m.InvitedByUserId)
             .OnDelete(DeleteBehavior.Restrict);

            // A user can only have one membership row per household.
            e.HasIndex(m => new { m.HouseholdId, m.UserId }).IsUnique();
        });

        builder.Entity<PlaidItem>(e =>
        {
            e.HasOne(p => p.User)
             .WithMany(u => u.PlaidItems)
             .HasForeignKey(p => p.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(p => p.PlaidItemId).IsUnique();
        });

        builder.Entity<Account>(e =>
        {
            e.HasOne(a => a.PlaidItem)
             .WithMany(p => p.Accounts)
             .HasForeignKey(a => a.PlaidItemId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(a => a.PlaidAccountId).IsUnique();
        });

        builder.Entity<Transaction>(e =>
        {
            e.HasOne(t => t.Account)
             .WithMany(a => a.Transactions)
             .HasForeignKey(t => t.AccountId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(t => t.PlaidTransactionId).IsUnique();
            e.Property(t => t.Amount).HasColumnType("decimal(18,2)");
        });

        builder.Entity<Recommendation>(e =>
        {
            e.Property(r => r.EstimatedMonthlySavings).HasColumnType("decimal(18,2)");
        });
    }
}
