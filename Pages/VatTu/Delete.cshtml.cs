using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QLVT.Web.Infrastructure.Branches;

namespace QLVT.Web.Pages.VatTu;

[Authorize(Roles = "ChiNhanh,User")]
public class DeleteModel : PageModel
{
    private readonly IBranchDbContextProvider _branchDb;

    public DeleteModel(IBranchDbContextProvider branchDb)
    {
        _branchDb = branchDb;
    }

    [BindProperty]
    public Data.Models.Vattu VatTu { get; set; } = default!;

    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        if (string.IsNullOrEmpty(id))
            return NotFound();

        var db = _branchDb.DbContext;
        var vatTu = await db.Vattus.FirstOrDefaultAsync(v => v.Mavt == id);
        if (vatTu == null)
            return NotFound();

        VatTu = vatTu;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string id)
    {
        if (string.IsNullOrEmpty(id))
            return NotFound();

        var db = _branchDb.DbContext;
        var vatTu = await db.Vattus.FirstOrDefaultAsync(v => v.Mavt == id);
        if (vatTu == null)
            return NotFound();

        var usedInCtddh = await db.Ctddhs.AnyAsync(c => c.Mavt == id);
        var usedInCtpn = await db.Ctpns.AnyAsync(c => c.Mavt == id);
        var usedInCtpx = await db.Ctpxes.AnyAsync(c => c.Mavt == id);

        if (usedInCtddh || usedInCtpn || usedInCtpx)
        {
            ErrorMessage = "Không thể xoá vật tư vì đã được sử dụng trong chứng từ.";
            VatTu = vatTu;
            return Page();
        }

        db.Vattus.Remove(vatTu);
        await db.SaveChangesAsync();

        return RedirectToPage("Index");
    }
}
