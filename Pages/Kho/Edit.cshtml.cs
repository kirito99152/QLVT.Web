using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QLVT.Web.Infrastructure.Branches;

namespace QLVT.Web.Pages.Kho;

[Authorize(Roles = "ChiNhanh,User")]
public class EditModel : PageModel
{
    private readonly IBranchDbContextProvider _branchDb;
    private readonly IBranchProvider _branchProvider;

    public EditModel(IBranchDbContextProvider branchDb, IBranchProvider branchProvider)
    {
        _branchDb = branchDb;
        _branchProvider = branchProvider;
    }

    [BindProperty]
    public Data.Models.Kho Kho { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(string id)
    {
        if (string.IsNullOrEmpty(id))
            return NotFound();

        var db = _branchDb.DbContext;
        var branch = _branchProvider.CurrentBranch;

        var kho = await db.Khos.FirstOrDefaultAsync(k => k.Makho == id && k.Macn == branch);
        if (kho == null)
            return NotFound();

        Kho = kho;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var db = _branchDb.DbContext;
        var branch = _branchProvider.CurrentBranch;

        if (!ModelState.IsValid)
            return Page();

        Kho.Macn = branch;
        db.Attach(Kho).State = EntityState.Modified;

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            var exists = await db.Khos.AnyAsync(k => k.Makho == Kho.Makho && k.Macn == branch);
            if (!exists)
                return NotFound();
            throw;
        }

        return RedirectToPage("Index");
    }
}
