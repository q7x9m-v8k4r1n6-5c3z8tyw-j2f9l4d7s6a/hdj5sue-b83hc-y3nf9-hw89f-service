using OVCMOVE.Domain.Common;

namespace OVCMOVE.Domain.Entities;

/// <summary>
/// Domain entity: Lịch sử thay đổi điểm số bởi Actor
/// </summary>
public class ScoringLog : BaseEntity
{
    public string EventCode {get; set;} = string.Empty;
    public string EventName {get; set;} = string.Empty;
    public Guid RaceId {get; set;}
    public Guid TeamId {get; set;}
    public Guid? ActorId {get; set;} // null ~ system
    public Guid? BoothId {get; set;}
    public int Delta {get; set;}
    public int ScoreBefore {get; set;}
    public int ScoreAfter {get; set;}
    public string ReasonCode {get; set;} = string.Empty;
    public string Reason {get; set;} = string.Empty;
}