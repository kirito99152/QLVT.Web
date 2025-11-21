using Microsoft.EntityFrameworkCore;
using QLVT.Web.Data.Lookup;
using QLVT.Web.Data.Models;

namespace QLVT.Web.Data;

public class QlvtLookupDbContext : DbContext
{
    public QlvtLookupDbContext(DbContextOptions<QlvtLookupDbContext> options)
        : base(options)
    {
    }

    public DbSet<NhanVienLookup> NhanViens { get; set; } = null!;
    public DbSet<KhoLookup> Khos { get; set; } = null!;
    public DbSet<ChiNhanhLookup> ChiNhanhs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // map tên bảng (nếu trùng tên thì có thể bỏ)
        modelBuilder.Entity<NhanVienLookup>(entity =>
        {
            entity.ToTable("NhanVien");
            entity.HasKey(e => e.Manv);
            // Guid sẽ được sinh ra từ ứng dụng, không phải từ DB
            entity.Property(e => e.Manv).ValueGeneratedNever();
        });

        modelBuilder.Entity<KhoLookup>(entity =>
        {
            entity.ToTable("Kho");
            entity.HasKey(e => e.Makho);
            // Đảm bảo khóa chính không phải là identity
            entity.Property(e => e.Makho).ValueGeneratedNever();
        });
        modelBuilder.Entity<ChiNhanhLookup>(entity =>
        {
            entity.ToTable("ChiNhanh");
            entity.HasKey(e => e.Macn);
            // Đảm bảo khóa chính không phải là identity
            entity.Property(e => e.Macn).ValueGeneratedNever();
        });
    }
}
