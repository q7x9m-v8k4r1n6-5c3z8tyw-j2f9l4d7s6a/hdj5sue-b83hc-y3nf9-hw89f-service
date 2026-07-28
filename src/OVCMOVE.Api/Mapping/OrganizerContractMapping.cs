using OVCMOVE.Api.Contracts;
using OVCMOVE.Application.Features.Organizers.Command.CreateOrganizer;
using OVCMOVE.Application.Features.Organizers.Command.ChangeOrganizerStatus;
using OVCMOVE.Application.Features.Organizers.Query.GetAllOrganizers;
using OVCMOVE.Application.Features.Organizers.Query.SearchOrganizer;

namespace OVCMOVE.Api.Mapping;

public static class OrganizerContractMapping
{
    /// <summary>Maps the organizer API request to its create command.</summary>
    public static CreateOrganizerCommand ToCommand(
        this OrganizerContract.CreateOrganizerRequest request) => new()
        {
            Email = request.Email,
            RoleIds = request.RoleIds
        };

    public static OrganizerContract.OrganizerResponse ToResponse(
        this OrganizerResponse result) => new()
        {
            Id = result.Id,
            Email = result.Email,
            DisplayName = result.DisplayName,
            Role = result.Role,
            Status = result.Status,
            CreatedAt = result.CreatedAt
        };

    public static OrganizerContract.OrganizerStatusResponse ToResponse(
        this OrganizerStatusResponse result) => new()
        {
            OrganizerId = result.OrganizerId,
            Status = result.Status
        };

    public static OrganizerContract.OrganizerListItemResponse ToResponse(
        this GetAllOrganizersResultModel result) => new()
        {
            Id = result.Id,
            UserId = result.UserId,
            DisplayName = result.DisplayName,
            Email = result.Email,
            AvatarUrl = result.AvatarUrl,
            Role = result.Role,
            Status = result.Status
        };

    public static OrganizerContract.OrganizerSearchItemResponse ToResponse(
        this SearchOrganizerResultModel result) => new()
        {
            Id = result.Id,
            DisplayName = result.DisplayName,
            Email = result.Email,
            AvatarUrl = result.AvatarUrl
        };
}
