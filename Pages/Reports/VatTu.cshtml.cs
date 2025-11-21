using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using QLVT.Web.Data;
using QLVT.Web.Data.Models;
using System.Data;
using QLVT.Web.Infrastructure.Branches;

namespace QLVT.Web.Pages.Reports
{
    [Authorize]
    public class VatTuModel : PageModel
    {
        private readonly IBranchDbContextProvider _branchDb;
        private readonly Func<string, QlvtDbContext> _dbContextFactory;
        private readonly QlvtLookupDbContext _lookupDbContext;

        public VatTuModel(IBranchDbContextProvider branchDb, Func<string, QlvtDbContext> dbContextFactory, QlvtLookupDbContext lookupDbContext)
        {
            _branchDb = branchDb;
            _dbContextFactory = dbContextFactory;
            _lookupDbContext = lookupDbContext;
        }

        public List<Vattu> VatTus { get; set; } = new();
        public SelectList? ChiNhanhSelectList { get; set; }
        public string? TenChiNhanh { get; set; }

        public async Task<IActionResult> OnGetAsync(string? branch)
        {
            string? chiNhanhValue = branch?.Trim();
            QlvtDbContext db;

            if (User.IsInRole("ChiNhanh") || User.IsInRole("User"))
            {
                chiNhanhValue = _branchDb.DbContext.Database.GetDbConnection().DataSource.Contains("CN1") ? "CN1" : "CN2";
            }

            if (User.IsInRole("CongTy"))
            {
                // Lấy danh sách chi nhánh từ DB tra cứu
                var chiNhanhs = await _lookupDbContext.ChiNhanhs
                    .Select(cn => new { Macn = cn.Macn.Trim(), cn.ChiNhanh1 })
                    .ToListAsync();
                ChiNhanhSelectList = new SelectList(chiNhanhs, "Macn", "ChiNhanh1", chiNhanhValue);
            }

            if (string.IsNullOrEmpty(chiNhanhValue))
            {
                if (User.IsInRole("CongTy"))
                {
                    TenChiNhanh = "Tất cả chi nhánh";
                    var allVatTus = new List<Vattu>();
                    var chiNhanhs = await _lookupDbContext.ChiNhanhs.AsNoTracking().ToListAsync();
                    foreach (var cn in chiNhanhs)
                    {
                        var dbBranch = _dbContextFactory(cn.Macn.Trim());
                        allVatTus.AddRange(await dbBranch.Vattus.AsNoTracking().ToListAsync());
                    }

                    // Gộp các vật tư từ các chi nhánh và tính tổng số lượng tồn
                    VatTus = allVatTus
                        .GroupBy(vt => vt.Mavt)
                        .Select(g => new Vattu
                        {
                            Mavt = g.Key,
                            Tenvt = g.First().Tenvt, // Giả định Tenvt và Dvt là giống nhau cho cùng Mavt
                            Dvt = g.First().Dvt,
                            Soluongton = g.Sum(vt => vt.Soluongton)
                        }).OrderBy(vt => vt.Tenvt).ToList();
                    return Page();
                }
                else
                {
                    VatTus = new List<Vattu>();
                    return Page();
                }
            }

            db = _dbContextFactory(chiNhanhValue);
            var chiNhanh = await db.ChiNhanhs.AsNoTracking().FirstOrDefaultAsync(s => s.Macn.Trim() == chiNhanhValue);
            TenChiNhanh = chiNhanh?.ChiNhanh1 ?? string.Empty;

            VatTus = await db.Vattus.OrderBy(vt => vt.Tenvt).ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnGetExportAsync(string? branch)
        {
            string? chiNhanhValue = branch?.Trim();
            QlvtDbContext db;
            List<Vattu> data;

            if (User.IsInRole("ChiNhanh") || User.IsInRole("User"))
            {
                chiNhanhValue = _branchDb.DbContext.Database.GetDbConnection().DataSource.Contains("CN1") ? "CN1" : "CN2";
            }

            if (string.IsNullOrEmpty(chiNhanhValue))
            {
                if (User.IsInRole("CongTy"))
                {
                    var allVatTus = new List<Vattu>();
                    var chiNhanhs = await _lookupDbContext.ChiNhanhs.AsNoTracking().ToListAsync();
                    foreach (var cn in chiNhanhs)
                    {
                        var dbBranch = _dbContextFactory(cn.Macn.Trim());
                        allVatTus.AddRange(await dbBranch.Vattus.AsNoTracking().ToListAsync());
                    }

                    // Gộp các vật tư từ các chi nhánh và tính tổng số lượng tồn
                    data = allVatTus
                        .GroupBy(vt => vt.Mavt)
                        .Select(g => new Vattu
                        {
                            Mavt = g.Key,
                            Tenvt = g.First().Tenvt,
                            Dvt = g.First().Dvt,
                            Soluongton = g.Sum(vt => vt.Soluongton)
                        }).OrderBy(vt => vt.Tenvt).ToList();
                }
                else
                {
                    return new EmptyResult();
                }
            }
            else
            {
                db = _dbContextFactory(chiNhanhValue);
                data = await db.Vattus.OrderBy(vt => vt.Tenvt).ToListAsync();
            }

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("DS_VatTu");
                worksheet.Cell(1, 1).Value = "Mã VT";
                worksheet.Cell(1, 2).Value = "Tên Vật Tư";
                worksheet.Cell(1, 3).Value = "Đơn Vị Tính";
                worksheet.Cell(1, 4).Value = "Số Lượng Tồn";

                int currentRow = 2;
                foreach (var vt in data)
                {
                    worksheet.Cell(currentRow, 1).Value = vt.Mavt;
                    worksheet.Cell(currentRow, 2).Value = vt.Tenvt;
                    worksheet.Cell(currentRow, 3).Value = vt.Dvt;
                    worksheet.Cell(currentRow, 4).Value = vt.Soluongton;
                    currentRow++;
                }

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "DanhMucVatTu.xlsx");
                }
            }
        }
    }
}
