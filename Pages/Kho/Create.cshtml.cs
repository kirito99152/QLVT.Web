using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QLVT.Web.Infrastructure.Branches;

namespace QLVT.Web.Pages.Kho;

[Authorize(Roles = "ChiNhanh,User")]
public class CreateModel : PageModel
{
    private readonly IBranchDbContextProvider _branchDb;
    private readonly IBranchProvider _branchProvider;

    public CreateModel(IBranchDbContextProvider branchDb, IBranchProvider branchProvider)
    {
        _branchDb = branchDb;
        _branchProvider = branchProvider;
    }

    [BindProperty]
    public Data.Models.Kho Kho { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var db = _branchDb.DbContext;
        var branch = _branchProvider.CurrentBranch;

        if (!ModelState.IsValid)
            return Page();

        // MACN luôn trùng với chi nhánh hiện tại
        Kho.Macn = branch;

        var exists = await db.Khos.AnyAsync(k => k.Makho == Kho.Makho);
        if (exists)
        {
            ModelState.AddModelError("Kho.Makho", "Mã kho đã tồn tại.");
            return Page();
        }

        db.Khos.Add(Kho);
        await db.SaveChangesAsync();

        return RedirectToPage("Index");
    }
}
