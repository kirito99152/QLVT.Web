using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QLVT.Web.Data.Models;
using QLVT.Web.Infrastructure.Branches;

namespace QLVT.Web.Pages.PhieuNhap;

[Authorize(Roles = "CongTy,ChiNhanh,User")]
public class IndexModel : PageModel
{
    private readonly IBranchDbContextProvider _branchDb;

    public IndexModel(IBranchDbContextProvider branchDb)
    {
        _branchDb = branchDb;
    }

    public IList<Data.Models.PhieuNhap> PhieuNhaps { get; set; } = new List<Data.Models.PhieuNhap>();

    public async Task OnGetAsync()
    {
        var db = _branchDb.DbContext;
        PhieuNhaps = await db.PhieuNhaps
            .Include(p => p.ManvNavigation)
            .OrderByDescending(p => p.Ngay).ThenBy(p => p.Mapn)
            .ToListAsync();
    }
}