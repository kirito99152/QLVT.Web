using System;
using System.Collections.Generic;

namespace QLVT.Web.Data.Models;

public partial class Ctpn
{
    public string Mapn { get; set; } = null!;

    public string Mavt { get; set; } = null!;

    public int Soluong { get; set; }

    public double Dongia { get; set; }

    public virtual PhieuNhap MapnNavigation { get; set; } = null!;

    public virtual Vattu MavtNavigation { get; set; } = null!;
}
