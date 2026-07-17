using OVCMOVE.Domain.Common;

namespace OVCMOVE.Domain.Entities;

public class RefreshTokenEntity : BaseEntity
{
    public Guid UserId { get; set; } 
    
    public string Token { get; set; } = string.Empty;
    
    public DateTime ExpiryDate { get; set; }
    
    public bool IsRevoked { get; set; } 
}