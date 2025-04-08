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

    public virtual DbSet<config> configs { get; set; }

    public virtual DbSet<field> fields { get; set; }

    public virtual DbSet<field_option> field_options { get; set; }

    public virtual DbSet<floor_plan> floor_plans { get; set; }

    public virtual DbSet<password_log> password_logs { get; set; }

    public virtual DbSet<user_role> user_roles { get; set; }


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

        modelBuilder.Entity<config>(entity =>
        {
            entity.HasKey(e => e.P_Id);

            entity.ToTable("config");

            entity.Property(e => e.P_Name)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.P_Value).HasMaxLength(100);
        });

        modelBuilder.Entity<field>(entity =>
        {
            entity.HasKey(e => new { e.Field_Category, e.Field_Name });

            entity.ToTable("field");

            entity.Property(e => e.Field_Category).HasMaxLength(50);
            entity.Property(e => e.Field_Name).HasMaxLength(100);
            entity.Property(e => e.Field_Display).HasMaxLength(200);
            entity.Property(e => e.Field_Tag).HasMaxLength(50);
            entity.Property(e => e.Field_Type).HasMaxLength(50);
        });

        modelBuilder.Entity<field_option>(entity =>
        {
            entity.HasKey(e => new { e.Field_Name, e.Field_Option }).HasName("PK_Field_Option");

            entity.ToTable("field_option");

            entity.Property(e => e.Field_Name)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Field_Option).HasMaxLength(50);
        });

        modelBuilder.Entity<floor_plan>(entity =>
        {
            entity.HasKey(e => e.F_Id);

            entity.ToTable("floor_plan");

            entity.Property(e => e.Created_Time).HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Status).HasMaxLength(20);
            entity.Property(e => e.Style).HasMaxLength(500);
            entity.Property(e => e.Updated_Time).HasColumnType("datetime");
        });

        modelBuilder.Entity<password_log>(entity =>
        {
            entity.HasKey(e => e.P_Id);

            entity.ToTable("password_log");

            entity.HasIndex(e => e.AgentID, "IX_AgentID");

            entity.Property(e => e.Created_Time).HasColumnType("datetime");
            entity.Property(e => e.Password)
                .HasMaxLength(500)
                .IsUnicode(false);
        });

        modelBuilder.Entity<user_role>(entity =>
        {
            entity.HasKey(e => e.RoleID);

            entity.ToTable("user_role");

            entity.Property(e => e.Categories).HasMaxLength(1000);
            entity.Property(e => e.Companies).HasMaxLength(1000);
            entity.Property(e => e.Functions).HasMaxLength(1000);
            entity.Property(e => e.RoleName).HasMaxLength(50);
            entity.Property(e => e.RoleStatus).HasMaxLength(1);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
