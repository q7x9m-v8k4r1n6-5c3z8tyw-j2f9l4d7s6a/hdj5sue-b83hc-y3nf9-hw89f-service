using System;
using System.Collections.Generic;
using System.Text;

namespace OVCMOVE.Infrastructure.Options
{
    public class ExternalServicesConfigOptions
    {
        public const string SectionName = "ExternalServicesConfig";
        public EmailServiceOption EmailService { get; set; }

        public class EmailServiceOption {
            public string Email { get; set; }
            public string Password { get; set; }
        }
    }
}
