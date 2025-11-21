using Microsoft.EntityFrameworkCore;
using QLVT.Web.Data.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace QLVT.Web.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(
            Func<string, QlvtDbContext> dbContextFactory,
            QlvtLookupDbContext lookupDbContext)
        {
            // 1. Áp dụng migrations cho Lookup DB trước
            await lookupDbContext.Database.MigrateAsync();

            // 2. Lấy danh sách tất cả các chi nhánh để áp dụng migrations cho từng chi nhánh
            var allBranches = new[] { "CN1", "CN2" }; // Hoặc lấy từ lookupDbContext.ChiNhanhs nếu cần thiết

            // 3. Lặp qua từng chi nhánh để migrate
            foreach (var branch in allBranches)
            {
                var branchCode = branch.Trim();
                var dbContext = dbContextFactory(branchCode);

                // Áp dụng migrations cho DB của chi nhánh
                await dbContext.Database.MigrateAsync();
            }
        }
    }
}