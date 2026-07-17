using System;

namespace OVCMOVE.Api.Contracts;

/// <summary>
/// Gói dữ liệu dùng để  kết nối giữa một Đội đua và một Giải đua.
/// </summary>
public class BoothContract
{
    public string Name { get; set; }
    public string Place { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string BoothOrganizerID { get; set; }
    public Guid RaceID { get; set; } 
}