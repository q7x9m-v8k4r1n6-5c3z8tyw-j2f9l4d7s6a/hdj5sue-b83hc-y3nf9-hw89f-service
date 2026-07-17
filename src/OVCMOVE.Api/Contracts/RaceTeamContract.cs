using System;

namespace OVCMOVE.Api.Contracts;

/// <summary>
/// Gói dữ liệu dùng để  kết nối giữa một Đội đua và một Giải đua.
/// </summary>
public class RaceTeamContract
{
    public Guid RaceID { get; set; }
    public Guid TeamID { get; set; }
}