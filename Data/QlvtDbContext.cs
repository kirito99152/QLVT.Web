﻿using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using QLVT.Web.Data.Models;

namespace QLVT.Web.Data;

public partial class QlvtDbContext : DbContext
{
    public QlvtDbContext(DbContextOptions<QlvtDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ChiNhanh> ChiNhanhs { get; set; }

    public virtual DbSet<Ctddh> Ctddhs { get; set; }

    public virtual DbSet<Ctpn> Ctpns { get; set; }

    public virtual DbSet<Ctpx> Ctpxes { get; set; }

    public virtual DbSet<DatHang> DatHangs { get; set; }

    public virtual DbSet<Kho> Khos { get; set; }

    public virtual DbSet<NhanVien> NhanViens { get; set; }

    public virtual DbSet<PhieuNhap> PhieuNhaps { get; set; }

    public virtual DbSet<PhieuXuat> PhieuXuats { get; set; }

    public virtual DbSet<Vattu> Vattus { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChiNhanh>(entity =>
        {
            entity.HasKey(e => e.Macn);

            entity.ToTable("ChiNhanh");

            entity.HasIndex(e => e.ChiNhanh1, "UK_ChiNhanh").IsUnique();

            entity.Property(e => e.Macn)
                .HasMaxLength(10)
                .IsFixedLength()
                .HasColumnName("MACN");
            entity.Property(e => e.ChiNhanh1)
                .HasMaxLength(100)
                .HasColumnName("ChiNhanh");
            entity.Property(e => e.Diachi)
                .HasMaxLength(100)
                .HasColumnName("DIACHI");
            entity.Property(e => e.SoDt)
                .HasMaxLength(15)
                .HasColumnName("SoDT");
        });

        modelBuilder.Entity<Ctddh>(entity =>
        {
            entity.HasKey(e => new { e.MasoDdh, e.Mavt });

            entity.ToTable("CTDDH");

            entity.Property(e => e.MasoDdh)
                .HasMaxLength(8)
                .IsFixedLength()
                .HasColumnName("MasoDDH");
            entity.Property(e => e.Mavt)
                .HasMaxLength(4)
                .IsFixedLength()
                .HasColumnName("MAVT");
            entity.Property(e => e.Dongia).HasColumnName("DONGIA");
            entity.Property(e => e.Soluong).HasColumnName("SOLUONG");

            entity.HasOne(d => d.MasoDdhNavigation).WithMany(p => p.Ctddhs)
                .HasForeignKey(d => d.MasoDdh)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CTDDH_DatHang");

            entity.HasOne(d => d.MavtNavigation).WithMany(p => p.Ctddhs)
                .HasForeignKey(d => d.Mavt)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CTDDH_VatTu");
        });

        modelBuilder.Entity<Ctpn>(entity =>
        {
            entity.HasKey(e => new { e.Mapn, e.Mavt });

            entity.ToTable("CTPN");

            entity.Property(e => e.Mapn)
                .HasMaxLength(8)
                .IsFixedLength()
                .HasColumnName("MAPN");
            entity.Property(e => e.Mavt)
                .HasMaxLength(4)
                .IsFixedLength()
                .HasColumnName("MAVT");
            entity.Property(e => e.Dongia).HasColumnName("DONGIA");
            entity.Property(e => e.Soluong).HasColumnName("SOLUONG");

            entity.HasOne(d => d.MapnNavigation).WithMany(p => p.Ctpns)
                .HasForeignKey(d => d.Mapn)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CTPN_PhieuNhap");

            entity.HasOne(d => d.MavtNavigation).WithMany(p => p.Ctpns)
                .HasForeignKey(d => d.Mavt)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CTPN_VatTu");
        });

        modelBuilder.Entity<Ctpx>(entity =>
        {
            entity.HasKey(e => new { e.Mapx, e.Mavt });

            entity.ToTable("CTPX");

            entity.Property(e => e.Mapx)
                .HasMaxLength(8)
                .IsFixedLength()
                .HasColumnName("MAPX");
            entity.Property(e => e.Mavt)
                .HasMaxLength(4)
                .IsFixedLength()
                .HasColumnName("MAVT");
            entity.Property(e => e.Dongia).HasColumnName("DONGIA");
            entity.Property(e => e.Soluong).HasColumnName("SOLUONG");

            entity.HasOne(d => d.MapxNavigation).WithMany(p => p.Ctpxes)
                .HasForeignKey(d => d.Mapx)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CTPX_PX");

            entity.HasOne(d => d.MavtNavigation).WithMany(p => p.Ctpxes)
                .HasForeignKey(d => d.Mavt)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CTPX_VatTu");
        });

        modelBuilder.Entity<DatHang>(entity =>
        {
            entity.HasKey(e => e.MasoDdh);

            entity.ToTable("DatHang");

            entity.Property(e => e.MasoDdh)
                .HasMaxLength(8)
                .IsFixedLength()
                .HasColumnName("MasoDDH");
            entity.Property(e => e.Makho)
                .HasMaxLength(4)
                .IsFixedLength()
                .HasColumnName("MAKHO");
            entity.Property(e => e.Manv).HasColumnName("MANV"); // Kiểu Guid sẽ được cập nhật từ model
            entity.Property(e => e.Ngay)
                .HasColumnName("NGAY");
            entity.Property(e => e.NhaCc)
                .HasMaxLength(100)
                .HasColumnName("NhaCC");

            entity.HasOne(d => d.MakhoNavigation).WithMany(p => p.DatHangs)
                .HasForeignKey(d => d.Makho)
                .HasConstraintName("FK_DatHang_Kho");

            entity.HasOne(d => d.ManvNavigation).WithMany(p => p.DatHangs)
                .HasForeignKey(d => d.Manv)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DatHang_NhanVien");
        });

        modelBuilder.Entity<Kho>(entity =>
        {
            entity.HasKey(e => e.Makho);

            entity.ToTable("Kho");

            entity.HasIndex(e => e.Tenkho, "UK_TENKHO").IsUnique();

            entity.Property(e => e.Makho)
                .HasMaxLength(4)
                .IsFixedLength()
                .HasColumnName("MAKHO");
            entity.Property(e => e.Diachi)
                .HasMaxLength(100)
                .HasColumnName("DIACHI");
            entity.Property(e => e.Macn)
                .HasMaxLength(10)
                .IsFixedLength()
                .HasColumnName("MACN");
            entity.Property(e => e.Tenkho)
                .HasMaxLength(30)
                .HasColumnName("TENKHO");

            entity.HasOne(d => d.MacnNavigation).WithMany(p => p.Khos)
                .HasForeignKey(d => d.Macn)
                .HasConstraintName("FK_Kho_Kho");
        });

        modelBuilder.Entity<NhanVien>(entity =>
        {
            entity.HasKey(e => e.Manv);

            entity.ToTable("NhanVien");

            entity.Property(e => e.Manv)
                .ValueGeneratedNever()
                .HasColumnName("MANV");
            entity.Property(e => e.Diachi) // Kiểu Manv sẽ là Guid
                .HasMaxLength(100)
                .HasColumnName("DIACHI");
            entity.Property(e => e.Ho)
                .HasMaxLength(40)
                .HasColumnName("HO");
            entity.Property(e => e.Luong).HasColumnName("LUONG");
            entity.Property(e => e.Macn)
                .HasMaxLength(10)
                .IsFixedLength()
                .HasColumnName("MACN");
            entity.Property(e => e.Ngaysinh)
                .HasColumnType("datetime")
                .HasColumnName("NGAYSINH");
            entity.Property(e => e.Ten)
                .HasMaxLength(10)
                .HasColumnName("TEN");
            entity.Property(e => e.TrangThaiXoa);

            entity.HasOne(d => d.MacnNavigation).WithMany(p => p.NhanViens)
                .HasForeignKey(d => d.Macn)
                .HasConstraintName("FK_NhanVien_ChiNhanh");
        });

        modelBuilder.Entity<PhieuNhap>(entity =>
        {
            entity.HasKey(e => e.Mapn);

            entity.ToTable("PhieuNhap");

            entity.HasIndex(e => e.MasoDdh, "UK_MaSoDDH").IsUnique();

            entity.Property(e => e.Mapn)
                .HasMaxLength(8)
                .IsFixedLength()
                .HasColumnName("MAPN");
            entity.Property(e => e.Makho)
                .HasMaxLength(4)
                .IsFixedLength()
                .HasColumnName("MAKHO");
            entity.Property(e => e.Manv).HasColumnName("MANV"); // Kiểu Guid sẽ được cập nhật từ model
            entity.Property(e => e.MasoDdh)
                .HasMaxLength(8)
                .IsFixedLength()
                .HasColumnName("MasoDDH");
            entity.Property(e => e.Ngay)
                .HasColumnName("NGAY");

            entity.HasOne(d => d.MakhoNavigation).WithMany(p => p.PhieuNhaps)
                .HasForeignKey(d => d.Makho)
                .HasConstraintName("FK_PhieuNhap_Kho");

            entity.HasOne(d => d.ManvNavigation).WithMany(p => p.PhieuNhaps)
                .HasForeignKey(d => d.Manv)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PhieuNhap_NhanVien");

            entity.HasOne(d => d.MasoDdhNavigation).WithOne(p => p.PhieuNhap)
                .HasForeignKey<PhieuNhap>(d => d.MasoDdh)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PhieuNhap_DatHang");
        });

        modelBuilder.Entity<PhieuXuat>(entity =>
        {
            entity.HasKey(e => e.Mapx).HasName("PK_PX");

            entity.ToTable("PhieuXuat");

            entity.Property(e => e.Mapx)
                .HasMaxLength(8)
                .IsFixedLength()
                .HasColumnName("MAPX");
            entity.Property(e => e.Hotenkh)
                .HasMaxLength(100)
                .HasColumnName("HOTENKH");
            entity.Property(e => e.Makho)
                .HasMaxLength(4)
                .IsFixedLength()
                .HasColumnName("MAKHO");
            entity.Property(e => e.Manv).HasColumnName("MANV"); // Kiểu Guid sẽ được cập nhật từ model
            entity.Property(e => e.Ngay)
                .HasColumnName("NGAY");

            entity.HasOne(d => d.MakhoNavigation).WithMany(p => p.PhieuXuats)
                .HasForeignKey(d => d.Makho)
                .HasConstraintName("FK_PhieuXuat_Kho");

            entity.HasOne(d => d.ManvNavigation).WithMany(p => p.PhieuXuats)
                .HasForeignKey(d => d.Manv)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PX_NhanVien");
        });

        modelBuilder.Entity<Vattu>(entity =>
        {
            entity.HasKey(e => e.Mavt).HasName("PK_VatTu");

            entity.ToTable("Vattu");

            entity.HasIndex(e => e.Tenvt, "UK_TENVT").IsUnique();

            entity.Property(e => e.Mavt)
                .HasMaxLength(4)
                .IsFixedLength()
                .HasColumnName("MAVT");
            entity.Property(e => e.Dvt)
                .HasMaxLength(15)
                .HasColumnName("DVT");
            entity.Property(e => e.Soluongton).HasColumnName("SOLUONGTON");
            entity.Property(e => e.Tenvt)
                .HasMaxLength(30)
                .HasColumnName("TENVT");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
