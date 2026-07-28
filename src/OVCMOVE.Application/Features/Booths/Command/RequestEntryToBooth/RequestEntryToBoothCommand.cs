using MediatR;
using System;

namespace OVCMOVE.Application.Features.Booths.Commands.RequestEntryToBooth;

/// <summary>
/// Tín hiệu Lệnh gửi qua MediatR Bus để xin tham gia trạm
/// Trả về một Tuple chứa trạng thái (IsSuccess) và Thông báo (Message)
/// </summary>
public class RequestEntryToBoothCommand : IRequest<(bool IsSuccess, string Message)>
{
    public Guid BoothId { get; set; }
    public Guid TeamId { get; set; }
}