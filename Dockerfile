# ============================
# STAGE 1: BUILD (biên dịch)
# ============================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Đặt thư mục làm việc bên trong container là /src
WORKDIR /src

COPY . .

RUN dotnet restore

RUN dotnet publish -c Release -o /app/publish


# ============================
# STAGE 2: RUNTIME (chạy app)
# ============================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:8080

EXPOSE 8080

# Lệnh mặc định khi container khởi động: chạy ứng dụng QLVT.Web.dll bằng dotnet
ENTRYPOINT ["dotnet", "QLVT.Web.dll"]
