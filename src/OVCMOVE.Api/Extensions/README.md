# 🛡️ Hướng dẫn sử dụng Rate Limiting & Lockout API

Dự án hiện tại áp dụng cơ chế bảo mật nhiều lớp (Defense in Depth) để chống Spam và Hacker Brute-force.

## 1. Internal APIs (Các API thông thường)
Mặc định, TẤT CẢ các API kế thừa từ `BaseController` đều bị áp dụng luật `InternalApiPolicy` (Tối đa 60 requests / 1 phút / 1 IP).
Nếu user vượt quá, API tự động trả về **HTTP 429 (Too Many Requests)**.

**💡 Cách tuỳ chỉnh cho từng API cụ thể:**
- **Muốn siết chặt hơn (vd: API Export data chỉ cho 5 req/phút):**
  1. Vào `RateLimiterExtensions.cs`, copy thêm 1 luật mới (vd: `StrictPolicy` = 5 req/min).
  2. Lên Controller, gắn thẻ `[EnableRateLimiting("StrictPolicy")]` đè lên hàm đó.
- **Muốn tắt hoàn toàn Rate Limit (vd: API Webhook của đối tác):**
  1. Gắn thẻ `[DisableRateLimiting]` lên hàm đó.

## 2. Login APIs (Bảo vệ Brute-force)
Các API Login sử dụng song song 2 lớp bảo vệ:
- Lớp 1: Chống Spam DDoS (bằng Sliding Window của BaseController).
- Lớp 2: Chống đoán mật khẩu (bằng `LoginLockoutService`).

**Quy tắc phạt của LoginLockoutService:**
- Sai 5 lần liên tiếp: Trả về **HTTP 429**. Yêu cầu đợi 10 giây.
- Sai 20 lần liên tiếp: Trả về **HTTP 403 (Forbidden)**. Khóa IP/Tài khoản trong 24 giờ.
*(Đăng nhập thành công sẽ tự động reset toàn bộ hồ sơ đen).*