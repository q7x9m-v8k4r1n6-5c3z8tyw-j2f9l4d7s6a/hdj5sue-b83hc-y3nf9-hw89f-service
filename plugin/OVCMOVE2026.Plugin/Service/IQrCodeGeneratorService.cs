using System;

namespace OVCMOVE2026.Plugin.Services.QrCode;

public interface IQrCodeGeneratorService
{
    /// <summary>
    /// Nhận vào một chuỗi (Payload) và trả về mảng byte nguyên thủy của ảnh PNG mã QR
    /// </summary>
    byte[] GeneratePngBytes(string payload);
}