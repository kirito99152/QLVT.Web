using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QLVT.Web.Infrastructure.Branches;

namespace QLVT.Web.Pages.VatTu;

[Authorize(Roles = "ChiNhanh,User")]
public class EditModel : PageModel
{
    private readonly IBranchDbContextProvider _branchDb;

    public EditModel(IBranchDbContextProvider branchDb)
    {
        _branchDb = branchDb;
    }

    [BindProperty]
    public Data.Models.Vattu VatTu { get; set; } = default!;

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

        if (!ModelState.IsValid)
            return Page();

        if (!string.Equals(id, VatTu.Mavt, StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError("VatTu.Mavt", "Không được đổi mã vật tư.");
            return Page();
        }

        var db = _branchDb.DbContext;
        db.Attach(VatTu).State = EntityState.Modified;

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            var exists = await db.Vattus.AnyAsync(v => v.Mavt == VatTu.Mavt);
            if (!exists)
                return NotFound();
            throw;
        }

        return RedirectToPage("Index");
    }
}
