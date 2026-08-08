using System;
using QRCoder;

namespace OVCMOVE2026.Plugin.Services.QrCode;

public class QrCodeGeneratorService : IQrCodeGeneratorService
{
    public byte[] GeneratePngBytes(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new ArgumentNullException(nameof(payload), "Nội dung QR không được để trống.");
        }

        // Khởi tạo bộ tạo mã QR
        using var qrGenerator = new QRCodeGenerator();
        
        // Tạo data QR với mức độ sửa lỗi Q (Chất lượng cao, chịu được hỏng hóc 25%)
        using var qrCodeData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        
        // Render thẳng ra PngByte (Không dùng System.Drawing để đảm bảo chạy mượt trên Linux/Docker)
        using var qrCode = new PngByteQRCode(qrCodeData);
        
        // Trả về mảng byte (số 20 là kích thước pixel của mỗi ô vuông nhỏ trong QR)
        return qrCode.GetGraphic(20);
    }
}