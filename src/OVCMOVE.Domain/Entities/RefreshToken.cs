using OVCMOVE.Domain.Common;

namespace OVCMOVE.Domain.Entities;

/// <summary>
/// Domain.Entities: các thông tin của refreshtoken dùng cho login-logout
/// </summary>
public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; } 
    
    public string Token { get; set; } = string.Empty;
    
    public DateTime ExpiryDate { get; set; }
    
    public bool IsRevoked { get; set; } 
}