namespace OVCMOVE.Application.DTOs.Booth;

/// <summary>
/// DTO chứa thông tin Organizer duyệt cho Đội vào trạm
/// </summary>
public class AcceptEntryToBoothDto
{
    public Guid BoothId { get; set; }
    public Guid TeamId { get; set; }
}