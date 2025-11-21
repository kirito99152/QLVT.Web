using System;
using System.Collections.Generic;

namespace QLVT.Web.Data.Models;

public partial class NhanVien
{
    public Guid Manv { get; set; }

    public string? Ho { get; set; }

    public string? Ten { get; set; }

    public string? Diachi { get; set; }

    public DateTime? Ngaysinh { get; set; }

    public double? Luong { get; set; }

    public string? Macn { get; set; }

    public int? TrangThaiXoa { get; set; }

    public virtual ICollection<DatHang> DatHangs { get; set; } = new List<DatHang>();

    public virtual ChiNhanh? MacnNavigation { get; set; }

    public virtual ICollection<PhieuNhap> PhieuNhaps { get; set; } = new List<PhieuNhap>();

    public virtual ICollection<PhieuXuat> PhieuXuats { get; set; } = new List<PhieuXuat>();
}
