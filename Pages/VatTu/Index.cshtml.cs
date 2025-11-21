using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QLVT.Web.Infrastructure.Branches;

namespace QLVT.Web.Pages.VatTu;

[Authorize(Roles = "CongTy,ChiNhanh,User")]
public class IndexModel : PageModel
{
    private readonly IBranchDbContextProvider _branchDb;

    public IndexModel(IBranchDbContextProvider branchDb)
    {
        _branchDb = branchDb;
    }

    public IList<Data.Models.Vattu> VatTus { get; set; } = new List<Data.Models.Vattu>();

    public async Task OnGetAsync()
    {
        var db = _branchDb.DbContext;

        VatTus = await db.Vattus
            .OrderBy(v => v.Mavt)
            .ToListAsync();
    }
}
