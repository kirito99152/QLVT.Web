using System;
using System.Collections.Generic;

namespace QLVT.Web.Data.Models;

public partial class DatHang
{
    public string MasoDdh { get; set; } = null!;

    public DateOnly Ngay { get; set; }

    public string NhaCc { get; set; } = null!;

    public Guid Manv { get; set; }

    public string? Makho { get; set; }

    public virtual ICollection<Ctddh> Ctddhs { get; set; } = new List<Ctddh>();

    public virtual Kho? MakhoNavigation { get; set; }

    public virtual NhanVien ManvNavigation { get; set; } = null!;

    public virtual PhieuNhap? PhieuNhap { get; set; }
}
