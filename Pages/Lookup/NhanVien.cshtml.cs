using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using QLVT.Web.Data;
using QLVT.Web.Data.Lookup;
using QLVT.Web.Data.Models;

namespace QLVT.Web.Pages.Lookup;

[Authorize(Roles = "CongTy,ChiNhanh,User")]
public class NhanVienLookupModel : PageModel
{
    private readonly QlvtLookupDbContext _lookupDb;

    public NhanVienLookupModel(QlvtLookupDbContext lookupDb)
    {
        _lookupDb = lookupDb;
    }

    public IList<NhanVienLookup> NhanViens { get; set; } = new List<NhanVienLookup>();

    public async Task OnGetAsync()
    {
        NhanViens = await _lookupDb.NhanViens
            .OrderBy(nv => nv.Manv)
            .ToListAsync();
    }
}
