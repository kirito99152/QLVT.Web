using QLVT.Web.Data;

namespace QLVT.Web.Infrastructure.Branches;

public class BranchDbContextProvider : IBranchDbContextProvider
{
    private readonly Func<string, QlvtDbContext> _dbContextFactory;
    private readonly IBranchProvider _branchProvider;

    private QlvtDbContext? _dbContext;

    public BranchDbContextProvider(
        Func<string, QlvtDbContext> dbContextFactory,
        IBranchProvider branchProvider)
    {
        _dbContextFactory = dbContextFactory;
        _branchProvider = branchProvider;
    }

    public QlvtDbContext DbContext
        => _dbContext ??= _dbContextFactory(_branchProvider.CurrentBranch);

    public void Dispose()
    {
        _dbContext?.Dispose();
    }
}
