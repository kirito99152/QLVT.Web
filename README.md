# Hướng dẫn Cài đặt và Chạy dự án QLVT

Đây là tài liệu hướng dẫn các bước để cài đặt và chạy dự án Quản lý Vật tư (QLVT) trên môi trường phát triển (development).

## Yêu cầu tiên quyết

Trước khi bắt đầu, hãy đảm bảo bạn đã cài đặt các công cụ sau trên máy tính của mình:

*   [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
*   [Docker Desktop](https://www.docker.com/products/docker-desktop/)

## Hướng dẫn Cài đặt

### 1. Khởi chạy Cơ sở dữ liệu

Mở một terminal hoặc command prompt, di chuyển vào thư mục `SQLSERVER` của dự án và chạy lệnh sau để khởi tạo và chạy SQL Server trong một Docker container.

```bash
docker compose up -d
```

### 2. Cài đặt Entity Framework Core Tools (Chạy 1 lần)

Nếu bạn chưa cài đặt công cụ `dotnet-ef` trên máy, hãy chạy lệnh sau. Đây là công cụ cần thiết để quản lý migrations cho cơ sở dữ liệu.

```bash
dotnet tool install --global dotnet-ef --version 8.0.11
```

### 3. Tạo và Áp dụng Migrations (Chạy 1 lần)

Các lệnh sau sẽ tạo migrations ban đầu cho các DbContext khác nhau của dự án.

```bash
# Tạo migration cho các context
dotnet ef migrations add InitialCreate --context QlvtLookupDbContext
dotnet ef migrations add InitialCreate --context QlvtDbContext
dotnet ef migrations add InitialCreate --context AuthDbContext
```

## Chạy ứng dụng ở môi trường Dev

Sau khi hoàn tất các bước cài đặt, di chuyển vào thư mục của project web và chạy lệnh sau để khởi động ứng dụng. `dotnet watch` sẽ tự động build lại và tải lại ứng dụng mỗi khi có sự thay đổi trong mã nguồn.

```bash
dotnet watch
```