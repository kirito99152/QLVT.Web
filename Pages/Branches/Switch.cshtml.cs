using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using QLVT.Web.Identity;

namespace QLVT.Web.Pages.Branches;

[Authorize(Roles = "CongTy")]
public class SwitchModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public SwitchModel(UserManager<ApplicationUser> userManager,
                       SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [BindProperty]
    public string SelectedBranch { get; set; } = "CN1";

    public async Task OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return;

        var claims = await _userManager.GetClaimsAsync(user);
        var currentBranchClaim = claims.FirstOrDefault(c => c.Type == "CurrentBranch");

        if (currentBranchClaim != null && !string.IsNullOrEmpty(currentBranchClaim.Value))
        {
            SelectedBranch = currentBranchClaim.Value;
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (SelectedBranch != "CN1" && SelectedBranch != "CN2")
        {
            ModelState.AddModelError("", "Chi nhánh không hợp lệ.");
            return Page();
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToPage("/Account/Login", new { area = "Identity" });
        }

        // Lấy tất cả claims hiện tại của user
        var claims = await _userManager.GetClaimsAsync(user);
        var existingBranchClaim = claims.FirstOrDefault(c => c.Type == "CurrentBranch");

        // Xoá claim cũ nếu có
        if (existingBranchClaim != null)
        {
            await _userManager.RemoveClaimAsync(user, existingBranchClaim);
        }

        // Thêm claim mới
        var newBranchClaim = new Claim("CurrentBranch", SelectedBranch);
        await _userManager.AddClaimAsync(user, newBranchClaim);

        // Refresh lại cookie đăng nhập để cập nhật claim
        await _signInManager.RefreshSignInAsync(user);

        return RedirectToPage("/Index");
    }
}
