using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using QLVT.Web.Data.Models;
using QLVT.Web.Infrastructure.Branches;

namespace QLVT.Web.Pages.NhanVien;

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
    public Data.Models.NhanVien NhanVien { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var db = _branchDb.DbContext;
        var branch = _branchProvider.CurrentBranch;

        var nv = await db.NhanViens.FirstOrDefaultAsync(n => n.Manv.Equals(id) && n.Macn == branch);
        if (nv == null)
        {
            return NotFound();
        }

        NhanVien = nv;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var db = _branchDb.DbContext;
        var branch = _branchProvider.CurrentBranch;

        if (!ModelState.IsValid)
            return Page();

        // đảm bảo không chỉnh sang chi nhánh khác
        NhanVien.Macn = branch;

        db.Attach(NhanVien).State = EntityState.Modified;

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            var exists = await db.NhanViens.AnyAsync(n => n.Manv.Equals(NhanVien.Manv) && n.Macn == branch);
            if (!exists)
                return NotFound();
            throw;
        }

        return RedirectToPage("Index");
    }
}