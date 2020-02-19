using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PersonalFinanceReport.Toshl.Dto
{
    /// <summary>
    /// https://developer.toshl.com/docs/me/
    /// </summary>
    public class MeResponseDto
    {
        public MeResponseDto()
        {
            Social = new List<string>();
            Steps = new List<string>();
            Flags = new List<string>();
        }

        public string Id { get; set; }
        public string Email { get; set; }

        [JsonProperty("first_name")]
        public string FirstName { get; set; }

        [JsonProperty("last_name")]
        public string LastName { get; set; }

        public string Joined { get; set; }
        public string Modified { get; set; }
        public MeProResponseDto Pro { get; set; }
        public MeCurrencyResponseDto Currency { get; set; }

        [JsonProperty("start_day")]
        public int StartDay { get; set; }

        public int Notifications { get; set; }
        public List<string> Social { get; set; }
        public List<string> Steps { get; set; }
        public MeLimitsResponseDto Limits { get; set; }
        public string Locale { get; set; }
        public string Language { get; set; }
        public string Timezone { get; set; }
        public string Country { get; set; }

        [JsonProperty("otp_enabled")]
        public bool OtpEnabled { get; set; }

        public List<string> Flags { get; set; }
        public string Extra { get; set; }
    }

    public class MeProResponseDto
    {
        public string Level { get; set; }
        public string Since { get; set; }
        public string Until { get; set; }

        // ...
    }

    public class MeCurrencyResponseDto
    {
        public string Main { get; set; }
        public string Update { get; set; }

        [JsonProperty("update_accounts")]
        public bool UpdateAccounts { get; set; }

        // ...
    }

    public class MeLimitsResponseDto
    {
        public bool Accounts { get; set; }
        public bool Budgets { get; set; }
        public bool Images { get; set; }
        public bool Import { get; set; }
        public bool Bank { get; set; }
        public bool Repeats { get; set; }
        public bool Reminders { get; set; }
        public bool Export { get; set; }

        [JsonProperty("pro_share")]
        public bool ProShare { get; set; }

        public bool Passcode { get; set; }
        public bool Planning { get; set; }
        public bool Locations { get; set; }
    }
}
