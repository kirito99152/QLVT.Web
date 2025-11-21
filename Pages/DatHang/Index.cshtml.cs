using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QLVT.Web.Data.Models;
using QLVT.Web.Infrastructure.Branches;

namespace QLVT.Web.Pages.DatHang;

[Authorize(Roles = "CongTy,ChiNhanh,User")]
public class IndexModel : PageModel
{
    private readonly IBranchDbContextProvider _branchDb;

    public IndexModel(IBranchDbContextProvider branchDb)
    {
        _branchDb = branchDb;
    }

    public IList<Data.Models.DatHang> DatHangs { get; set; } = new List<Data.Models.DatHang>();

    public async Task OnGetAsync()
    {
        var db = _branchDb.DbContext;

        DatHangs = await db.DatHangs
            .Include(d => d.ManvNavigation) // Nạp thông tin nhân viên để hiển thị tên
            .OrderByDescending(d => d.Ngay).ThenBy(d => d.MasoDdh)
            .ToListAsync();
    }
}