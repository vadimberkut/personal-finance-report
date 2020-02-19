using System;
using System.Collections.Generic;
using System.Text;

namespace PersonalFinanceReport
{
    public class ApplicationSettings
    {
        public ToshlSettings Toshl { get; set; }
    }

    public class ToshlSettings
    {
        public string HttpApiUrl { get; set; }
        public string PersonalApiToken { get; set; }
    }
}
