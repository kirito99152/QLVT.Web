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
    public class HoatDongNhanVienModel : PageModel
    {
        private readonly IBranchDbContextProvider _branchDb;
        private readonly Func<string, QlvtDbContext> _dbContextFactory;
        private readonly QlvtLookupDbContext _lookupDbContext;

        public HoatDongNhanVienModel(IBranchDbContextProvider branchDb, Func<string, QlvtDbContext> dbContextFactory, QlvtLookupDbContext lookupDbContext)
        {
            _branchDb = branchDb;
            _dbContextFactory = dbContextFactory;
            _lookupDbContext = lookupDbContext;
        }

        [TempData]
        public string? ErrorMessage { get; set; }
        public SelectList? ChiNhanhSelectList { get; set; }
        public SelectList? NhanVienSelectList { get; set; }
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

            if (!string.IsNullOrEmpty(chiNhanhValue))
            {
                var db = _dbContextFactory(chiNhanhValue);
                var nhanViens = await db.NhanViens
                    .Where(nv => nv.TrangThaiXoa == 0)
                    .Select(nv => new { nv.Manv, HoTen = nv.Ho + " " + nv.Ten })
                    .ToListAsync();
                NhanVienSelectList = new SelectList(nhanViens, "Manv", "HoTen");
            }
        }

        public async Task<JsonResult> OnGetNhanViensAsync(string branch)
        {
            if (string.IsNullOrEmpty(branch))
            {
                return new JsonResult(new List<SelectListItem>());
            }
            var db = _dbContextFactory(branch);
            var nhanViens = await db.NhanViens
                .Where(nv => nv.TrangThaiXoa == 0)
                .Select(nv => new SelectListItem { Value = nv.Manv.ToString(), Text = nv.Ho + " " + nv.Ten })
                .ToListAsync();
            return new JsonResult(nhanViens);
        }

        public class EmployeeActivityRow
        {
            public DateOnly Ngay { get; set; }
            public string SoPhieu { get; set; } = "";
            public string LoaiPhieu { get; set; } = ""; // "Nhập" hoặc "Xuất"
            public string? KhachHang { get; set; }
            public string TenVatTu { get; set; } = "";
            public int SoLuong { get; set; }
            public decimal DonGia { get; set; }
            public decimal TriGia => SoLuong * DonGia;
        }

        public async Task<IActionResult> OnGetExportActivityAsync(string branch, string manv, DateOnly? fromDate, DateOnly? toDate)
        {
            if (string.IsNullOrWhiteSpace(manv) || fromDate == null || toDate == null)
            {
                ErrorMessage = "Vui lòng chọn nhân viên và khoảng thời gian.";
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

            if (!Guid.TryParse(manv, out Guid manvGuid))
            {
                ErrorMessage = "Mã nhân viên không hợp lệ.";
                await OnGetAsync(branch);
                return Page();
            }

            var nv = await db.NhanViens.FirstOrDefaultAsync(x => x.Manv == manvGuid);
            if (nv == null)
            {
                ErrorMessage = "Không tìm thấy nhân viên.";
                await OnGetAsync(branch);
                return Page();
            }
            string hoTenNv = $"{nv.Ho} {nv.Ten}".Trim();

            var nhapQuery =
                from pn in db.PhieuNhaps
                join ctpn in db.Ctpns on pn.Mapn equals ctpn.Mapn
                join vt in db.Vattus on ctpn.Mavt equals vt.Mavt
                join dh in db.DatHangs on pn.MasoDdh equals dh.MasoDdh
                where pn.Manv == manvGuid && pn.Ngay >= fromDate && pn.Ngay <= toDate
                select new EmployeeActivityRow
                {
                    Ngay = pn.Ngay,
                    SoPhieu = pn.Mapn,
                    LoaiPhieu = "Nhập",
                    KhachHang = dh.NhaCc,
                    TenVatTu = vt.Tenvt,
                    SoLuong = ctpn.Soluong,
                    DonGia = (decimal)ctpn.Dongia
                };

            var xuatQuery =
                from px in db.PhieuXuats
                join ctpx in db.Ctpxes on px.Mapx equals ctpx.Mapx
                join vt in db.Vattus on ctpx.Mavt equals vt.Mavt
                where px.Manv == manvGuid && px.Ngay >= fromDate && px.Ngay <= toDate
                select new EmployeeActivityRow
                {
                    Ngay = px.Ngay,
                    SoPhieu = px.Mapx,
                    LoaiPhieu = "Xuất",
                    KhachHang = px.Hotenkh,
                    TenVatTu = vt.Tenvt,
                    SoLuong = ctpx.Soluong,
                    DonGia = (decimal)ctpx.Dongia
                };

            var activities = (await nhapQuery.ToListAsync())
                .Concat(await xuatQuery.ToListAsync())
                .OrderBy(a => a.Ngay)
                .ThenBy(a => a.SoPhieu)
                .ToList();

            if (!activities.Any())
            {
                ErrorMessage = "Nhân viên không có chứng từ nào trong khoảng thời gian đã chọn.";
                await OnGetAsync(branch);
                return Page();
            }

            var bytes = GenerateEmployeeActivityExcel(hoTenNv, fromDate.Value, toDate.Value, activities);
            var fileName = $"HoatDongNhanVien_{hoTenNv}_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        private byte[] GenerateEmployeeActivityExcel(string hoTenNhanVien, DateOnly fromDate, DateOnly toDate, List<EmployeeActivityRow> data)
        {
            using (var workbook = new XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("HoatDongNhanVien");
                int row = 1;

                ws.Cell(row, 1).Value = "HOẠT ĐỘNG NHÂN VIÊN";
                ws.Range(row, 1, row, 8).Merge().Style.Font.SetBold().Font.SetFontSize(14).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                row++;

                ws.Cell(row, 1).Value = $"Từ ngày: {fromDate:dd/MM/yyyy} đến ngày {toDate:dd/MM/yyyy}";
                ws.Range(row, 1, row, 8).Merge().Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                row += 2;

                ws.Cell(row, 1).Value = $"Họ tên nhân viên : {hoTenNhanVien}";
                ws.Range(row, 1, row, 8).Merge();
                row++;

                ws.Cell(row, 1).Value = $"Ngày lập báo cáo: {DateTime.Now:dd/MM/yyyy}";
                ws.Range(row, 1, row, 8).Merge();
                row += 2;

                int headerRow = row;
                ws.Cell(headerRow, 1).Value = "Ngày";
                ws.Cell(headerRow, 2).Value = "Số phiếu";
                ws.Cell(headerRow, 3).Value = "Loại phiếu";
                ws.Cell(headerRow, 4).Value = "Khách hàng/NCC";
                ws.Cell(headerRow, 5).Value = "Tên vật tư";
                ws.Cell(headerRow, 6).Value = "Số lượng";
                ws.Cell(headerRow, 7).Value = "Đơn giá";
                ws.Cell(headerRow, 8).Value = "Trị giá";
                var headerRange = ws.Range(headerRow, 1, headerRow, 8);
                headerRange.Style.Font.SetBold().Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center).Border.SetOutsideBorder(XLBorderStyleValues.Thin).Border.SetInsideBorder(XLBorderStyleValues.Thin);
                row++;

                decimal tongCong = 0;
                var groups = data.OrderBy(a => a.Ngay).GroupBy(a => new { a.Ngay.Year, a.Ngay.Month });

                foreach (var g in groups)
                {
                    ws.Cell(row, 1).Value = $"Tháng : {g.Key.Month:00}/{g.Key.Year}";
                    ws.Range(row, 1, row, 8).Merge().Style.Font.SetBold();
                    row++;

                    decimal monthTotal = 0;
                    foreach (var item in g)
                    {
                        ws.Cell(row, 1).Value = item.Ngay.ToString("dd/MM/yyyy");
                        ws.Cell(row, 2).Value = item.SoPhieu;
                        ws.Cell(row, 3).Value = item.LoaiPhieu;
                        ws.Cell(row, 4).Value = item.KhachHang;
                        ws.Cell(row, 5).Value = item.TenVatTu;
                        ws.Cell(row, 6).Value = item.SoLuong;
                        ws.Cell(row, 7).Value = item.DonGia;
                        ws.Cell(row, 8).Value = item.TriGia;
                        ws.Range(row, 1, row, 8).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin).Border.SetInsideBorder(XLBorderStyleValues.Thin);
                        ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0";
                        ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00";
                        ws.Cell(row, 8).Style.NumberFormat.Format = "#,##0.00";
                        monthTotal += item.TriGia;
                        row++;
                    }

                    ws.Cell(row, 1).Value = $"Tổng tháng {g.Key.Month:00}/{g.Key.Year}";
                    ws.Range(row, 1, row, 7).Merge().Style.Font.SetBold();
                    ws.Cell(row, 8).Value = monthTotal;
                    ws.Cell(row, 8).Style.NumberFormat.Format = "#,##0.00";
                    ws.Range(row, 1, row, 8).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin).Border.SetInsideBorder(XLBorderStyleValues.Thin);
                    tongCong += monthTotal;
                    row++;
                }

                row++;
                ws.Cell(row, 1).Value = $"Tổng cộng: {tongCong:N0} đồng";
                ws.Range(row, 1, row, 4).Merge().Style.Font.SetBold();
                row++;
                ws.Cell(row, 1).Value = $"({NumberToVietnameseText((long)tongCong)} đồng chẵn)";
                ws.Range(row, 1, row, 8).Merge().Style.Font.SetBold();

                ws.Columns().AdjustToContents();
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }

        private static readonly string[] ChuSo = { "không", "một", "hai", "ba", "bốn", "năm", "sáu", "bảy", "tám", "chín" };
        private static string ReadThreeDigits(int number, bool readZeroHundred)
        {
            int tram = number / 100;
            int chuc = (number % 100) / 10;
            int donVi = number % 10;
            var parts = new List<string>();
            if (readZeroHundred || tram > 0) { parts.Add(ChuSo[tram] + " trăm"); }
            if (chuc > 1)
            {
                parts.Add(ChuSo[chuc] + " mươi");
                if (donVi == 1) parts.Add("mốt");
                else if (donVi > 1) parts.Add(ChuSo[donVi]);
            }
            else if (chuc == 1)
            {
                parts.Add("mười");
                if (donVi == 5) parts.Add("lăm");
                else if (donVi > 0) parts.Add(ChuSo[donVi]);
            }
            else if (donVi > 0)
            {
                if (parts.Any()) parts.Add("linh");
                parts.Add(ChuSo[donVi]);
            }
            return string.Join(" ", parts);
        }

        public static string NumberToVietnameseText(long number)
        {
            if (number == 0) return "Không";
            if (number < 0) return "Âm " + NumberToVietnameseText(-number);
            string[] units = { "", "nghìn", "triệu", "tỷ", "nghìn tỷ", "triệu tỷ" };
            var parts = new List<string>();
            int unitIndex = 0;
            bool isFirstGroup = true;
            while (number > 0)
            {
                int threeDigits = (int)(number % 1000);
                if (threeDigits != 0)
                {
                    string segment = ReadThreeDigits(threeDigits, !isFirstGroup);
                    parts.Insert(0, segment + " " + units[unitIndex]);
                }
                number /= 1000;
                unitIndex++;
                isFirstGroup = false;
            }
            var result = string.Join(" ", parts).Trim().Replace("  ", " ");
            return char.ToUpper(result[0]) + result.Substring(1);
        }
    }
}