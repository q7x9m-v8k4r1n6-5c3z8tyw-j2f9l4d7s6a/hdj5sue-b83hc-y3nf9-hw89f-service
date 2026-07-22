using OVCMOVE.Domain.Common;

namespace OVCMOVE.Domain.Entities;

/// <summary>
/// Domain.Entities: các thông tin của refreshtoken dùng cho login-logout
/// </summary>
public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; } 
    public Guid SessionId { get; set; }
    public Guid FamilyId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public Guid? ReplacedByTokenId { get; set; }
    public DateTime? RevokedAt { get; set; }
    
    public DateTime ExpiryDate { get; set; }
    
    public bool IsRevoked { get; set; } 
}
