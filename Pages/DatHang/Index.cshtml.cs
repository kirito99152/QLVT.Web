using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QLVT.Web.Data.Models;
using QLVT.Web.Infrastructure.Branches;

namespace QLVT.Web.Pages.DatHang;

[Authorize(Roles = "CongTy,ChiNhanh,User")]
public class IndexModel : PageModel
{
    private readonly IBranchDbContextProvider _branchDb;

    public IndexModel(IBranchDbContextProvider branchDb)
    {
        _branchDb = branchDb;
    }

    public IList<Data.Models.DatHang> DatHangs { get; set; } = new List<Data.Models.DatHang>();

    public async Task OnGetAsync()
    {
        var db = _branchDb.DbContext;

        DatHangs = await db.DatHangs
            .Include(d => d.ManvNavigation) // Nạp thông tin nhân viên để hiển thị tên
            .OrderByDescending(d => d.Ngay).ThenBy(d => d.MasoDdh)
            .ToListAsync();
    }

    public async Task<JsonResult> OnGetDetailsAsync(string id)
    {
        var db = _branchDb.DbContext;

        var datHang = await db.DatHangs
            .Include(d => d.ManvNavigation) // Nạp thông tin nhân viên
            .Include(d => d.Ctddhs)         // Nạp chi tiết đơn hàng
                .ThenInclude(ct => ct.MavtNavigation) // Từ chi tiết, nạp thông tin vật tư
            .FirstOrDefaultAsync(d => d.MasoDdh.Trim() == id.Trim());

        if (datHang == null)
        {
            return new JsonResult(new { error = "Đơn hàng không tìm thấy" }) { StatusCode = 404 };
        }

        // Tạo đối tượng để trả về client
        var result = new
        {
            masoDdh = datHang.MasoDdh.Trim(),
            ngay = datHang.Ngay.ToShortDateString(),
            nhaCc = datHang.NhaCc,
            nhanVien = $"{datHang.ManvNavigation?.Ho} {datHang.ManvNavigation?.Ten}",
            chiTiet = datHang.Ctddhs.Select(ct => new
            {
                tenVt = ct.MavtNavigation?.Tenvt,
                soLuong = ct.Soluong,
                donGia = ct.Dongia
            }).ToList()
        };

        return new JsonResult(result);
    }
}