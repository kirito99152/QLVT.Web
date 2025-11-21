using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QLVT.Web.Pages.DatHang
{
    public class CreateDatHangInputModel
    {
        // Dùng để nhận giá trị từ trường ẩn, đảm bảo model state hợp lệ
        [Required]
        public string MasoDdh { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn ngày lập đơn.")]
        public DateOnly Ngay { get; set; } = DateOnly.FromDateTime(DateTime.Now);

        [Required(ErrorMessage = "Vui lòng nhập nhà cung cấp.")]
        public string NhaCc { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn kho.")]
        public string Makho { get; set; } = string.Empty;

        public List<ChiTietInputModel> ChiTiet { get; set; } = new();
    }

    public class ChiTietInputModel
    {
        public string Mavt { get; set; } = string.Empty;
        public int Soluong { get; set; }
        public double Dongia { get; set; }
    }
}