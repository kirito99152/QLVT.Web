using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using QLVT.Web.Identity;

namespace QLVT.Web.Infrastructure.Branches;

public class ClaimsBranchProvider : IBranchProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserManager<ApplicationUser> _userManager;

    public ClaimsBranchProvider(
        IHttpContextAccessor httpContextAccessor,
        UserManager<ApplicationUser> userManager)
    {
        _httpContextAccessor = httpContextAccessor;
        _userManager = userManager;
    }

    public string CurrentBranch
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null ||
                httpContext.User?.Identity?.IsAuthenticated != true)
            {
                // chưa đăng nhập -> tạm CN1
                return "CN1";
            }

            var userPrincipal = httpContext.User;

            // 1. Nếu là Công ty -> lấy từ claim CurrentBranch
            if (userPrincipal.IsInRole("CongTy"))
            {
                var branchClaim = userPrincipal.FindFirst("CurrentBranch")?.Value;
                return string.IsNullOrEmpty(branchClaim) ? "CN1" : branchClaim;
            }

            // 2. Nếu là ChiNhanh/User -> dùng BranchCode cố định trong ApplicationUser
            var userId = userPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return "CN1";

            var user = _userManager.FindByIdAsync(userId).GetAwaiter().GetResult();
            if (user == null || string.IsNullOrEmpty(user.BranchCode))
                return "CN1";

            return user.BranchCode!;
        }
    }
}
