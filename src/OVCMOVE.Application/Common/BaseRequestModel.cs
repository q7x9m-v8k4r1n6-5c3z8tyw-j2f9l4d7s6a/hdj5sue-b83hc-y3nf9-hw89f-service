using System;
using System.Collections.Generic;
using System.Text;

namespace OVCMOVE.Application.Common
{
    public class BaseRequestModel
    {
        public string? CreatedBy { get; set; }
        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; } = false;
    }
}
