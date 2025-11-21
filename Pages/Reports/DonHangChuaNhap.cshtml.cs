using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QLVT.Web.Data;
using QLVT.Web.Infrastructure.Branches;
using System.Data;

namespace QLVT.Web.Pages.Reports
{
    [Authorize]
    public class DonHangChuaNhapModel : PageModel
    {
        private readonly IBranchDbContextProvider _branchDb;
        private readonly Func<string, QlvtDbContext> _dbContextFactory;
        private readonly QlvtLookupDbContext _lookupDbContext;

        public DonHangChuaNhapModel(IBranchDbContextProvider branchDb, Func<string, QlvtDbContext> dbContextFactory, QlvtLookupDbContext lookupDbContext)
        {
            _branchDb = branchDb;
            _dbContextFactory = dbContextFactory;
            _lookupDbContext = lookupDbContext;
        }

        // Lớp nội bộ để hứng kết quả từ SP
        public class DonHangReportItem
        {
            public string MasoDdh { get; set; } = string.Empty;
            // Sửa từ DateTime thành DateOnly để khớp với model DatHang
            public DateOnly Ngay { get; set; }
            public string NhaCc { get; set; } = string.Empty;
            public string HoTenNv { get; set; } = string.Empty;
            public string Tenvt { get; set; } = string.Empty;
            public int Soluong { get; set; }
            public decimal Dongia { get; set; }
        }

        public List<DonHangReportItem> ReportData { get; set; } = new();
        public SelectList? ChiNhanhSelectList { get; set; }
        public string? TenChiNhanh { get; set; }

        private async Task<List<DonHangReportItem>> GetDataAsync(QlvtDbContext db)
        {
            // Thay thế SP bằng truy vấn LINQ
            var query = from dh in db.DatHangs
                        where !db.PhieuNhaps.Any(pn => pn.MasoDdh == dh.MasoDdh)
                        join ctdh in db.Ctddhs on dh.MasoDdh equals ctdh.MasoDdh
                        join nv in db.NhanViens on dh.Manv equals nv.Manv
                        join vt in db.Vattus on ctdh.Mavt equals vt.Mavt
                        select new DonHangReportItem
                        {
                            MasoDdh = dh.MasoDdh,
                            Ngay = dh.Ngay,
                            NhaCc = dh.NhaCc,
                            HoTenNv = nv.Ho + " " + nv.Ten,
                            Tenvt = vt.Tenvt,
                            Soluong = ctdh.Soluong ?? 0,
                            Dongia = (decimal)(ctdh.Dongia ?? 0.0)
                        };

            return await query.ToListAsync();
        }

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
                    // Khi không chọn chi nhánh, user Công ty sẽ xem tất cả
                    TenChiNhanh = "Tất cả chi nhánh";
                    var allData = new List<DonHangReportItem>();
                    var branchNames = await _lookupDbContext.ChiNhanhs.Select(cn => cn.Macn.Trim()).ToListAsync();
                    foreach (var branchName in branchNames)
                    {
                        var branchDb = _dbContextFactory(branchName);
                        allData.AddRange(await GetDataAsync(branchDb));
                    }
                    ReportData = allData.OrderBy(r => r.Ngay).ThenBy(r => r.MasoDdh).ToList();
                    return Page();
                }
                else
                {
                    ReportData = new List<DonHangReportItem>();
                    return Page();
                }
            }

            db = _dbContextFactory(chiNhanhValue);
            var chiNhanh = await db.ChiNhanhs.AsNoTracking().FirstOrDefaultAsync(s => s.Macn.Trim() == chiNhanhValue);
            TenChiNhanh = chiNhanh?.ChiNhanh1 ?? string.Empty;

            ReportData = await GetDataAsync(db);
            return Page();
        }

        public async Task<IActionResult> OnGetExportAsync(string branch)
        {
            string? chiNhanhValue = branch?.Trim();
            var data = new List<DonHangReportItem>();

            if (string.IsNullOrEmpty(chiNhanhValue))
            {
                if (User.IsInRole("CongTy"))
                {
                    var branchNames = await _lookupDbContext.ChiNhanhs.Select(cn => cn.Macn.Trim()).ToListAsync();
                    foreach (var branchName in branchNames)
                    {
                        var branchDb = _dbContextFactory(branchName);
                        data.AddRange(await GetDataAsync(branchDb));
                    }
                    data = data.OrderBy(r => r.Ngay).ThenBy(r => r.MasoDdh).ToList();
                }
                else
                {
                    return new EmptyResult();
                }
            }
            else
            {
                var db = _dbContextFactory(chiNhanhValue);
                data = await GetDataAsync(db);
            }

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("DDH_ChuaNhap");
                worksheet.Cell(1, 1).Value = "MSĐĐH";
                worksheet.Cell(1, 2).Value = "Ngày Lập";
                worksheet.Cell(1, 3).Value = "Nhà Cung Cấp";
                worksheet.Cell(1, 4).Value = "Họ Tên NV";
                worksheet.Cell(1, 5).Value = "Tên Vật Tư";
                worksheet.Cell(1, 6).Value = "Số Lượng Đặt";
                worksheet.Cell(1, 7).Value = "Đơn Giá";

                int currentRow = 2;
                foreach (var item in data)
                {
                    worksheet.Cell(currentRow, 1).Value = item.MasoDdh;
                    worksheet.Cell(currentRow, 2).Value = item.Ngay.ToShortDateString();
                    worksheet.Cell(currentRow, 3).Value = item.NhaCc;
                    worksheet.Cell(currentRow, 4).Value = item.HoTenNv;
                    worksheet.Cell(currentRow, 5).Value = item.Tenvt;
                    worksheet.Cell(currentRow, 6).Value = item.Soluong;
                    worksheet.Cell(currentRow, 7).Value = item.Dongia;
                    currentRow++;
                }

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "DonHangChuaNhap.xlsx");
                }
            }
        }
    }
}
