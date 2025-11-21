using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QLVT.Web.Data;
using QLVT.Web.Data.Models;
using QLVT.Web.Infrastructure.Branches;

namespace QLVT.Web.Pages.NhanVien;

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
    public Data.Models.NhanVien NhanVien { get; set; } = default!;

    public string? ErrorMessage { get; set; }

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

    public async Task<IActionResult> OnPostAsync(Guid id)
    {
        var db = _branchDb.DbContext;
        var branch = _branchProvider.CurrentBranch;

        var nv = await db.NhanViens.FirstOrDefaultAsync(n => n.Manv.Equals(id) && n.Macn == branch);
        if (nv == null)
        {
            return NotFound();
        }

        // kiểm tra ràng buộc: đã dùng trong phiếu / đơn hàng chưa
        var usedInOrders = await db.DatHangs.AnyAsync(d => d.Manv.Equals(id));
        var usedInPhieuNhap = await db.PhieuNhaps.AnyAsync(pn => pn.Manv.Equals(id));
        var usedInPhieuXuat = await db.PhieuXuats.AnyAsync(px => px.Manv.Equals(id));

        if (usedInOrders)
        {
            ErrorMessage = "Không thể xoá nhân viên vì đã tham gia đơn đặt hàng.";
            NhanVien = nv;
            return Page();
        }
        if (usedInPhieuNhap)
        {
            ErrorMessage = "Không thể xoá nhân viên vì đã tham gia phiếu nhập.";
            NhanVien = nv;
            return Page();
        }
        if (usedInPhieuXuat)
        {
            ErrorMessage = "Không thể xoá nhân viên vì đã tham gia phiếu xuất.";
            NhanVien = nv;
            return Page();
        }

        db.NhanViens.Remove(nv);
        await db.SaveChangesAsync();

        return RedirectToPage("Index");
    }
}
