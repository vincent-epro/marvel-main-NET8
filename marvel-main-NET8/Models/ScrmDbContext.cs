using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace marvel_main_NET8.Models;

public partial class ScrmDbContext : DbContext
{
    public ScrmDbContext()
    {
    }

    public ScrmDbContext(DbContextOptions<ScrmDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<agentinfo> agentinfos { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<agentinfo>(entity =>
        {
            entity.HasKey(e => new { e.AgentID, e.SellerID }).HasName("PK_agentInfo");

            entity.ToTable("agentinfo");

            entity.Property(e => e.SellerID)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Account_status)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.AgentName).HasMaxLength(100);
            entity.Property(e => e.ColId).ValueGeneratedOnAdd();
            entity.Property(e => e.Counter).HasDefaultValue(0);
            entity.Property(e => e.Email)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.ExpiryDate).HasColumnType("datetime");
            entity.Property(e => e.LastLoginDate).HasColumnType("datetime");
            entity.Property(e => e.Password)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.Photo_Type)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
