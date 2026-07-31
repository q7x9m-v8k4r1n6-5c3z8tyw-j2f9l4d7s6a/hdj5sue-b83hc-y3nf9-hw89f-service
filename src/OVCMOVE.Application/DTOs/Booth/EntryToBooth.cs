using System.ComponentModel.DataAnnotations;

namespace OVCMOVE.Application.DTOs.Booth
{
    public class EntryToBoothDto
    {
        /// <summary>
        /// ID của Trạm mà Đội chơi vừa quét mã QR
        /// </summary>
        [Required(ErrorMessage = "Mã trạm (BoothId) không được để trống")]
        public Guid BoothId { get; set; }

        /// <summary>
        /// ID của Đội thi thực hiện quét mã QR
        /// </summary>
        [Required(ErrorMessage = "Mã đội chơi (TeamId) không được để trống")]
        public Guid TeamId { get; set; }
    }
}
