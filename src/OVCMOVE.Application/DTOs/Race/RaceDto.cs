using System;

namespace OVCMOVE.Application.DTOs.Race;

public static class RaceDto
{
    /// <summary>
    /// Booth input model for creating new race request
    /// </summary>
    public class BoothInput
    {
        public string Name { get; set; } = string.Empty;
        public string Place { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string OrganizerID { get; set; } = string.Empty;
    }
    /// <summary>
    /// Team input model for creating new race request
    /// </summary>
    public class RaceTeamInputDto
    {
        public Guid TeamID { get; set; }
    }
}
