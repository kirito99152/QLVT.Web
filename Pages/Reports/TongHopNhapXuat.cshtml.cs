using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QLVT.Web.Data;
using QLVT.Web.Infrastructure.Branches;
using System.Globalization;

namespace QLVT.Web.Pages.Reports
{
    [Authorize(Roles = "CongTy,ChiNhanh")]
    public class TongHopNhapXuatModel : PageModel
    {
        private readonly IBranchDbContextProvider _branchDb;
        private readonly Func<string, QlvtDbContext> _dbContextFactory;
        private readonly QlvtLookupDbContext _lookupDbContext;

        public TongHopNhapXuatModel(IBranchDbContextProvider branchDb, Func<string, QlvtDbContext> dbContextFactory, QlvtLookupDbContext lookupDbContext)
        {
            _branchDb = branchDb;
            _dbContextFactory = dbContextFactory;
            _lookupDbContext = lookupDbContext;
        }

        [TempData]
        public string? ErrorMessage { get; set; }
        public SelectList? ChiNhanhSelectList { get; set; }
        public string? CurrentBranch { get; private set; }

        public async Task OnGetAsync(string? branch)
        {
            string? chiNhanhValue = branch?.Trim();

            CurrentBranch = User.FindFirst("BranchCode")?.Value;
            if (User.IsInRole("ChiNhanh"))
            {
                chiNhanhValue = CurrentBranch;
            }

            if (User.IsInRole("CongTy"))
            {
                var chiNhanhs = await _lookupDbContext.ChiNhanhs
                    .Select(cn => new { Macn = cn.Macn.Trim(), cn.ChiNhanh1 })
                    .ToListAsync();
                ChiNhanhSelectList = new SelectList(chiNhanhs, "Macn", "ChiNhanh1", chiNhanhValue);
            }
        }

        public class TongHopNhapXuatRow
        {
            public DateOnly Ngay { get; set; }
            public decimal TongNhap { get; set; }
            public decimal TongXuat { get; set; }
            public decimal TyLeNhap { get; set; }   // 0–1
            public decimal TyLeXuat { get; set; }   // 0–1
        }

