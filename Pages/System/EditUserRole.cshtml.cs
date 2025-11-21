using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QLVT.Web.Data;
using QLVT.Web.Identity;
using System.ComponentModel.DataAnnotations;

namespace QLVT.Web.Pages.System
{
    [Authorize(Roles = "CongTy")]
    public class EditUserRoleModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<EditUserRoleModel> _logger;

        public EditUserRoleModel(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, ILogger<EditUserRoleModel> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required]
            public string UserId { get; set; } = string.Empty;
            public string? UserName { get; set; }
            public List<string> SelectedRoles { get; set; } = new List<string>();
        }

        public List<IdentityRole> AllRoles { get; set; } = new();

        [TempData]
        public string? StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(string? id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            // Ngăn người dùng tự sửa vai trò của chính mình
            var currentUserId = _userManager.GetUserId(User);
            if (id == currentUserId)
            {
                StatusMessage = "Lỗi: Bạn không thể tự thay đổi vai trò của chính mình.";
                return RedirectToPage("./ManageAccounts");
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            AllRoles = await _roleManager.Roles.ToListAsync();

            Input = new InputModel
            {
                UserId = user.Id,
                UserName = user.UserName ?? string.Empty,
                SelectedRoles = userRoles.ToList()
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Ngăn người dùng tự sửa vai trò của chính mình (kiểm tra lại ở phía POST)
            var currentUserId = _userManager.GetUserId(User);
            if (Input.UserId == currentUserId)
            {
                ModelState.AddModelError(string.Empty, "Bạn không thể tự thay đổi vai trò của chính mình.");
                return Page();
            }

            var user = await _userManager.FindByIdAsync(Input.UserId);
            if (user == null)
            {
                return NotFound();
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            var selectedRoles = Input.SelectedRoles ?? new List<string>();

            var rolesToAdd = selectedRoles.Except(currentRoles).ToList();
            var rolesToRemove = currentRoles.Except(selectedRoles).ToList();

            if (rolesToAdd.Any())
            {
                await _userManager.AddToRolesAsync(user, rolesToAdd);
            }

            if (rolesToRemove.Any())
            {
                await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
            }

            _logger.LogInformation("User {UserId} roles updated by {AdminId}.", user.Id, currentUserId);
            StatusMessage = $"Đã cập nhật vai trò cho người dùng {user.UserName}.";
            return RedirectToPage("./ManageAccounts");
        }
    }
}