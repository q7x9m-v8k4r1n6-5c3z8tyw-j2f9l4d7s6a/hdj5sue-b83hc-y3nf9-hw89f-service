using System.Text;
using System.Text.Encodings.Web;

namespace OVCMOVE.Application.Common;

/// <summary>Provides the shared branded layout for account notification emails.</summary>
public static class AccountEmailTemplate
{
    public static string Subject(string title) => $"[OVC] {title}";

    public static string Build(
        string title,
        string greeting,
        string introduction,
        IReadOnlyCollection<(string Label, string Value)> details,
        string? closing = null)
    {
        var encode = HtmlEncoder.Default;
        var detailRows = new StringBuilder();
        foreach (var (label, value) in details)
        {
            detailRows.Append($"<div style=\"margin-bottom:12px;font-size:14px;line-height:21px;color:#525252;\">{encode.Encode(label)}<br><strong style=\"font-size:16px;color:#1a1c1c;\">{encode.Encode(value)}</strong></div>");
        }

        return $"""
            <!doctype html>
            <html lang="vi"><body style="margin:0;padding:0;background:#f7f7f7;font-family:Arial,sans-serif;color:#1a1c1c;">
            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="padding:32px 16px;background:#f7f7f7;"><tr><td align="center">
            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:600px;background:#fff;border:1px solid #eee;border-radius:16px;overflow:hidden;">
            <tr><td style="padding:24px 32px;background:#420001;color:#fff;"><div style="font-size:22px;font-weight:700;letter-spacing:.5px;">OISP Volunteer Club</div></td></tr>
            <tr><td style="padding:32px;"><h1 style="margin:0 0 12px;font-size:24px;line-height:32px;">{encode.Encode(title)}</h1><p style="margin:0 0 24px;font-size:15px;line-height:24px;color:#525252;">Mến chào <strong style="color:#1a1c1c;">{encode.Encode(greeting)}</strong>, {encode.Encode(introduction)}</p>
            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background:#fff5f5;border:1px solid #fdcacb;border-radius:12px;"><tr><td style="padding:20px;"><div style="margin-bottom:14px;font-size:12px;font-weight:700;letter-spacing:.8px;color:#8b1f21;text-transform:uppercase;">Thông tin tài khoản</div>{detailRows}</td></tr></table>
            <p style="margin:24px 0 0;font-size:14px;line-height:22px;color:#525252;">{encode.Encode(closing ?? "Chúc bạn có những trải nghiệm tuyệt vời với MOVE!")}</p><p style="margin:20px 0 0;font-size:14px;line-height:22px;color:#525252;">Trân trọng,<br>OISP Volunteer Club</p></td></tr>
            <tr><td style="padding:18px 32px;background:#fafafa;border-top:1px solid #eee;font-size:12px;line-height:18px;color:#737373;">Đây là email tự động từ OISP Volunteer Club, vui lòng bỏ qua nếu bạn không nhận biết yêu cầu này.</td></tr>
            </table></td></tr></table></body></html>
            """;
    }
}
