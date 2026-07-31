using System;

namespace OVCMOVE.Application.DTOs.Booth
{
    public class BoothActiveSessionDto
    {
        public Guid BoothID { get; set; }
        public int Status { get; set; } 
        /// <summary>
        /// thông tin đội đang chơi trạm đó
        /// </summary>
        public Guid? TeamID { get; set; } 
        public string Name { get; set; }    
    }
}
