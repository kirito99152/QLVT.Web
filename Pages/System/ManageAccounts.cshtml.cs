using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QLVT.Web.Identity;

namespace QLVT.Web.Pages.System
{
    [Authorize(Roles = "CongTy")]
    public class ManageAccountsModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public ManageAccountsModel(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [TempData]
        public string? StatusMessage { get; set; }

        public List<UserViewModel> Users { get; set; } = new List<UserViewModel>();

        public class UserViewModel
        {
            public string UserId { get; set; } = string.Empty;
            public string UserName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Roles { get; set; } = string.Empty;
            public string BranchCode { get; set; } = string.Empty;
            public Guid? Manv { get; set; }
        }

        public async Task OnGetAsync()
        {
            var users = await _userManager.Users.ToListAsync();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                Users.Add(new UserViewModel
                {
                    UserId = user.Id,
                    UserName = user.UserName ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    Roles = string.Join(", ", roles),
                    BranchCode = user.BranchCode ?? "N/A",
                    Manv = user.Manv
                });
            }
        }
    }
}