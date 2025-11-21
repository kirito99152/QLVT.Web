using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QLVT.Web.Identity;
using System.ComponentModel.DataAnnotations;

namespace QLVT.Web.Pages.System
{
    [Authorize(Roles = "CongTy")]
    public class ChangeUserPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ChangeUserPasswordModel> _logger;

        public ChangeUserPasswordModel(UserManager<ApplicationUser> userManager, ILogger<ChangeUserPasswordModel> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required]
            public string UserId { get; set; } = string.Empty;

            public string? UserName { get; set; }

            [Required(ErrorMessage = "Phải nhập mật khẩu mới.")]
            [StringLength(100, ErrorMessage = "{0} phải dài từ {2} đến {1} ký tự.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Mật khẩu mới")]
            public string NewPassword { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Display(Name = "Xác nhận mật khẩu mới")]
            [Compare("NewPassword", ErrorMessage = "Mật khẩu mới và mật khẩu xác nhận không khớp.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnGetAsync(string? id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound($"Không tìm thấy người dùng với ID '{id}'.");
            }

            Input = new InputModel
            {
                UserId = user.Id,
                UserName = user.UserName ?? string.Empty
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.FindByIdAsync(Input.UserId);
            if (user == null)
            {
                return NotFound($"Không tìm thấy người dùng với ID '{Input.UserId}'.");
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, Input.NewPassword);

            if (result.Succeeded)
            {
                _logger.LogInformation("Quản trị viên đã đổi mật khẩu cho người dùng {UserId}.", user.Id);
                TempData["StatusMessage"] = $"Đã đổi mật khẩu thành công cho người dùng {user.UserName}.";
                return RedirectToPage("./ManageAccounts");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return Page();
        }
    }
}