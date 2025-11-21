using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QLVT.Web.Identity;
using QLVT.Web.Data;
using QLVT.Web.Data.Models;

namespace QLVT.Web.Identity;

public static class SeedData
{
    private static readonly string[] Roles = new[] { "CongTy", "ChiNhanh", "User" };

    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // 1. Tạo roles nếu chưa có
        foreach (var role in Roles)
        {
            if (!await roleManager.Roles.AnyAsync(r => r.Name == role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // 2. Tạo user Công Ty
        var congTyEmail = "congty@qlvt.local";
        var congTyUser = await userManager.FindByEmailAsync(congTyEmail);
        if (congTyUser == null)
        {
            congTyUser = new ApplicationUser
            {
                UserName = congTyEmail,
                Email = congTyEmail,
                EmailConfirmed = true,
                BranchCode = null // Công ty không cố định chi nhánh
            };

            await userManager.CreateAsync(congTyUser, "CongTy@123");
            await userManager.AddToRoleAsync(congTyUser, "CongTy");
        }

        // 3. Tạo user Chi Nhánh (CN1)
        var chiNhanhEmail = "chinhanh1@qlvt.local";
        var chiNhanhUser = await userManager.FindByEmailAsync(chiNhanhEmail);
        if (chiNhanhUser == null)
        {
            chiNhanhUser = new ApplicationUser
            {
                UserName = chiNhanhEmail,
                Email = chiNhanhEmail,
                EmailConfirmed = true,
                BranchCode = "CN1"
                // Manv sẽ được cập nhật sau khi NhanVien được tạo
            };

            await userManager.CreateAsync(chiNhanhUser, "ChiNhanh@123");
            await userManager.AddToRoleAsync(chiNhanhUser, "ChiNhanh");
            // Cập nhật lại user để lấy Id vừa được tạo
            chiNhanhUser = await userManager.FindByEmailAsync(chiNhanhEmail);
        }

        // 4. Tạo user User (CN1)
        var userEmail = "user1@qlvt.local";
        var user = await userManager.FindByEmailAsync(userEmail);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = userEmail,
                Email = userEmail,
                EmailConfirmed = true,
                BranchCode = "CN1"
                // Manv sẽ được cập nhật sau khi NhanVien được tạo
            };

            await userManager.CreateAsync(user, "User@123");
            await userManager.AddToRoleAsync(user, "User");
            // Cập nhật lại user để lấy Id vừa được tạo
            user = await userManager.FindByEmailAsync(userEmail);
        }

        // 5. Seed data cho các chi nhánh
        if (chiNhanhUser != null && user != null)
        {
            await SeedBranchDataAsync(scope.ServiceProvider, chiNhanhUser, user);
        }
    }

