using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QLVT.Web.Data.Models;
using QLVT.Web.Infrastructure.Branches;

namespace QLVT.Web.Pages.NhanVien;

[Authorize(Roles = "CongTy,ChiNhanh,User")]
public class IndexModel : PageModel
{
    private readonly IBranchDbContextProvider _branchDb;
    private readonly IBranchProvider _branchProvider;

    public IndexModel(IBranchDbContextProvider branchDb, IBranchProvider branchProvider)
    {
        _branchDb = branchDb;
        _branchProvider = branchProvider;
    }

    public IList<Data.Models.NhanVien> NhanViens { get; set; } = new List<Data.Models.NhanVien>();

    public async Task OnGetAsync()
    {
        var db = _branchDb.DbContext;
        var currentBranch = _branchProvider.CurrentBranch; // "CN1" hoặc "CN2"

        NhanViens = await db.NhanViens
            .Where(nv => nv.Macn == currentBranch) // nếu trong DB MACN trùng branch
            .OrderBy(nv => nv.Ten).ThenBy(nv => nv.Ho)
            .ToListAsync();
    }
}
