using Microsoft.EntityFrameworkCore;
using Purrfolio.Core.Models;

namespace Purrfolio.Infrastructure.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<InvestmentRecord> InvestmentRecords => Set<InvestmentRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InvestmentRecord>(entity =>
        {
            entity.ToTable("InvestmentRecords");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(128).IsRequired();
            entity.Property(x => x.AssetClass).HasConversion<string>().IsRequired();
            entity.Property(x => x.CouponFrequency).HasConversion<string>();
            entity.Property(x => x.Quantity).HasPrecision(18, 4);
            entity.Property(x => x.UnitPrice).HasPrecision(18, 4);
            entity.Property(x => x.Fees).HasPrecision(18, 4);
            entity.Property(x => x.CouponRate).HasPrecision(9, 6);
            entity.Property(x => x.AccruedInterest).HasPrecision(18, 4);
        });
    }
}
