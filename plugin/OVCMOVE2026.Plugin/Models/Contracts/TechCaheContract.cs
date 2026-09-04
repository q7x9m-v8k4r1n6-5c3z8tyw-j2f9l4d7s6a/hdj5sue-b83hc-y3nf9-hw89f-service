using Microsoft.AspNetCore.Http;

namespace OVCMOVE2026.Plugin.Models.Contracts;

public class VerifyMissionCodeRequest
{
    public string Code { get; set; } = string.Empty;
}

public class SubmitTechCacheResultRequest
{
    public string Code { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty; // "success" hoặc "fail"
    public IFormFile? Video { get; set; }
}