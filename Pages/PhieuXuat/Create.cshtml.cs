using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QLVT.Web.Identity;
using QLVT.Web.Data;
using QLVT.Web.Data.Models;
using QLVT.Web.Infrastructure.Branches;

namespace QLVT.Web.Pages.PhieuXuat;

[Authorize(Roles = "ChiNhanh,User")]
public class CreateModel : PageModel
{
    private readonly IBranchDbContextProvider _branchDb;
    private readonly UserManager<ApplicationUser> _userManager;

    public CreateModel(IBranchDbContextProvider branchDb, UserManager<ApplicationUser> userManager)
    {
        _branchDb = branchDb;
        _userManager = userManager;
    }

    [BindProperty]
    public QLVT.Web.Data.Models.PhieuXuat PhieuXuat { get; set; } = new() { Ngay = DateOnly.FromDateTime(DateTime.Now) };

    [BindProperty]
    public List<Ctpx> ChiTietPhieuXuats { get; set; } = new();

    public SelectList? KhoSelectList { get; set; }
    public SelectList? VatTuSelectList { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        await PopulateSelectListsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var db = _branchDb.DbContext;

        if (!await ValidatePhieuXuatAsync(db))
        {
            await PopulateSelectListsAsync();
            return Page();
        }

        if (!ModelState.IsValid)
        {
            await PopulateSelectListsAsync();
            return Page();
        }

         // Gán nhân viên lập phiếu
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            ModelState.AddModelError("", "Không thể xác định người dùng hiện tại.");
            await PopulateSelectListsAsync();
            return Page();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user?.Manv != null)
        {
            PhieuXuat.Manv = user.Manv.Value;
        }
        else
        {
            ModelState.AddModelError("", "Không thể xác định được nhân viên lập phiếu.");
            await PopulateSelectListsAsync();
            return Page();
        }

        PhieuXuat.Ctpxes = ChiTietPhieuXuats;

        db.PhieuXuats.Add(PhieuXuat);
        
        // Cập nhật số lượng tồn kho
        foreach (var ctpx in ChiTietPhieuXuats)
        {
            var vattu = await db.Vattus.FindAsync(ctpx.Mavt);
            if (vattu != null)
            {
                vattu.Soluongton -= ctpx.Soluong;
            }
        }

        await db.SaveChangesAsync();

        return RedirectToPage("./Index");
    }

    private async Task PopulateSelectListsAsync()
    {
        var db = _branchDb.DbContext;
        KhoSelectList = new SelectList(await db.Khos.ToListAsync(), "Makho", "Tenkho");
        VatTuSelectList = new SelectList(await db.Vattus.Where(vt => vt.Soluongton > 0).ToListAsync(), "Mavt", "Tenvt");
    }

    private async Task<bool> ValidatePhieuXuatAsync(QlvtDbContext db)
    {
        var isValid = true;
        if (await db.PhieuXuats.AnyAsync(p => p.Mapx == PhieuXuat.Mapx))
        {
            ModelState.AddModelError("PhieuXuat.Mapx", "Mã phiếu xuất đã tồn tại.");
            isValid = false;
        }

        var tonKhoVatTu = await db.Vattus.ToDictionaryAsync(vt => vt.Mavt, vt => vt.Soluongton);
        foreach (var ctpx in ChiTietPhieuXuats)
        {
            if (!tonKhoVatTu.TryGetValue(ctpx.Mavt, out var soLuongTon) || ctpx.Soluong > soLuongTon)
            {
                ModelState.AddModelError("", $"Số lượng xuất của vật tư {ctpx.Mavt} không được vượt quá số lượng tồn ({soLuongTon}).");
                isValid = false;
            }
        }
        return isValid;
    }
}