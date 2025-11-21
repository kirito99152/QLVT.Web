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

namespace QLVT.Web.Pages.PhieuNhap;

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
    public QLVT.Web.Data.Models.PhieuNhap PhieuNhap { get; set; } = new() { Ngay = DateOnly.FromDateTime(DateTime.Now) };

    [BindProperty]
    public List<Ctpn> ChiTietPhieuNhaps { get; set; } = new();

    public SelectList? DatHangSelectList { get; set; }

    public async Task OnGetAsync()
    {
        await PopulateDatHangSelectListAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var db = _branchDb.DbContext;

        // Validate
        if (!await ValidatePhieuNhapAsync(db))
        {
            await PopulateDatHangSelectListAsync();
            return Page();
        }

        if (!ModelState.IsValid)
        {
            await PopulateDatHangSelectListAsync();
            return Page();
        }

         // Gán nhân viên lập phiếu
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            ModelState.AddModelError("", "Không thể xác định người dùng hiện tại.");
            await PopulateDatHangSelectListAsync();
            return Page();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user?.Manv != null)
        {
            PhieuNhap.Manv = user.Manv.Value;
        }
        else
        {
            ModelState.AddModelError("", "Không thể xác định được nhân viên lập phiếu.");
            await PopulateDatHangSelectListAsync();
            return Page();
        }

        PhieuNhap.Ctpns = ChiTietPhieuNhaps;

        db.PhieuNhaps.Add(PhieuNhap);
        await db.SaveChangesAsync();

        return RedirectToPage("./Index");
    }

    private async Task PopulateDatHangSelectListAsync()
    {
        var db = _branchDb.DbContext;
        // Lấy danh sách các DDH chưa có phiếu nhập
        var ddhChuaNhap = await db.DatHangs
            .Where(d => d.PhieuNhap == null)
            .Select(d => new { d.MasoDdh, Display = $"{d.MasoDdh} - {d.Ngay.ToShortDateString()} - {d.NhaCc}" })
            .ToListAsync();

        DatHangSelectList = new SelectList(ddhChuaNhap, "MasoDdh", "Display");
    }

    private async Task<bool> ValidatePhieuNhapAsync(QlvtDbContext db)
    {
        var isValid = true;
        // Kiểm tra trùng mã PN
        if (await db.PhieuNhaps.AnyAsync(p => p.Mapn == PhieuNhap.Mapn))
        {
            ModelState.AddModelError("PhieuNhap.Mapn", "Mã phiếu nhập đã tồn tại.");
            isValid = false;
        }

        // Lấy chi tiết đơn đặt hàng gốc để so sánh
        var chiTietDatHang = await db.Ctddhs
            .Where(c => c.MasoDdh == PhieuNhap.MasoDdh)
            .ToDictionaryAsync(c => c.Mavt, c => c.Soluong);

        foreach (var ctpn in ChiTietPhieuNhaps)
        {
            if (!chiTietDatHang.TryGetValue(ctpn.Mavt, out var soLuongDat) || ctpn.Soluong > soLuongDat)
            {
                ModelState.AddModelError("", $"Số lượng nhập của vật tư {ctpn.Mavt} không được vượt quá số lượng đặt ({soLuongDat}).");
                isValid = false;
            }
        }
        return isValid;
    }

    public async Task<IActionResult> OnGetChiTietDatHangAsync(string masoDdh)
    {
        var db = _branchDb.DbContext;
        var chiTiet = await db.Ctddhs
            .Include(c => c.MavtNavigation)
            .Where(c => c.MasoDdh == masoDdh)
            .Select(c => new
            {
                c.Mavt,
                TenVt = c.MavtNavigation.Tenvt,
                c.Soluong,
                c.Dongia
            })
            .ToListAsync();
        return new JsonResult(chiTiet);
    }
}