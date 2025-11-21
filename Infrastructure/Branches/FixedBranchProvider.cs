namespace QLVT.Web.Infrastructure.Branches;

public class FixedBranchProvider : IBranchProvider
{
    // TODO: thay bằng đọc từ Claims/Session
    public string CurrentBranch => "CN1";
}
