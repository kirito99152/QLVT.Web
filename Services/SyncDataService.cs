using Microsoft.EntityFrameworkCore;
using QLVT.Web.Data;
using QLVT.Web.Data.Lookup;

namespace QLVT.Web.Services
{
    public class SyncDataService : BackgroundService
    {
        private readonly ILogger<SyncDataService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public SyncDataService(ILogger<SyncDataService> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Chờ 10 giây để ứng dụng khởi động hoàn tất
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Bắt đầu đồng bộ dữ liệu định kỳ.");

                try
                {
                    // Tạo scope riêng cho mỗi lần chạy để đảm bảo DbContext được giải phóng đúng cách
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var dbContextFactory = scope.ServiceProvider.GetRequiredService<Func<string, QlvtDbContext>>();
                        var lookupDbContext = scope.ServiceProvider.GetRequiredService<QlvtLookupDbContext>();

                        await SyncBranchData(dbContextFactory, lookupDbContext, "CN1", stoppingToken);
                        await SyncBranchData(dbContextFactory, lookupDbContext, "CN2", stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Đã xảy ra lỗi trong quá trình đồng bộ dữ liệu.");
                }

                _logger.LogInformation("Đồng bộ dữ liệu hoàn tất. Chờ 5 phút cho lần chạy tiếp theo.");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        private async Task SyncBranchData(
            Func<string, QlvtDbContext> dbContextFactory,
            QlvtLookupDbContext lookupDbContext,
            string branchCode,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Đang đồng bộ dữ liệu cho chi nhánh: {branchCode}");

            // Sử dụng DbContext factory để tạo context cho chi nhánh nguồn
            await using var sourceDbContext = dbContextFactory(branchCode);
            await using var transaction = await lookupDbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                // Đồng bộ NhanVien
                await lookupDbContext.NhanViens.Where(nv => nv.Macn == branchCode).ExecuteDeleteAsync(cancellationToken);
                var nhanViens = await sourceDbContext.NhanViens.AsNoTracking().ToListAsync(cancellationToken);
                var nhanVienLookups = nhanViens.Select(nv => new NhanVienLookup
                {
                    Manv = nv.Manv,
                    Ho = nv.Ho,
                    Ten = nv.Ten,
                    Diachi = nv.Diachi,
                    Ngaysinh = nv.Ngaysinh,
                    Luong = nv.Luong,
                    Macn = branchCode, // Gán mã chi nhánh
                    TrangThaiXoa = nv.TrangThaiXoa
                });
                await lookupDbContext.NhanViens.AddRangeAsync(nhanVienLookups, cancellationToken);

                // Đồng bộ Kho
                await lookupDbContext.Khos.Where(k => k.Macn == branchCode).ExecuteDeleteAsync(cancellationToken);
                var khos = await sourceDbContext.Khos.AsNoTracking().ToListAsync(cancellationToken);
                var khoLookups = khos.Select(k => new KhoLookup
                {
                    Makho = k.Makho,
                    Tenkho = k.Tenkho,
                    Diachi = k.Diachi,
                    Macn = branchCode // Gán mã chi nhánh
                });
                await lookupDbContext.Khos.AddRangeAsync(khoLookups, cancellationToken);

                // Đồng bộ ChiNhanh
                // Bảng ChiNhanh thường chỉ chứa 1 dòng trên mỗi DB, nhưng vẫn đồng bộ để đảm bảo
                await lookupDbContext.ChiNhanhs.Where(cn => cn.Macn == branchCode).ExecuteDeleteAsync(cancellationToken);
                var chiNhanhs = await sourceDbContext.ChiNhanhs.AsNoTracking().ToListAsync(cancellationToken);
                var chiNhanhLookups = chiNhanhs.Select(cn => new ChiNhanhLookup
                {
                    Macn = cn.Macn, // Chỉ đồng bộ mã và tên chi nhánh
                    ChiNhanh1 = cn.ChiNhanh1
                    // Không cần đồng bộ Diachi và SoDt cho lookup
                });
                await lookupDbContext.ChiNhanhs.AddRangeAsync(chiNhanhLookups, cancellationToken);

                // Lưu tất cả thay đổi vào DB tra cứu
                await lookupDbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation($"Đồng bộ thành công cho chi nhánh: {branchCode}");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken); // Rollback nếu có lỗi
                _logger.LogError(ex, $"Lỗi khi đồng bộ dữ liệu cho chi nhánh {branchCode}.");
                throw;
            }
        }
    }
}