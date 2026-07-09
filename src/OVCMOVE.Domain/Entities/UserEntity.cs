using OVCMOVE.Domain.Common;
using OVCMOVE.Domain.Enums;

namespace OVCMOVE.Domain.Entities;

public class UserEntity : BaseEntity<Guid> 
{
    public string? Username { get; set; } 
    
    public string? PasswordHash { get; set; }
    
    public string Email { get; set; } = string.Empty; 
    
    public UserRole Role { get; set; } 
    
    public string? NickName { get; set; }
    
    public UserStatus Status { get; set; } = UserStatus.Active; 
}