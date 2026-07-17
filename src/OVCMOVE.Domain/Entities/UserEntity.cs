using OVCMOVE.Domain.Common;
using OVCMOVE.Domain.Constants;

namespace OVCMOVE.Domain.Entities;

public class UserEntity : BaseEntity
{
    public string? Username { get; set; } 
    
    public string? PasswordHash { get; set; }
    
    public string Email { get; set; } = string.Empty; 
    
    public string Role { get; set; } 
    
    public string? DisplayName { get; set; }
    
    public string Status { get; set; } = UserConstant.Status.Active; 
}