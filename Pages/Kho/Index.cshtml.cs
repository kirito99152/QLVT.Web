using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QLVT.Web.Infrastructure.Branches;

namespace QLVT.Web.Pages.Kho;

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

    public IList<Data.Models.Kho> Khos { get; set; } = new List<Data.Models.Kho>();

    public async Task OnGetAsync()
    {
        var db = _branchDb.DbContext;
        var currentBranch = _branchProvider.CurrentBranch;

        Khos = await db.Khos
            .Where(k => k.Macn == currentBranch)
            .OrderBy(k => k.Makho)
            .ToListAsync();
    }
}
