# OVC-MOVE-SERVICE

## Giới thiệu dự án

OVC-MOVE-SERVICE là hệ thống backend phục vụ ứng dụng tính điểm, hỗ trợ vận hành vòng `RACE` trong chương trình `MOVE`.

- GitHub: `https://github.com/q7x9m-v8k4r1n6-5c3z8tyw-j2f9l4d7s6a/hdj5sue-b83hc-y3nf9-hw89f-service`
- Domain API: `https://api.oispvolunteerclub.com`

## Công nghệ sử dụng

Các công nghệ chính đang được áp dụng trong dự án:

- `.NET 10` (`net10.0`)
- `ASP.NET Core Web API` trên `.NET 10`
- `Clean Architecture`
- `MediatR 12.5.0`
- `AutoMapper 14.0.0`
- `Dapper 2.1.79`
- `Microsoft.Data.SqlClient 7.0.2`
- `Swashbuckle.AspNetCore 10.2.3`
- `DotNetEnv 3.2.0`
- `xUnit 2.9.3`
- `Microsoft.NET.Test.Sdk 17.14.1`
- `coverlet.collector 6.0.4`

## Chạy local bằng Docker

1. Tạo file `.env` từ `.env.example`.
2. Điền các biến:
   - `OVCMOVE_DbConfig__SQLServer__ConnectionString`: connection string SQL Server.
   - `OVCMOVE_ExternalServicesConfig__EmailService__Email`: email dùng để gửi mail.
   - `OVCMOVE_ExternalServicesConfig__EmailService__Password`: mật khẩu hoặc app password của email.
3. Build và chạy service:

```bash
docker compose up --build -d
```

API chạy tại `http://localhost:5000`. Kiểm tra health check:

```bash
curl http://localhost:5000/api/health
```

## CI/CD

GitHub Actions nằm trong `.github/workflows`.

- `ci-pipeline.yml`: chạy khi push lên `develop` hoặc mở pull request vào `main`/`develop`; thực hiện Build và Test.
- `cd-pipeline.yml`: chạy khi push lên `main`; build Docker image, push lên Docker Hub và Deploy lên Oracle VM qua SSH.

Các repository secrets cần cấu hình trên GitHub:

- `DOCKER_USERNAME`
- `DOCKER_PASSWORD`
- `ORACLE_VM_IP`
- `ORACLE_VM_USER`
- `ORACLE_VM_SSH_KEY`

Trên Oracle VM, thư mục `/app/ovcmove` cần có `docker-compose.yml` và file `.env` production với các biến `OVCMOVE_...` giống `.env.example`.
Workflow CD sẽ export `DOCKER_IMAGE` để `docker compose` dùng đúng image vừa pull từ Docker Hub.

## Cấu hình và bảo mật

- Database connection string, JWT secrets và các secret khác phải được gắn bằng biến môi trường hoặc GitHub Secrets.
- Không hard-code secret database/JWT vào `appsettings*.json` trong repo.
- `.env.example` chỉ chứa template, không chứa password thật.

## Health check

API health check trả về `200 OK` tại:

```bash
GET /api/health
```

Production domain:

```bash
https://api.oispvolunteerclub.com/api/health
```

## Thông tin phiên bản

### Version 2016.1.0

- Ngày bắt đầu: 06-07-2026
- Ngày kết thúc: Dự kiến cuối 09-2026

### Tác giả version 2016.1.0

| Vai trò | Họ và tên | Email |
| --- | --- | --- |
| Project Manager | Khoa Đào Anh | khoa.dao23102006@hcmut.edu.vn |
| Software Developer | Tân Nguyễn Nhật | tan.nguyennhat07@hcmut.edu.vn |
| Software Developer | Hồ Minh Phúc | phuc0908061@gmail.com |
| Software Developer | Đạt Lê Cẩm | dat.ledraco07@hcmut.edu.vn |
| Software Developer | Hiếu Lê Lương Trung | hieu.le27@hcmut.edu.vn |
| Software Developer | Hoàng Lương Thế | hoang.luong010206@hcmut.edu.vn |

