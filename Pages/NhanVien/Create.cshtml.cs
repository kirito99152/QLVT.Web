using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QLVT.Web.Data;
using QLVT.Web.Data.Models;
using QLVT.Web.Infrastructure.Branches;
using QLVT.Web.Identity;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace QLVT.Web.Pages.NhanVien

{
    [Authorize(Roles = "ChiNhanh")]
    public class CreateModel : PageModel
    {
        private readonly IBranchDbContextProvider _branchDb;
        private readonly UserManager<ApplicationUser> _userManager;

        public CreateModel(
            IBranchDbContextProvider branchDb,
            UserManager<ApplicationUser> userManager)
        {
            _branchDb = branchDb;
            _userManager = userManager;
        }

        [BindProperty]
        public QLVT.Web.Data.Models.NhanVien NhanVien { get; set; } = new();

        [BindProperty]
        public CreateInputModel Input { get; set; } = new();

        public SelectList RoleSelectList { get; set; } = new SelectList(Enumerable.Empty<SelectListItem>());

        public IActionResult OnGet()
        {
            // Chỉ cho phép tạo user với role ChiNhanh hoặc User
            var roles = new[] { new { Id = "ChiNhanh", Name = "Chi nhánh" }, new { Id = "User", Name = "User" } };
            RoleSelectList = new SelectList(roles, "Id", "Name");
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var roles = new[] { new { Id = "ChiNhanh", Name = "Chi nhánh" }, new { Id = "User", Name = "User" } };
            RoleSelectList = new SelectList(roles, "Id", "Name");

            if (!ModelState.IsValid) // Kiểm tra cả NhanVien và Input
            {
                return Page();
            }

            var db = _branchDb.DbContext;
            var currentBranchCode = User.FindFirst("BranchCode")?.Value;

            // Bước 1: Tạo ApplicationUser
            var user = new ApplicationUser
            {
                UserName = Input.Email,
                Email = Input.Email,
                BranchCode = currentBranchCode,
                EmailConfirmed = true // Bỏ qua bước xác thực email cho tiện
            };

            var result = await _userManager.CreateAsync(user, Input.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return Page();
            }

            // Bước 2: Gán Role cho User
            await _userManager.AddToRoleAsync(user, Input.Role);

            try
            {
                // Bước 3: Tạo NhanVien với Id từ User vừa tạo
                NhanVien.Manv = Guid.Parse(user.Id); // Lấy Id của user làm Manv
                NhanVien.Macn = currentBranchCode;
                NhanVien.TrangThaiXoa = 0; // Mặc định là đang hoạt động

                db.NhanViens.Add(NhanVien);
                await db.SaveChangesAsync(); // Lưu NhanVien vào QlvtDbContext

                // Bước 4: Cập nhật lại Manv cho ApplicationUser (để liên kết 2 chiều)
                user.Manv = NhanVien.Manv;
                await _userManager.UpdateAsync(user);
            }
            catch (Exception)
            {
                // Nếu có lỗi khi lưu NhanVien, thực hiện "hoàn tác" bằng cách xóa User đã tạo
                await _userManager.DeleteAsync(user);
                
                // Ghi log và báo lỗi cho người dùng
                ModelState.AddModelError(string.Empty, "Đã có lỗi xảy ra khi tạo thông tin nhân viên. Vui lòng thử lại.");
                // Có thể ghi log chi tiết lỗi `ex` ở đây để debug
                return Page();
            }

            return RedirectToPage("./Index");
        }
    }
}