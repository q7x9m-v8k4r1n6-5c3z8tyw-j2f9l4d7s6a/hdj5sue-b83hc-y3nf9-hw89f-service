using Microsoft.AspNetCore.Http;

namespace OVCMOVE2026.Plugin.Models.Contracts;

public class SubmitMissionEvidenceRequest
{
    public List<IFormFile>? Images { get; set; }
    public List<IFormFile>? Videos { get; set; }
}