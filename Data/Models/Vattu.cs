using System;
using System.Collections.Generic;

namespace QLVT.Web.Data.Models;

public partial class Vattu
{
    public string Mavt { get; set; } = null!;

    public string Tenvt { get; set; } = null!;

    public string Dvt { get; set; } = null!;

    public int Soluongton { get; set; }

    public virtual ICollection<Ctddh> Ctddhs { get; set; } = new List<Ctddh>();

    public virtual ICollection<Ctpn> Ctpns { get; set; } = new List<Ctpn>();

    public virtual ICollection<Ctpx> Ctpxes { get; set; } = new List<Ctpx>();
}
