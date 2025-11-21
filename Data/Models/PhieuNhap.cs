﻿using System;
using System.Collections.Generic;

namespace QLVT.Web.Data.Models;

public partial class PhieuNhap
{
    public string Mapn { get; set; } = null!;

    public DateOnly Ngay { get; set; }

    public string MasoDdh { get; set; } = null!;

    public Guid Manv { get; set; }

    public string? Makho { get; set; }

    public virtual ICollection<Ctpn> Ctpns { get; set; } = new List<Ctpn>();

    public virtual Kho? MakhoNavigation { get; set; }

    public virtual NhanVien ManvNavigation { get; set; } = null!;

    public virtual DatHang MasoDdhNavigation { get; set; } = null!;
}