        public async Task<IActionResult> OnGetExportTongHopNXAsync(
            string branch,
            DateOnly? fromDate,
            DateOnly? toDate)
        {
            if (fromDate == null || toDate == null)
            {
                ErrorMessage = "Vui lòng chọn khoảng thời gian.";
                await OnGetAsync(branch);
                return Page();
            }

            string? chiNhanhValue = branch?.Trim();
            if (User.IsInRole("ChiNhanh"))
            {
                chiNhanhValue = User.FindFirst("BranchCode")?.Value;
            }

            if (string.IsNullOrEmpty(chiNhanhValue))
            {
                ErrorMessage = "Vui lòng chọn chi nhánh.";
                await OnGetAsync(branch);
                return Page();
            }

            var db = _dbContextFactory(chiNhanhValue);

            // ===== 1. Tổng tiền nhập theo ngày =====
            var nhapTheoNgay = await (
                from pn in db.PhieuNhaps
                join ctpn in db.Ctpns on pn.Mapn equals ctpn.Mapn
                where pn.Ngay >= fromDate && pn.Ngay <= toDate
                group new { pn, ctpn } by pn.Ngay
                into g
                select new
                {
                    Ngay = g.Key,
                    TongNhap = g.Sum(x => x.ctpn.Soluong * (decimal)x.ctpn.Dongia)
                }).ToListAsync();

            // ===== 2. Tổng tiền xuất theo ngày =====
            var xuatTheoNgay = await (
                from px in db.PhieuXuats
                join ctpx in db.Ctpxes on px.Mapx equals ctpx.Mapx
                where px.Ngay >= fromDate && px.Ngay <= toDate
                group new { px, ctpx } by px.Ngay
                into g
                select new
                {
                    Ngay = g.Key,
                    TongXuat = g.Sum(x => x.ctpx.Soluong * (decimal)x.ctpx.Dongia)
                }).ToListAsync();

            // ===== 3. Gộp 2 list lại theo ngày =====
            var nhapDict = nhapTheoNgay.ToDictionary(x => x.Ngay, x => x.TongNhap);
            var xuatDict = xuatTheoNgay.ToDictionary(x => x.Ngay, x => x.TongXuat);

            var allDates = nhapDict.Keys
                .Union(xuatDict.Keys)
                .OrderBy(d => d)
                .ToList();

            var rows = new List<TongHopNhapXuatRow>();

            foreach (var d in allDates)
            {
                nhapDict.TryGetValue(d, out var tn);
                xuatDict.TryGetValue(d, out var tx);

                rows.Add(new TongHopNhapXuatRow
                {
                    Ngay = d,
                    TongNhap = tn,
                    TongXuat = tx
                });
            }

            if (!rows.Any())
            {
                ErrorMessage = "Không có dữ liệu nhập/xuất trong khoảng thời gian đã chọn.";
                await OnGetAsync(branch);
                return Page();
            }

            // ===== 4. Tính tỷ lệ % so với tổng =====
            var tongNhapAll = rows.Sum(r => r.TongNhap);
            var tongXuatAll = rows.Sum(r => r.TongXuat);

            foreach (var r in rows)
            {
                r.TyLeNhap = tongNhapAll > 0 ? r.TongNhap / tongNhapAll : 0;
                r.TyLeXuat = tongXuatAll > 0 ? r.TongXuat / tongXuatAll : 0;
            }

            var fileBytes = GenerateTongHopNhapXuatExcel(
                fromDate.Value,
                toDate.Value,
                rows,
                tongNhapAll,
                tongXuatAll);

            var fileName = $"TongHopNhapXuat_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}.xlsx";
            return File(fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        private byte[] GenerateTongHopNhapXuatExcel(
            DateOnly fromDate,
            DateOnly toDate,
            List<TongHopNhapXuatRow> rows,
            decimal tongNhapAll,
            decimal tongXuatAll)
        {
            using (var wb = new XLWorkbook())
            {
                var ws = wb.Worksheets.Add("TongHopNX");
                int row = 1;

                // Tiêu đề
                ws.Cell(row, 1).Value = "BẢNG TỔNG HỢP NHẬP XUẤT";
                ws.Range(row, 1, row, 5).Merge();
                ws.Range(row, 1, row, 5).Style
                    .Font.SetBold()
                    .Font.SetFontSize(14)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                row++;

                ws.Cell(row, 1).Value =
                    $"TỪ {fromDate:dd/MM/yy} ĐẾN {toDate:dd/MM/yy}";
                ws.Range(row, 1, row, 5).Merge();
                ws.Range(row, 1, row, 5).Style
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                row += 2;

                // Header bảng
                int headerRow = row;
                ws.Cell(headerRow, 1).Value = "NGÀY";
                ws.Cell(headerRow, 2).Value = "NHẬP";
                ws.Cell(headerRow, 3).Value = "TỶ LỆ";
                ws.Cell(headerRow, 4).Value = "XUẤT";
                ws.Cell(headerRow, 5).Value = "TỶ LỆ";

                var headerRange = ws.Range(headerRow, 1, headerRow, 5);
                headerRange.Style.Font.SetBold();
                headerRange.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                row++;

                // Dữ liệu từng ngày
                foreach (var r in rows)
                {
                    ws.Cell(row, 1).Value = r.Ngay.ToString("dd/MM/yyyy");
                    ws.Cell(row, 2).Value = r.TongNhap;
                    ws.Cell(row, 3).Value = r.TyLeNhap;   // 0–1
                    ws.Cell(row, 4).Value = r.TongXuat;
                    ws.Cell(row, 5).Value = r.TyLeXuat;   // 0–1

                    ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0";
                    ws.Cell(row, 3).Style.NumberFormat.Format = "0.00%";
                    ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0";
                    ws.Cell(row, 5).Style.NumberFormat.Format = "0.00%";

                    ws.Range(row, 1, row, 5).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    ws.Range(row, 1, row, 5).Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                    row++;
                }

                // Dòng CỘNG
                ws.Cell(row, 1).Value = "CỘNG";
                ws.Cell(row, 1).Style.Font.SetBold();

                ws.Cell(row, 2).Value = tongNhapAll;
                ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0";
                ws.Cell(row, 2).Style.Font.SetBold();

                ws.Cell(row, 4).Value = tongXuatAll;
                ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0";
                ws.Cell(row, 4).Style.Font.SetBold();

                ws.Range(row, 1, row, 5).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                ws.Range(row, 1, row, 5).Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                row += 2;

                // Ghi chú
                ws.Cell(row, 1).Value = "Ghi chú:";
                ws.Cell(row, 1).Style.Font.SetBold();
                row++;
                ws.Cell(row, 1).Value = "- Mỗi ngày thể hiện một dòng, tỷ lệ là % so với tổng.";
                row++;
                ws.Cell(row, 1).Value = "- Tổng cộng dòng cuối cùng là tổng số tiền trong khoảng thời gian.";

                ws.Columns().AdjustToContents();

                using (var ms = new MemoryStream())
                {
                    wb.SaveAs(ms);
                    return ms.ToArray();
                }
            }
        }
    }
}