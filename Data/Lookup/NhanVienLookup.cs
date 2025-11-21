namespace QLVT.Web.Data.Lookup;

public class NhanVienLookup
{
    public Guid Manv { get; set; }
    public string Ho { get; set; } = null!;
    public string Ten { get; set; } = null!;
    public string? Diachi { get; set; }
    public DateTime? Ngaysinh { get; set; }
    public double? Luong { get; set; }
    public string Macn { get; set; } = null!;
    public int? TrangThaiXoa { get; set; }
}
