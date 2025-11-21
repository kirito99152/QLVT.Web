using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QLVT.Web.Infrastructure.Branches;

namespace QLVT.Web.Pages.Kho;

[Authorize(Roles = "ChiNhanh,User")]
public class DeleteModel : PageModel
{
    private readonly IBranchDbContextProvider _branchDb;
    private readonly IBranchProvider _branchProvider;

    public DeleteModel(IBranchDbContextProvider branchDb, IBranchProvider branchProvider)
    {
        _branchDb = branchDb;
        _branchProvider = branchProvider;
    }

    [BindProperty]
    public Data.Models.Kho Kho { get; set; } = default!;

    public string? ErrorMessage { get; set; }

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

    public async Task<IActionResult> OnPostAsync(string id)
    {
        if (string.IsNullOrEmpty(id))
            return NotFound();

        var db = _branchDb.DbContext;
        var branch = _branchProvider.CurrentBranch;

        var kho = await db.Khos.FirstOrDefaultAsync(k => k.Makho == id && k.Macn == branch);
        if (kho == null)
            return NotFound();

        var usedInPhieuNhap = await db.PhieuNhaps.AnyAsync(pn => pn.Makho == id);
        var usedInPhieuXuat = await db.PhieuXuats.AnyAsync(px => px.Makho == id);

        if (usedInPhieuNhap)
        {
            ErrorMessage = "Không thể xoá kho vì đã được dùng trong phiếu nhập.";
            Kho = kho;
            return Page();
        }
        if (usedInPhieuXuat)
        {
            ErrorMessage = "Không thể xoá kho vì đã được dùng trong phiếu xuất.";
            Kho = kho;
            return Page();
        }

        db.Khos.Remove(kho);
        await db.SaveChangesAsync();

        return RedirectToPage("Index");
    }
}
