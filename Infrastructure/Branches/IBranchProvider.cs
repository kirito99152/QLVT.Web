namespace QLVT.Web.Infrastructure.Branches;

public interface IBranchProvider
{
    /// <summary>
    /// Mã chi nhánh hiện tại, ví dụ: "CN1" hoặc "CN2".
    /// TODO: đọc từ user/claims bỏ hard-code.
    /// </summary>
    string CurrentBranch { get; }
}
