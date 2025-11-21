using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QLVT.Web.Identity;
using QLVT.Web.Data.Models;
using QLVT.Web.Infrastructure.Branches;

namespace QLVT.Web.Pages.DatHang;

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
    public QLVT.Web.Data.Models.DatHang DatHang { get; set; } = new() { Ngay = DateOnly.FromDateTime(DateTime.Now) };

    [BindProperty]
    public List<Ctddh> ChiTietDatHangs { get; set; } = new();

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

        if (ChiTietDatHangs.Count == 0)
        {
            ModelState.AddModelError("", "Đơn đặt hàng phải có ít nhất một vật tư.");
        }

        // Kiểm tra trùng mã DDH
        var exists = await db.DatHangs.AnyAsync(d => d.MasoDdh == DatHang.MasoDdh);
        if (exists)
        {
            ModelState.AddModelError("DatHang.MasoDdh", "Mã số đơn đặt hàng đã tồn tại.");
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
            DatHang.Manv = user.Manv.Value;
        }
        else
        {
            ModelState.AddModelError("", "Không thể xác định được nhân viên lập phiếu.");
            await PopulateSelectListsAsync();
            return Page();
        }

        DatHang.Ctddhs = ChiTietDatHangs;

        db.DatHangs.Add(DatHang);
        await db.SaveChangesAsync();

        return RedirectToPage("./Index");
    }

    private async Task PopulateSelectListsAsync()
    {
        var db = _branchDb.DbContext;
        KhoSelectList = new SelectList(await db.Khos.ToListAsync(), "Makho", "Tenkho");
        VatTuSelectList = new SelectList(await db.Vattus.ToListAsync(), "Mavt", "Tenvt");
    }
}