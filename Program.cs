using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QLVT.Web.Identity;
using QLVT.Web.Data;
using QLVT.Web.Services;
using QLVT.Web.Infrastructure.Branches;


var builder = WebApplication.CreateBuilder(args);


// 1. Factory cho QlvtDbContext (CN1/CN2)
builder.Services.AddDbContextFactory<QlvtDbContext>();

builder.Services.AddScoped<Func<string, QlvtDbContext>>(sp => branchName =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var optionsBuilder = new DbContextOptionsBuilder<QlvtDbContext>();

    var connStr = config.GetConnectionString(branchName)
                 ?? throw new InvalidOperationException($"Missing connection string for {branchName}");

    optionsBuilder.UseSqlServer(connStr);

    return new QlvtDbContext(optionsBuilder.Options);
});

// 2. DbContext cho phân mảnh 3 (tra cứu)
builder.Services.AddDbContext<QlvtLookupDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("TraCuu")));

// 3. Đăng ký provider chi nhánh & provider DbContext
builder.Services.AddHttpContextAccessor();
// builder.Services.AddScoped<IBranchProvider, FixedBranchProvider>(); // TODO: Thay bằng ClaimsBranchProvider : IBranchProvider
builder.Services.AddScoped<IBranchProvider, ClaimsBranchProvider>();
builder.Services.AddScoped<IBranchDbContextProvider, BranchDbContextProvider>();

// 4. Đăng ký dịch vụ đồng bộ dữ liệu chạy nền
builder.Services.AddHostedService<SyncDataService>();


// DbContext cho Identity (Auth)
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Auth")));

// Identity: ApplicationUser + 3 role
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = true;
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 6;
    })
    .AddEntityFrameworkStores<AuthDbContext>()
    .AddDefaultTokenProviders()
    .AddDefaultUI();


// Add services to the container.
builder.Services.AddAuthorization(options =>
{
    // Dựa trên logic đã có, các báo cáo này dành cho mọi người dùng đã đăng nhập
    options.AddPolicy("VatTuReportPolicy", policy => policy.RequireAuthenticatedUser());
    options.AddPolicy("BangKeReportPolicy", policy => policy.RequireAuthenticatedUser());
    options.AddPolicy("DonHangReportPolicy", policy => policy.RequireAuthenticatedUser());

    // Riêng báo cáo nhân viên chỉ dành cho CongTy và ChiNhanh
    options.AddPolicy("NhanVienReportPolicy", policy => policy.RequireRole("CongTy", "ChiNhanh"));
});
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// --- TỰ ĐỘNG MIGRATE VÀ SEED DATA KHI KHỞI ĐỘNG ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        // 1. Migrate AuthDbContext
        var authDbContext = services.GetRequiredService<AuthDbContext>();
        await authDbContext.Database.MigrateAsync();

        // 2. Migrate các DbContext còn lại (Lookup và các chi nhánh)
        var dbContextFactory = services.GetRequiredService<Func<string, QlvtDbContext>>();
        var lookupDbContext = services.GetRequiredService<QlvtLookupDbContext>();
        await DbInitializer.InitializeAsync(dbContextFactory, lookupDbContext);

        // 3. Seed dữ liệu ban đầu
        await SeedData.InitializeAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred during database initialization and seeding.");
    }
}
// ----------------------------------------------------

app.MapRazorPages();

await app.RunAsync();
