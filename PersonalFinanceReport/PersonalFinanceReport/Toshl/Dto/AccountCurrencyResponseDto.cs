using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PersonalFinanceReport.Toshl.Dto
{
    public class AccountCurrencyResponseDto
    {
        public string Code { get; set; }
        public decimal Rate { get; set; }

        [JsonProperty("main_rate")]
        public decimal MainRate { get; set; }

        public bool Fixed { get; set; }
    }
}
