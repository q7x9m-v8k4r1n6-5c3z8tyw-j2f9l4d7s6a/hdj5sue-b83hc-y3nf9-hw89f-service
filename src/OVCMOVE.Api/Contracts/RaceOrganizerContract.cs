using System;

namespace OVCMOVE.Api.Contracts;

/// <summary>
/// Gói dữ liệu dùng để  kết nối giữa BTC và một Giải đua.
/// </summary>
public class RaceOrganizerContract
{
    public Guid RaceID { get; set; }
    public Guid OrganizerID { get; set; }
}