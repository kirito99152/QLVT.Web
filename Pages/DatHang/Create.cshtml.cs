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
    public CreateDatHangInputModel Input { get; set; } = new();

    public SelectList? KhoSelectList { get; set; }
    public SelectList? VatTuSelectList { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        await PopulateSelectListsAsync();

        // Tạo một mã DDH ngẫu nhiên gồm 8 ký tự và đảm bảo nó là duy nhất
        var db = _branchDb.DbContext;
        string newId;
        do
        {
            newId = GenerateRandomId(8);
        }
        while (await db.DatHangs.AnyAsync(d => d.MasoDdh == newId));

        Input.MasoDdh = newId;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Kiểm tra validation trên Input Model
        // ModelState bây giờ sẽ hoạt động chính xác vì Input Model khớp với dữ liệu từ form
        if (Input.ChiTiet.Count == 0)
        {
            ModelState.AddModelError("", "Đơn đặt hàng phải có ít nhất một vật tư.");
        }

        if (!ModelState.IsValid)
        {
            // Nếu không hợp lệ, tải lại các SelectList và hiển thị lại trang với lỗi
            await PopulateSelectListsAsync();
            return Page();
        }

        // Nếu ModelState hợp lệ, tiến hành ánh xạ từ ViewModel sang Entity Model
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = userId != null ? await _userManager.FindByIdAsync(userId) : null;

        if (user?.Manv == null)
        {
            ModelState.AddModelError("", "Không thể xác định được nhân viên lập phiếu.");
            await PopulateSelectListsAsync();
            return Page();
        }

        var newDatHang = new QLVT.Web.Data.Models.DatHang
        {
            MasoDdh = Input.MasoDdh,
            Ngay = Input.Ngay,
            NhaCc = Input.NhaCc,
            Makho = Input.Makho,
            Manv = user.Manv.Value,
            Ctddhs = Input.ChiTiet.Select(ct => new Ctddh
            {
                Mavt = ct.Mavt,
                Soluong = ct.Soluong,
                Dongia = ct.Dongia
            }).ToList()
        };

        // Lưu vào cơ sở dữ liệu
        var db = _branchDb.DbContext;
        db.DatHangs.Add(newDatHang);
        await db.SaveChangesAsync();

        return RedirectToPage("./Index");
    }

    private async Task PopulateSelectListsAsync()
    {
        var db = _branchDb.DbContext;
        KhoSelectList = new SelectList(await db.Khos.ToListAsync(), "Makho", "Tenkho");
        VatTuSelectList = new SelectList(await db.Vattus.ToListAsync(), "Mavt", "Tenvt");
    }

    private static string GenerateRandomId(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}