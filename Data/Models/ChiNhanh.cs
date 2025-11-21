using System;
using System.Collections.Generic;

namespace QLVT.Web.Data.Models;

public partial class ChiNhanh
{
    public string Macn { get; set; } = null!;

    public string ChiNhanh1 { get; set; } = null!;

    public string Diachi { get; set; } = null!;

    public string SoDt { get; set; } = null!;

    public virtual ICollection<Kho> Khos { get; set; } = new List<Kho>();

    public virtual ICollection<NhanVien> NhanViens { get; set; } = new List<NhanVien>();
}
