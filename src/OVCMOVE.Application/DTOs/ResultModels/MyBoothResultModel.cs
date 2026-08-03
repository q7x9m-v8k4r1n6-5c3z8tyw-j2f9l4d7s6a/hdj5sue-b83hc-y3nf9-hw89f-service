namespace OVCMOVE.Application.DTOs.ResultModels;

public class MyBoothResultModel
{
    public Guid BoothId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Place { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}