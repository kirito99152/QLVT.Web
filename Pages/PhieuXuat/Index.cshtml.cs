using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QLVT.Web.Data.Models;
using QLVT.Web.Infrastructure.Branches;

namespace QLVT.Web.Pages.PhieuXuat;

[Authorize(Roles = "CongTy,ChiNhanh,User")]
public class IndexModel : PageModel
{
    private readonly IBranchDbContextProvider _branchDb;

    public IndexModel(IBranchDbContextProvider branchDb)
    {
        _branchDb = branchDb;
    }

    public IList<Data.Models.PhieuXuat> PhieuXuats { get; set; } = new List<Data.Models.PhieuXuat>();

    public async Task OnGetAsync()
    {
        var db = _branchDb.DbContext;
        PhieuXuats = await db.PhieuXuats
            .Include(p => p.ManvNavigation)
            .OrderByDescending(p => p.Ngay).ThenBy(p => p.Mapx)
            .ToListAsync();
    }
}