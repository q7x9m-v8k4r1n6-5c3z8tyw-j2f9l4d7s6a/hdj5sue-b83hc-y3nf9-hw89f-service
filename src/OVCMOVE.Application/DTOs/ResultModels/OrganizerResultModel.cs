namespace OVCMOVE.Application.DTOs.ResultModels;

public class OrganizerResultModel
{
    public class ChangeOrganizerStatusResultModel
    {
        public int OrganizerId { get; init; }

        public bool IsActive { get; init; }

        public bool IsSuccess { get; init; }

        public string Message { get; init; } = string.Empty;
    }
}
