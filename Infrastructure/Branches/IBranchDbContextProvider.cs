using QLVT.Web.Data;

namespace QLVT.Web.Infrastructure.Branches;

public interface IBranchDbContextProvider : IDisposable
{
    /// <summary>
    /// DbContext tương ứng với chi nhánh hiện tại.
    /// Được cache trong scope để không tạo lại nhiều lần.
    /// </summary>
    QlvtDbContext DbContext { get; }
}
