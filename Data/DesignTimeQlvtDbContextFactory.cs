using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace QLVT.Web.Data
{
    /// <summary>
    /// Factory này chỉ được sử dụng bởi các công cụ dòng lệnh của EF Core (ví dụ: dotnet ef migrations add).
    /// Nó cho phép các công cụ tạo một DbContext với một chuỗi kết nối cụ thể để so sánh model và tạo migration.
    /// Chúng ta chỉ cần trỏ đến MỘT trong các DB chi nhánh (ví dụ: CN1) vì tất cả chúng đều có cùng một schema.
    /// </summary>
    public class DesignTimeQlvtDbContextFactory : IDesignTimeDbContextFactory<QlvtDbContext>
    {
        public QlvtDbContext CreateDbContext(string[] args)
        {
            // Lấy configuration từ appsettings.json
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<QlvtDbContext>();

            // Sử dụng chuỗi kết nối của một chi nhánh bất kỳ, ví dụ "CN1"
            var connectionString = configuration.GetConnectionString("CN1");
            optionsBuilder.UseSqlServer(connectionString);

            return new QlvtDbContext(optionsBuilder.Options);
        }
    }
}