using System.ComponentModel.DataAnnotations;

namespace OVCMOVE.Application.DTOs.Booth;

public class BoothScoringRequestDTO
{
    [Required(ErrorMessage = "TeamID không được để trống")]
    public Guid TeamID { get; set; }

    [Required(ErrorMessage = "BoothID không được để trống")]
    public Guid BoothID { get; set; }

    //Đồng bộ khoảng điểm từ 1 đến 100 giống Validator
    [Range(1, 100, ErrorMessage = "Số điểm cộng mỗi lần phải từ 1 đến 100")]
    public int Score { get; set; }
}