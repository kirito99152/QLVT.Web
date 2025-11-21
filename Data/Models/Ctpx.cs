using System;
using System.Collections.Generic;

namespace QLVT.Web.Data.Models;

public partial class Ctpx
{
    public string Mapx { get; set; } = null!;

    public string Mavt { get; set; } = null!;

    public int Soluong { get; set; }

    public double Dongia { get; set; }

    public virtual PhieuXuat MapxNavigation { get; set; } = null!;

    public virtual Vattu MavtNavigation { get; set; } = null!;
}
