using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using QLVT.Web.Data;
using QLVT.Web.Infrastructure.Branches;

namespace QLVT.Web.Pages.Reports
{
    [Authorize]
    public class BangKeNhapXuatModel : PageModel
    {
        private readonly IBranchDbContextProvider _branchDb;
        private readonly Func<string, QlvtDbContext> _dbContextFactory;
        private readonly QlvtLookupDbContext _lookupDbContext;

        public BangKeNhapXuatModel(IBranchDbContextProvider branchDb, Func<string, QlvtDbContext> dbContextFactory, QlvtLookupDbContext lookupDbContext)
        {
            _branchDb = branchDb;
            _dbContextFactory = dbContextFactory;
            _lookupDbContext = lookupDbContext;
        }

        public SelectList? ChiNhanhSelectList { get; set; }
        public string? CurrentBranch { get; private set; }

        // Lớp nội bộ để hứng kết quả từ SP
        public class ReportItem
        {
            public string ThangNam { get; set; } = string.Empty;
            public string Tenvt { get; set; } = string.Empty;
            public int TongSoLuong { get; set; }
            public decimal TongTriGia { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(string? action, string? type, DateOnly? startDate, DateOnly? endDate, string? branch)
        {
            // Nếu action là "export", thực hiện logic xuất file
            if (action == "export" && !string.IsNullOrEmpty(type) && startDate.HasValue && endDate.HasValue)
            {
                return await ExportDataAsync(type, startDate.Value, endDate.Value, branch);
            }

            // Nếu không, chỉ hiển thị trang như bình thường
            await PreparePageAsync();
            return Page();
        }

        private async Task<List<ReportItem>> GetDataAsync(QlvtDbContext db, string type, DateOnly startDate, DateOnly endDate)
        {
            if (type == "N")
            {
                var query = from pn in db.PhieuNhaps
                            join ctpn in db.Ctpns on pn.Mapn equals ctpn.Mapn
                            join vt in db.Vattus on ctpn.Mavt equals vt.Mavt
                            where pn.Ngay >= startDate && pn.Ngay <= endDate
                            group new { ctpn, vt } by new { pn.Ngay.Year, pn.Ngay.Month, vt.Tenvt } into g
                            select new // Tạm thời select ra dữ liệu thô
                            {
                                g.Key.Year,
                                g.Key.Month,
                                Tenvt = g.Key.Tenvt,
                                TongSoLuong = g.Sum(x => x.ctpn.Soluong),
                                TongTriGia = (decimal)g.Sum(x => x.ctpn.Soluong * x.ctpn.Dongia)
                            };
                // Chuyển sang client-side để định dạng lại chuỗi ThangNam
                var results = await query.ToListAsync();
                return results
                    .OrderBy(r => r.Year).ThenBy(r => r.Month).ThenBy(r => r.Tenvt)
                    .Select(r => new ReportItem
                    {
                        ThangNam = $"{r.Month}/{r.Year}",
                        Tenvt = r.Tenvt,
                        TongSoLuong = r.TongSoLuong,
                        TongTriGia = r.TongTriGia
                    }).ToList();
            }
            else // type == "X"
            {
                var query = from px in db.PhieuXuats
                            join ctpx in db.Ctpxes on px.Mapx equals ctpx.Mapx
                            join vt in db.Vattus on ctpx.Mavt equals vt.Mavt
                            where px.Ngay >= startDate && px.Ngay <= endDate
                            // Giả định giá xuất bằng giá trên CTPX, nếu không có thì cần logic khác
                            group new { ctpx, vt } by new { px.Ngay.Year, px.Ngay.Month, vt.Tenvt } into g
                            select new // Tạm thời select ra dữ liệu thô
                            {
                                g.Key.Year,
                                g.Key.Month,
                                Tenvt = g.Key.Tenvt,
                                TongSoLuong = g.Sum(x => x.ctpx.Soluong),
                                // Giả định CTPX có đơn giá. Nếu không, logic này cần thay đổi.
                                TongTriGia = (decimal)g.Sum(x => x.ctpx.Soluong * (x.ctpx.Dongia))
                            };
                // Chuyển sang client-side để định dạng lại chuỗi ThangNam
                var results = await query.ToListAsync();
                return results
                    .OrderBy(r => r.Year).ThenBy(r => r.Month).ThenBy(r => r.Tenvt)
                    .Select(r => new ReportItem
                    {
                        ThangNam = $"{r.Month}/{r.Year}",
                        Tenvt = r.Tenvt,
                        TongSoLuong = r.TongSoLuong,
                        TongTriGia = r.TongTriGia
                    }).ToList();
            }
        }

        private async Task<IActionResult> ExportDataAsync(string type, DateOnly startDate, DateOnly endDate, string? branch)
        {
            var allData = new List<ReportItem>();
            string? chiNhanhValue = branch?.Trim();

            if (User.IsInRole("CongTy"))
            {
                if (string.IsNullOrEmpty(chiNhanhValue)) // Nếu không chọn chi nhánh, tổng hợp từ tất cả
                {
                    var branchNames = await _lookupDbContext.ChiNhanhs.Select(cn => cn.Macn.Trim()).ToListAsync();
                    foreach (var b in branchNames)
                    {
                        var db = _dbContextFactory(b);
                        allData.AddRange(await GetDataAsync(db, type, startDate, endDate));
                    }
                }
                else // Nếu chọn một chi nhánh cụ thể
                {
                    var db = _dbContextFactory(chiNhanhValue);
                    allData.AddRange(await GetDataAsync(db, type, startDate, endDate));
                }
            }
            else
            {
                // User ChiNhanh/User chỉ xem được dữ liệu của chi nhánh mình
                var db = _branchDb.DbContext;
                allData.AddRange(await GetDataAsync(db, type, startDate, endDate));
            }

            // Nhóm lại kết quả từ các chi nhánh
            var finalData = allData
                .GroupBy(r => new { r.ThangNam, r.Tenvt })
                .Select(g => new ReportItem
                {
                    ThangNam = g.Key.ThangNam,
                    Tenvt = g.Key.Tenvt,
                    TongSoLuong = g.Sum(r => r.TongSoLuong),
                    TongTriGia = g.Sum(r => r.TongTriGia)
                })
                .OrderBy(r => r.ThangNam).ThenBy(r => r.Tenvt)
                .ToList();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("BangKe");
                worksheet.Cell(1, 1).Value = "Tháng/Năm";
                worksheet.Cell(1, 2).Value = "Tên Vật Tư";
                worksheet.Cell(1, 3).Value = "Tổng Số Lượng";
                worksheet.Cell(1, 4).Value = "Tổng Trị Giá";

                int currentRow = 2;
                foreach (var item in finalData)
                {
                    worksheet.Cell(currentRow, 1).Value = item.ThangNam;
                    worksheet.Cell(currentRow, 2).Value = item.Tenvt;
                    worksheet.Cell(currentRow, 3).Value = item.TongSoLuong;
                    worksheet.Cell(currentRow, 4).Value = item.TongTriGia;
                    currentRow++;
                }

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    string loaiPhieu = type == "N" ? "Nhap" : "Xuat";
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"BangKeChiTiet_{loaiPhieu}.xlsx");
                }
            }
        }

        private async Task PreparePageAsync()
        {
            CurrentBranch = User.FindFirst("BranchCode")?.Value;

            if (User.IsInRole("CongTy"))
            {
                var chiNhanhs = await _lookupDbContext.ChiNhanhs
                    .Select(cn => new { Macn = cn.Macn.Trim(), cn.ChiNhanh1 })
                    .ToListAsync();
                ChiNhanhSelectList = new SelectList(chiNhanhs, "Macn", "ChiNhanh1");
            }
        }
    }
}
