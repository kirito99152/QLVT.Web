﻿using System;
using System.Collections.Generic;

namespace QLVT.Web.Data.Models;

public partial class PhieuXuat
{
    public string Mapx { get; set; } = null!;

    public DateOnly Ngay { get; set; }

    public string Hotenkh { get; set; } = null!;

    public Guid Manv { get; set; }

    public string? Makho { get; set; }

    public virtual ICollection<Ctpx> Ctpxes { get; set; } = new List<Ctpx>();

    public virtual Kho? MakhoNavigation { get; set; }

    public virtual NhanVien ManvNavigation { get; set; } = null!;
}
