using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QLVT.Web.Data;
using QLVT.Web.Infrastructure.Branches;

namespace QLVT.Web.Pages.VatTu;

[Authorize(Roles = "ChiNhanh,User")]
public class CreateModel : PageModel
{
    private readonly IBranchDbContextProvider _branchDb;
    private readonly Func<string, QlvtDbContext> _dbFactory;

    public CreateModel(IBranchDbContextProvider branchDb, Func<string, QlvtDbContext> dbFactory)
    {
        _branchDb = branchDb;
        _dbFactory = dbFactory;
    }

    [BindProperty]
    public Data.Models.Vattu VatTu { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        if (!await IsMaVtUniqueAsync(VatTu.Mavt))
        {
            ModelState.AddModelError("VatTu.Mavt", "Mã vật tư đã tồn tại ở chi nhánh khác.");
            return Page();
        }

        var db = _branchDb.DbContext;
        db.Vattus.Add(VatTu);
        await db.SaveChangesAsync();

        return RedirectToPage("Index");
    }

    private async Task<bool> IsMaVtUniqueAsync(string mavt)
    {
        if (string.IsNullOrWhiteSpace(mavt))
            return false;

        var branches = new[] { "CN1", "CN2" };
        foreach (var branch in branches)
        {
            await using var db = _dbFactory(branch);
            if (await db.Vattus.AnyAsync(v => v.Mavt == mavt))
                return false;
        }

        return true;
    }
}
