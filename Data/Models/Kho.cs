using System;
using System.Collections.Generic;

namespace QLVT.Web.Data.Models;

public partial class Kho
{
    public string Makho { get; set; } = null!;

    public string Tenkho { get; set; } = null!;

    public string? Diachi { get; set; }

    public string? Macn { get; set; }

    public virtual ICollection<DatHang> DatHangs { get; set; } = new List<DatHang>();

    public virtual ChiNhanh? MacnNavigation { get; set; }

    public virtual ICollection<PhieuNhap> PhieuNhaps { get; set; } = new List<PhieuNhap>();

    public virtual ICollection<PhieuXuat> PhieuXuats { get; set; } = new List<PhieuXuat>();
}
