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

    public virtual DbSet<Agentinfo> Agentinfos { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Agentinfo>(entity =>
        {
            entity.HasKey(e => new { e.AgentId, e.SellerId }).HasName("PK_agentInfo");

            entity.ToTable("agentinfo");

            entity.Property(e => e.AgentId).HasColumnName("AgentID");
            entity.Property(e => e.SellerId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("SellerID");
            entity.Property(e => e.AccountStatus)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Account_status");
            entity.Property(e => e.AgentName).HasMaxLength(100);
            entity.Property(e => e.ColId).ValueGeneratedOnAdd();
            entity.Property(e => e.Counter).HasDefaultValue(0);
            entity.Property(e => e.Email)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.ExpiryDate).HasColumnType("datetime");
            entity.Property(e => e.LastLoginDate).HasColumnType("datetime");
            entity.Property(e => e.LevelId).HasColumnName("LevelID");
            entity.Property(e => e.Password)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.PhotoType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Photo_Type");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
