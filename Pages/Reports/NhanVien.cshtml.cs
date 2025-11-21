using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QLVT.Web.Data;
using QLVT.Web.Data.Models;
using QLVT.Web.Identity;
using System.Data;
using QLVT.Web.Infrastructure.Branches;
using System.Security.Claims;

namespace QLVT.Web.Pages.Reports
{
    [Authorize(Roles = "CongTy,ChiNhanh")]
    public class NhanVienModel : PageModel
    {
        private readonly IBranchDbContextProvider _branchDb;
        private readonly Func<string, QlvtDbContext> _dbContextFactory;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly QlvtLookupDbContext _lookupDbContext;

        public NhanVienModel(IBranchDbContextProvider branchDb, Func<string, QlvtDbContext> dbContextFactory, UserManager<ApplicationUser> userManager, QlvtLookupDbContext lookupDbContext)
        {
            _branchDb = branchDb;
            _dbContextFactory = dbContextFactory;
            _userManager = userManager;
            _lookupDbContext = lookupDbContext;
        }

        public List<QLVT.Web.Data.Models.NhanVien> NhanViens { get; set; } = new();
        public SelectList? ChiNhanhSelectList { get; set; }
        public string? TenChiNhanh { get; set; }
        
        [TempData]
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(string? branch)
        {
            string? chiNhanhValue = branch?.Trim();
            QlvtDbContext db;

            if (User.IsInRole("ChiNhanh"))
            {
                db = _branchDb.DbContext;
                // Lấy mã chi nhánh từ claim của user
                // Đối với user ChiNhanh, chi nhánh được xác định bởi IBranchDbContextProvider
                // và đã được dùng để tạo 'db'. Ta chỉ cần lấy lại thông tin.
                chiNhanhValue = _branchDb.DbContext.Database.GetDbConnection().DataSource.Contains("CN1") ? "CN1" : "CN2"; // Cách lấy lại branch name từ connection
                db = _branchDb.DbContext; // Sử dụng context đã có
            }

            // Luôn tải SelectList cho user CongTy
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
                    // Lấy dữ liệu từ tất cả các chi nhánh
                    TenChiNhanh = "Tất cả chi nhánh";
                    var allNhanViens = new List<QLVT.Web.Data.Models.NhanVien>();
                    var chiNhanhs = await _lookupDbContext.ChiNhanhs.AsNoTracking().ToListAsync();
                    foreach (var cn in chiNhanhs)
                    {
                        var dbBranch = _dbContextFactory(cn.Macn.Trim());
                        var nhanViensInBranch = await dbBranch.NhanViens
                            .Where(nv => nv.TrangThaiXoa == 0)
                            .ToListAsync();
                        allNhanViens.AddRange(nhanViensInBranch);
                    }
                    NhanViens = allNhanViens;
                }
                else
                {
                    NhanViens = new List<QLVT.Web.Data.Models.NhanVien>();
                }
            }
            else
            {
                db = _dbContextFactory(chiNhanhValue);
                var chiNhanh = await db.ChiNhanhs.AsNoTracking().FirstOrDefaultAsync(s => s.Macn.Trim() == chiNhanhValue);
                TenChiNhanh = chiNhanh?.ChiNhanh1 ?? string.Empty;
                NhanViens = await db.NhanViens.Where(nv => nv.TrangThaiXoa == 0).ToListAsync();
            }
            
            return Page();
        }

        public async Task<IActionResult> OnGetExportAsync(string branch)
        {
            string? chiNhanhValue = branch?.Trim();
            QlvtDbContext db;

            if (User.IsInRole("ChiNhanh"))
            {
                // Tương tự như OnGetViewAsync, lấy chi nhánh từ context đã được thiết lập
                chiNhanhValue = _branchDb.DbContext.Database.GetDbConnection().DataSource.Contains("CN1") ? "CN1" : "CN2";
            }

            if (string.IsNullOrEmpty(chiNhanhValue))
            {
                ErrorMessage = "Vui lòng chọn một chi nhánh để xuất báo cáo.";
                await OnGetAsync(branch); // Tải lại select list
                return Page();
            }

            // Dùng factory để tạo DbContext cho đúng chi nhánh đã chọn
            db = _dbContextFactory(chiNhanhValue);

            var data = await db.NhanViens
                .Where(nv => nv.TrangThaiXoa == 0)
                .ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("DS_NhanVien");
                worksheet.Cell(1, 1).Value = "Mã NV";
                worksheet.Cell(1, 2).Value = "Họ và Tên";
                worksheet.Cell(1, 3).Value = "Địa chỉ";
                worksheet.Cell(1, 4).Value = "Ngày Sinh";
                worksheet.Cell(1, 5).Value = "Lương";

                int currentRow = 2;
                foreach (var nv in data)
                {
                    worksheet.Cell(currentRow, 1).Value = nv.Manv.ToString(); // Chuyển Guid thành chuỗi
                    worksheet.Cell(currentRow, 2).Value = $"{nv.Ho} {nv.Ten}";
                    worksheet.Cell(currentRow, 3).Value = nv.Diachi;
                    worksheet.Cell(currentRow, 4).Value = nv.Ngaysinh;
                    worksheet.Cell(currentRow, 5).Value = nv.Luong;
                    currentRow++;
                }

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "DanhSachNhanVien.xlsx");
                }
            }
        }
    }
}