using Microsoft.AspNetCore.Identity;

namespace QLVT.Web.Identity;

public class ApplicationUser : IdentityUser
{
    /// <summary>
    /// Mã chi nhánh (CN1, CN2). Với role Công Ty có thể null.
    /// </summary>
    public string? BranchCode { get; set; }

    /// <summary>
    /// Mã nhân viên tương ứng. Có thể null nếu user không phải là nhân viên.
    /// </summary>
    public Guid? Manv { get; set; }
}