    private static async Task SeedBranchDataAsync(IServiceProvider serviceProvider, ApplicationUser chiNhanhUser, ApplicationUser userUser)
    {
        var dbContextFactory = serviceProvider.GetRequiredService<Func<string, QlvtDbContext>>();
        var branchNames = new[] { "CN1", "CN2" };

        foreach (var branchName in branchNames)
        {
            using var db = dbContextFactory(branchName);

            // Tắt ràng buộc khóa ngoại để INSERT dễ dàng hơn
            await db.Database.ExecuteSqlRawAsync("EXEC sp_msforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL'");

            // Seed ChiNhanh (quan trọng, phải có trước Kho, NhanVien,...)
            if (!await db.ChiNhanhs.AnyAsync(cn => cn.Macn != null && cn.Macn.Trim() == branchName))
            {
                await db.ChiNhanhs.AddAsync(new ChiNhanh
                {
                    Macn = branchName,
                    ChiNhanh1 = $"Chi nhánh {branchName.Substring(2)}", // Ví dụ: "Chi nhánh 1"
                    Diachi = $"Địa chỉ cho chi nhánh {branchName.Substring(2)}", // Thêm địa chỉ
                    SoDt = $"090912345{branchName.Substring(2)}" // Thêm SĐT
                });
                await db.SaveChangesAsync();
            }

            // Seed Vattu (chung cho cả 2 chi nhánh)
            if (!await db.Vattus.AnyAsync())
            {
                var vattus = new[]
                {
                    new Vattu { Mavt = "M01", Tenvt = "Máy giặt tự động cửa trước", Dvt = "Cái", Soluongton = 0 },
                    new Vattu { Mavt = "MU01", Tenvt = "Máy uốn tóc", Dvt = "Cái", Soluongton = 10 },
                    new Vattu { Mavt = "MX02", Tenvt = "Máy sấy", Dvt = "Cái", Soluongton = 1 },
                    new Vattu { Mavt = "MX07", Tenvt = "Máy lạnh LG", Dvt = "Cái", Soluongton = 4 },
                    new Vattu { Mavt = "TV02", Tenvt = "Ti vi Sam Sung", Dvt = "Cái", Soluongton = 0 }
                };
                await db.Vattus.AddRangeAsync(vattus);
                await db.SaveChangesAsync();
            }

            // Seed Kho
            if (!await db.Khos.AnyAsync(k => k.Macn != null && k.Macn.Trim() == branchName))
            {
                if (branchName == "CN1")
                {
                    var khos = new[]
                    {
                        new Kho { Makho = "TD", Tenkho = "THỦ ĐỨC", Diachi = "34,Quang Trung THủ Đức TPHCM", Macn = branchName },
                        new Kho { Makho = "TK", Tenkho = "TỔNG KHO QUẬN 9", Diachi = "134 Đình Phong Phú ,Quận 9,TPHCM", Macn = branchName }
                    };
                    await db.Khos.AddRangeAsync(khos);
                    await db.SaveChangesAsync();
                }
                else if (branchName == "CN2")
                {
                    var khos = new[]
                    {
                        new Kho { Makho = "LP", Tenkho = "LONG PHU", Diachi = "127 Ngô Thì Nhậm, Thị xã Long Phú", Macn = branchName }
                    };
                    await db.Khos.AddRangeAsync(khos);
                    await db.SaveChangesAsync();
                }
            }

            // Seed NhanVien
            if (!await db.NhanViens.AnyAsync(n => n.Macn != null && n.Macn.Trim() == branchName))
            {
                if (branchName == "CN1")
                {
                    var nhanviens = new[]
                    {
                        new NhanVien { Manv = Guid.Parse(chiNhanhUser.Id), Ho = "Lương", Ten = "Trang", Diachi = "Thủ Đức", Ngaysinh = new DateTime(2000, 1, 1), Luong = 7000000, Macn = branchName, TrangThaiXoa = 0 },
                        new NhanVien { Manv = Guid.Parse(userUser.Id), Ho = "Trần", Ten = "Thanh", Diachi = "Quận 10", Ngaysinh = new DateTime(1995, 5, 5), Luong = 5000000, Macn = branchName, TrangThaiXoa = 0 },
                        new NhanVien { Manv = Guid.NewGuid(), Ho = "Hồ", Ten = "Thái", Diachi = "Bình Thạnh", Ngaysinh = new DateTime(2001, 3, 3), Luong = 6000000, Macn = branchName, TrangThaiXoa = 0 },
                        new NhanVien { Manv = Guid.NewGuid(), Ho = "Lê", Ten = "Trà", Diachi = "Phú Nhuận", Ngaysinh = new DateTime(1999, 9, 9), Luong = 7000000, Macn = branchName, TrangThaiXoa = 1 }
                    };
                    await db.NhanViens.AddRangeAsync(nhanviens);
                    await db.SaveChangesAsync();
                }
                else if (branchName == "CN2")
                {
                    var nvAn = new NhanVien { Manv = Guid.NewGuid(), Ho = "Hà", Ten = "An", Diachi = "Gò Vấp", Ngaysinh = new DateTime(1998, 8, 8), Luong = 5000000, Macn = branchName, TrangThaiXoa = 0 };

                    var nhanviens = new[]
                    {
                        new NhanVien { Manv = Guid.NewGuid(), Ho = "Nguyễn", Ten = "Hà", Diachi = "Quận 9", Ngaysinh = new DateTime(2002, 2, 2), Luong = 4000000, Macn = branchName, TrangThaiXoa = 1 },
                        new NhanVien { Manv = Guid.NewGuid(), Ho = "Thái", Ten = "Hà", Diachi = "Quận 6", Ngaysinh = new DateTime(2003, 4, 4), Luong = 7000000, Macn = branchName, TrangThaiXoa = 1 },
                        nvAn,
                        new NhanVien { Manv = Guid.NewGuid(), Ho = "Nguyễn", Ten = "Hợp", Diachi = "Thủ Đức", Ngaysinh = new DateTime(1997, 7, 7), Luong = 8000000, Macn = branchName, TrangThaiXoa = 0 }
                    };
                    await db.NhanViens.AddRangeAsync(nhanviens);
                    await db.SaveChangesAsync();

                    // Gán đơn hàng cho nhân viên "Hà An" vừa tạo ở CN2
                    var datHangs = new[]
                    {
                        new DatHang { MasoDdh = "MDDH03", Ngay = DateOnly.FromDateTime(new DateTime(2019, 10, 20)), NhaCc = "CTY Samsung", Manv = nvAn.Manv, Makho = "LP" }
                    };
                    await db.DatHangs.AddRangeAsync(datHangs);
                    await db.SaveChangesAsync();

                    var ctdh = new[]
                    {
                        new Ctddh { MasoDdh = "MDDH03", Mavt = "MX02", Soluong = 20, Dongia = 700000 }
                    };
                    await db.Ctddhs.AddRangeAsync(ctdh);
                    await db.SaveChangesAsync();
                }
            }

            // Seed DatHang, CTDDH, PhieuNhap, CTPN
            if (!await db.DatHangs.AnyAsync())
            {
                if (branchName == "CN1")
                {
                    var datHangs = new[]
                    {
                        new DatHang { MasoDdh = "MDDH01", Ngay = DateOnly.FromDateTime(new DateTime(2019, 10, 20)), NhaCc = "CTY Điện máy xanh", Manv = Guid.Parse(chiNhanhUser.Id), Makho = "TD" },
                        new DatHang { MasoDdh = "MDDH02", Ngay = DateOnly.FromDateTime(new DateTime(2019, 10, 20)), NhaCc = "CTY Panasonic", Manv = Guid.Parse(chiNhanhUser.Id), Makho = "TD" }
                    };
                    await db.DatHangs.AddRangeAsync(datHangs);
                    await db.SaveChangesAsync();

                    var ctdh = new[]
                    {
                        new Ctddh { MasoDdh = "MDDH01", Mavt = "M01", Soluong = 10, Dongia = 400000 },
                        new Ctddh { MasoDdh = "MDDH01", Mavt = "MX02", Soluong = 10, Dongia = 700000 },
                        new Ctddh { MasoDdh = "MDDH02", Mavt = "MU01", Soluong = 6, Dongia = 500000 }
                    };
                    await db.Ctddhs.AddRangeAsync(ctdh);
                    await db.SaveChangesAsync();

                    var phieuNhaps = new[]
                    {
                        new PhieuNhap { Mapn = "PN01", Ngay = DateOnly.FromDateTime(new DateTime(2019, 10, 20)), MasoDdh = "MDDH01", Manv = Guid.Parse(chiNhanhUser.Id), Makho = "TD" }
                    };
                    await db.PhieuNhaps.AddRangeAsync(phieuNhaps);
                    await db.SaveChangesAsync();

                    var ctpn = new[]
                    {
                        new Ctpn { Mapn = "PN01", Mavt = "M01", Soluong = 10, Dongia = 400000 },
                        new Ctpn { Mapn = "PN01", Mavt = "MX02", Soluong = 8, Dongia = 700000 }
                    };
                    await db.Ctpns.AddRangeAsync(ctpn);
                    await db.SaveChangesAsync();
                }
            }

            // Bật lại ràng buộc khóa ngoại
            await db.Database.ExecuteSqlRawAsync("EXEC sp_msforeachtable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL'");
        }

        // Cập nhật Manv cho ApplicationUser sau khi NhanVien đã được tạo
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        chiNhanhUser.Manv = Guid.Parse(chiNhanhUser.Id);
        await userManager.UpdateAsync(chiNhanhUser);

        userUser.Manv = Guid.Parse(userUser.Id);
        await userManager.UpdateAsync(userUser);
    }
}
